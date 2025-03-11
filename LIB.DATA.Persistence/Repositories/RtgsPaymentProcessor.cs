using System;
using System.Text;
using System.Threading.Tasks;
using IRepository;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using LIB.API.Persistence.Repositories.ExternalAPI;
using Microsoft.EntityFrameworkCore; // Ensure this is included if using EF Core

namespace LIB.API.Persistence.Repositories
{
    public class RtgsPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IRtgRepositoryAPI _RtgsRepositoryAPI;

        public RtgsPaymentProcessor(LIBAPIDbSQLContext dbContext, IRtgRepositoryAPI RtgsRepositoryAPI)
        {
            _dbContext = dbContext;
           _RtgsRepositoryAPI = RtgsRepositoryAPI;
        }
        private static readonly Random _random = new Random();
    

        public async Task<Response> ProcessPaymentAsyncRtgs(TransferRequest request, bool simulationIndicator,string account,string name)
        {
            var transaction = await _dbContext.Transaction
                .Where(t => t.referenceId == request.ReferenceId)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            try
            {
                if (transaction == null)
                {
                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),  // Generate a random ticket ID for tracking
                        traceId = request.ReferenceId.ToString(),  // The reference ID for the transaction
                        returnCode = "SB_Rtgs_001",  // The error code indicating no response from RTGS
                        EventDate = DateTime.UtcNow,  // Time when the error occurred
                        feedbacks = "Rtgs transaction response is null."  // Description of the error
                    };

                    // Add the error log entry to the database
                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_Rtgs_001",
                        Message = "Rtgs transaction response is null."
                    };
                }
                string originatorRef = $"RT{DateTime.Now:ddMMyyyy}_{transaction.Id}";

                var rtgTransactionRequest = new RtgTransactionRequest
                { AppCode = "MBK",
                    AppPassphrase = "FPWsnPmOYC/9jHfMzsKfuw==",
                    Amount = request.Amount.Value,
                    DebitAccount = account,
                       AccName = name,
                     DestAccount = request.PaymentInformation.Account.Id,
                   DestAccName = request.Payee.Contact.Name,
                    DestBankSwiftCode = request.PaymentInformation.Bank.Id,
                    Remarks = "From DBS",
                    OriginatorRef= originatorRef,
                    // Add any additional required fields from TransferRequest here
                };


        // Call CreateRtgsTransfer and process the response
        var RtgsResponse = await _RtgsRepositoryAPI.CreateRtgsTransfer(rtgTransactionRequest);


                if (RtgsResponse == null)
                {
                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_Rtgs_001",
                        Message = "Rtgs transaction response is null."
                    };
                }

                if (!RtgsResponse.success)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = $"Rtgs transaction failed: {RtgsResponse.message}";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),
                        traceId = request.ReferenceId.ToString(),
                        returnCode = "SB_Rtgs_002",
                        EventDate = DateTime.UtcNow,
                        feedbacks = $"Rtgs transaction failed: {RtgsResponse.message}"
                    };

                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_Rtgs_002",
                        Message = $"Rtgs transaction failed: {RtgsResponse.message}"
                    };
                }

                // Update transaction status upon successful response
                transaction.status = "Success";
                transaction.bankStatusMessage = "Rtgs transaction successful.";
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.requestedExecutionDate = DateTime.UtcNow;
                    transaction.conversationId = RtgsResponse.FinInstransactionId;

              
            
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = true,
                    Data = new TransferPostResponseBody
                    {
                        Id = request.ReferenceId,
                        Status = "Rtgs transaction successful."
                    }
                };
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = "Rtgs transaction failed.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                var errorLog = new ErrorLog
                {
                    ticketId = GenerateRandomString(6),
                    traceId = request.ReferenceId.ToString(),
                    returnCode = "SB_DS_003",
                    EventDate = DateTime.UtcNow,
                    feedbacks = $"Error processing rtgs transaction: {ex.Message}"
                };

                _dbContext.ErrorLog.Add(errorLog);
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = false,
                    ErrorCode = "SB_DS_003",
                    Message = $"Error processing rtgs transaction : {ex.Message}"
                };
            }
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
        public async Task<Response> ProcessPaymentAsync(TransferRequest request, bool simulationIndicator)
        {
            return new Response
            {
                IsSuccess = false,
                ErrorCode = "SB_Rtgs_001",
                Message = "Rtgs transaction response is null."
            };
        }

    }
}
