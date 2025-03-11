using System;
using System.Text;
using System.Threading.Tasks;
using IRepository;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore; // Ensure this is included if using EF Core

namespace LIB.API.Persistence.Repositories
{
    public class AwachPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IAwachRepositoryAPI _awachRepositoryAPI;

        public AwachPaymentProcessor(LIBAPIDbSQLContext dbContext, IAwachRepositoryAPI awachRepositoryAPI)
        {
            _dbContext = dbContext;
           _awachRepositoryAPI = awachRepositoryAPI;
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

                }

                // Call CreateAwachTransfer and process the response
                var awachResponse = await _awachRepositoryAPI.CreateAwachTransfer(request.Amount.Value, request.PaymentInformation.Account.Id);

                if (awachResponse == null)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_AWACH_001",
                        Message = "AWACH transaction response is null."
                    };
                }

                if (!awachResponse.success)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = $"AWACH transaction failed: {awachResponse.message}";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),
                        traceId = request.ReferenceId.ToString(),
                        returnCode = "SB_AWACH_002",
                        EventDate = DateTime.UtcNow,
                        feedbacks = $"AWACH transaction failed: {awachResponse.message}"
                    };

                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_AWACH_002",
                        Message = $"AWACH transaction failed: {awachResponse.message}"
                    };
                }

                // Update transaction status upon successful response
                transaction.status = "Success";
                transaction.bankStatusMessage = "AWACH transaction successful.";
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.requestedExecutionDate = DateTime.UtcNow;
                    transaction.conversationId = awachResponse.FinInstransactionId;

              
            
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = true,
                    Data = new TransferPostResponseBody
                    {
                        Id = request.ReferenceId,
                        Status = "AWACH transaction successful."
                    }
                };
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = "AWACH transaction failed.";
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
