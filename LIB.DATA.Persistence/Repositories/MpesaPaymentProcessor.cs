using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;
using IRepository;
using DTO;

namespace LIB.API.Persistence.Repositories
{
    public class MpesaPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IMpesaRepositoryAPI _mpesaRepository;
        private static readonly Random _random = new Random();

        public MpesaPaymentProcessor(LIBAPIDbSQLContext dbContext, IMpesaRepositoryAPI mpesaRepository)
        {
            _dbContext = dbContext;
            _mpesaRepository = mpesaRepository;
        }

        public async Task<Response> ProcessPaymentAsync(TransferRequest request, bool simulationIndicator)
        {
            var transaction = await _dbContext.Transaction
                .Where(t => t.referenceId == request.ReferenceId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            try
            {
                if (transaction == null)
                {
                    // Log the error into the ErrorLog table
                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),  // Generate a random ticket ID for tracking
                        traceId = request.ReferenceId.ToString(),  // The reference ID for the transaction
                        returnCode = "SB_DS_004",  // The error code indicating transaction not found
                        EventDate = DateTime.UtcNow,  // Time when the error occurred
                        feedbacks = "Transaction not found in the database."  // Description of the error
                    };

                    // Add the error log entry to the database
                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_DS_004",
                        Message = "Transaction not found in the database."
                    };
                }

                // Call Mpesa API to process the transfer with amount and phone number
                FinInsInsResponseDTO mpesaResponse = await _mpesaRepository.CreateMpesaTransfer(request.Amount.Value, request.PaymentInformation.Account.Id);

                if (mpesaResponse == null || !mpesaResponse.success)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = mpesaResponse?.message ?? "Mpesa transaction failed.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_MP_001",
                        Message = $"Mpesa transaction failed: {mpesaResponse?.message}",
                        // Data = mpesaResponse
                    };
                }

                //// Successful transaction update
                transaction.status = "Success";
                transaction.bankStatusMessage = "Mpesa transaction successful.";
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.conversationId = mpesaResponse.FinInstransactionId; // Store transaction ID


                var apiResponse = new TransferPostResponseBody
                {
                    Id = request.ReferenceId,
                    Status = "ETSWITCH transaction successful."
                    // Populate other properties as needed
                };
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = true,
                    Data = apiResponse
                };
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = "Mpesa transaction failed.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                return new Response
                {
                    IsSuccess = false,
                    ErrorCode = "SB_DS_003",
                    Message = $"Error processing transaction: {ex.Message}",
                    Data = new FinInsInsResponseDTO
                    {
                        success = false,
                        message = ex.Message,
                        FinInstransactionId = null,
                        ConversationID = null
                    }
                };
            }
        }
        public async Task<Response> ProcessPaymentAsyncRtgs(TransferRequest request, bool simulationIndicator, string name, string account)
        {
            return new Response
            {
                IsSuccess = false,
                ErrorCode = "SB_Rtgs_001",
                Message = "Rtgs transaction response is null."
            };
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var stringBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[_random.Next(chars.Length)]);
            }

            return stringBuilder.ToString();
        }
    }
}
