using System;
using System.Text;
using System.Threading.Tasks;
using DTO;
using IRepository;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace LIB.API.Persistence.Repositories
{
    public class TelebirrPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly ITelebirrRepositoryAPI _telebirrRepositoryAPI;
        private static readonly Random _random = new Random();
        public TelebirrPaymentProcessor(LIBAPIDbSQLContext dbContext, ITelebirrRepositoryAPI telebirrRepositoryAPI)
        {
            _dbContext = dbContext;
            _telebirrRepositoryAPI = telebirrRepositoryAPI;
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
                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_DS_004",
                        Message = "Transaction not found in the database."
                    };
                }

                // Simulate API call delay
                await Task.Delay(1000);

                // Call Telebirr API to process the transfer
                FinInsInsResponseDTO telebirrResponse = await _telebirrRepositoryAPI.CreateTelebirrTransaction(request.Amount.Value, request.PaymentInformation.Account.Id);

                if (telebirrResponse == null)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_TB_001",
                        Message = "Telebirr transaction response is null.",
                        Data = new FinInsInsResponseDTO
                        {
                            success = false,
                            message = "Telebirr response is null",
                            FinInstransactionId = null,
                            ConversationID = null
                        }
                    };
                }

                //// Handle unsuccessful transactions from Telebirr
                if (!telebirrResponse.success)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = $"Telebirr transaction failed: {telebirrResponse.message}";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),
                        traceId = request.ReferenceId.ToString(),
                        returnCode = "SB_TB_002",
                        EventDate = DateTime.UtcNow,
                        feedbacks = $"Telebirr transaction failed: {telebirrResponse.message}"
                    };

                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_TB_002",
                        Message = $"Telebirr transaction failed: {telebirrResponse.message}",
                        Data = telebirrResponse
                    };
                }

                //// Successful transaction update
                transaction.status = "Success";
                transaction.bankStatusMessage = "Telebirr transaction successful.";
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.conversationId = telebirrResponse.ConversationID; // Store transaction ID

                await _dbContext.SaveChangesAsync();

                var apiResponse = new TransferPostResponseBody
                {
                    Id = request.ReferenceId,
                    Status = "ETSWITCH transaction successful."
                    // Populate other properties as needed
                };
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
                    transaction.bankStatusMessage = "Telebirr transaction failed.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                var errorLog = new ErrorLog
                {
                    ticketId = GenerateRandomString(6),
                    traceId = request.ReferenceId.ToString(),
                    returnCode = "SB_DS_003",
                    EventDate = DateTime.UtcNow,
                    feedbacks = $"Error processing transaction: {ex.Message}"
                };

                _dbContext.ErrorLog.Add(errorLog);
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = false,
                    ErrorCode = "SB_DS_003",
                    Message = $"Error processing transaction: {ex.Message}",
                    //Data = new FinInsInsResponseDTO
                    //{
                    //    success = false,
                    //    message = ex.Message,
                    //    FinInstransactionId = null,
                    //    ConversationID = null
                    //}
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
