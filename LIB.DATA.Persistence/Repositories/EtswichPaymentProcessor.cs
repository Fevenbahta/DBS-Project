using System;
using System.Text;
using System.Threading.Tasks;
using IRepository;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;
using Repository;

namespace LIB.API.Persistence.Repositories
{
    public class EtswichPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IEthswichRepositoryAPI _ethswichRepositoryAPI; // Inject the service

        public EtswichPaymentProcessor(LIBAPIDbSQLContext dbContext, IEthswichRepositoryAPI ethswichRepositoryAPI)
        {
            _dbContext = dbContext;
            _ethswichRepositoryAPI = ethswichRepositoryAPI; // Assign service
        }

        private static readonly Random _random = new Random();

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

                // Call CreateEthswichTransaction
                var ethswitchResponse = await _ethswichRepositoryAPI.CreateEthswichTransaction(request.Amount.Value, request.PaymentInformation.Account.Id, request.PaymentInformation.Bank.Id);

                if (ethswitchResponse == null || !ethswitchResponse.success)
                {
                    if (transaction != null)
                    {
                        transaction.status = "Failed";
                        transaction.bankStatusMessage = $"ETSWITCH transaction failed: {ethswitchResponse?.message ?? "No response"}";
                        transaction.requestedExecutionDate = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();
                    }

                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),
                        traceId = request.ReferenceId.ToString(),
                        returnCode = "SB_ETHSWITCH_002",
                        EventDate = DateTime.UtcNow,
                        feedbacks = $"ETSWITCH transaction failed: {ethswitchResponse?.message ?? "No response"}"
                    };

                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_ETHSWITCH_002",
                        Message = $"ETSWITCH transaction failed: {ethswitchResponse?.message ?? "No response"}"
                    };
                }

                // Update transaction status upon successful response
                if (transaction != null)
                {
                    transaction.status = "Success";
                    transaction.bankStatusMessage = "ETSWITCH transaction successful.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    transaction.conversationId = ethswitchResponse.FinInstransactionId;
                    await _dbContext.SaveChangesAsync();
                }


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
                    transaction.bankStatusMessage = "ETSWITCH transaction failed due to an error.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                var errorLog = new ErrorLog
                {
                    ticketId = GenerateRandomString(6),
                    traceId = request.ReferenceId.ToString(),
                    returnCode = "SB_ETHSWITCH_003",
                    EventDate = DateTime.UtcNow,
                    feedbacks = $"Error processing transaction: {ex.Message}"
                };

                _dbContext.ErrorLog.Add(errorLog);
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = false,
                    ErrorCode = "SB_ETHSWITCH_003",
                    Message = $"Error processing transaction: {ex.Message}"
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
