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
    public class HelloCashPaymentProcessor : IPaymentProcessor
    {
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IHellocashRepositoryAPI _HellocashsRepositoryAPI;

        public HelloCashPaymentProcessor(LIBAPIDbSQLContext dbContext, IHellocashRepositoryAPI HellocashsRepositoryAPI)
        {
            _dbContext = dbContext;
            _HellocashsRepositoryAPI = HellocashsRepositoryAPI;
        }
        private static readonly Random _random = new Random();


        public async Task<Response> ProcessPaymentAsyncRtgs(TransferRequest request, bool simulationIndicator, string account, string name)
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
                        returnCode = "SB_Hellocashs_001",  // The error code indicating no response from HellocashS
                        EventDate = DateTime.UtcNow,  // Time when the error occurred
                        feedbacks = "Hellocashs transaction response is null."  // Description of the error
                    };

                    // Add the error log entry to the database
                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_Hellocashs_001",
                        Message = "Hellocashs transaction response is null."
                    };
                }
                string originatorRef = $"RT{DateTime.Now:ddMMyyyy}_{transaction.Id}";

                var HellocashTransactionRequest = new HellocashTransactionRequest
                {
                    AppCode = "MBK",
                    AppPassphrase = "FPWsnPmOYC/9jHfMzsKfuw==",
                    Amount = request.Amount.Value,
                    DebitAccount = account,
                    AccName = name,
                    DestAccount = request.PaymentInformation.Account.Id,
                    DestAccName = request.Payee.Contact.Name,
                    DestBankSwiftCode = request.PaymentInformation.Bank.Id,
                    Remarks = "From DBS",
                    OriginatorRef = originatorRef,
                    // Add any additional required fields from TransferRequest here
                };


                // Call CreateHellocashsTransfer and process the response
                var HellocashsResponse = await _HellocashsRepositoryAPI.CreateHellocashsTransfer(HellocashTransactionRequest);


                //if (HellocashsResponse == null)
                //{
                //    return new Response
                //    {
                //        IsSuccess = false,
                //        ErrorCode = "SB_Hellocashs_001",
                //        Message = "Hellocashs transaction response is null."
                //    };
                //}

                if (HellocashsResponse.success)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = $"Hellocashs transaction failed: {HellocashsResponse.message}";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    var errorLog = new ErrorLog
                    {
                        ticketId = GenerateRandomString(6),
                        traceId = request.ReferenceId.ToString(),
                        returnCode = "SB_Hellocashs_002",
                        EventDate = DateTime.UtcNow,
                        feedbacks = $"Hellocashs transaction failed: {HellocashsResponse.message}"
                    };

                    _dbContext.ErrorLog.Add(errorLog);
                    await _dbContext.SaveChangesAsync();

                    return new Response
                    {
                        IsSuccess = false,
                        ErrorCode = "SB_Hellocashs_002",
                        Message = $"Hellocashs transaction failed: {HellocashsResponse.message}"
                    };
                }

                // Update transaction status upon successful response
                transaction.status = "Success";
                transaction.bankStatusMessage = "Hellocashs transaction successful.";
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.requestedExecutionDate = DateTime.UtcNow;
                transaction.conversationId = HellocashsResponse.FinInstransactionId;



                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = true,
                    Data = new TransferPostResponseBody
                    {
                        Id = request.ReferenceId,
                        Status = "Hellocashs transaction successful."
                    }
                };
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.status = "Failed";
                    transaction.bankStatusMessage = "Hellocashs transaction failed.";
                    transaction.requestedExecutionDate = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                }

                var errorLog = new ErrorLog
                {
                    ticketId = GenerateRandomString(6),
                    traceId = request.ReferenceId.ToString(),
                    returnCode = "SB_DS_003",
                    EventDate = DateTime.UtcNow,
                    feedbacks = $"Error processing Hellocashs transaction: {ex.Message}"
                };

                _dbContext.ErrorLog.Add(errorLog);
                await _dbContext.SaveChangesAsync();

                return new Response
                {
                    IsSuccess = false,
                    ErrorCode = "SB_DS_003",
                    Message = $"Error processing Hellocashs transaction : {ex.Message}"
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
                ErrorCode = "SB_Hellocashs_001",
                Message = "Hellocashs transaction response is null."
            };
        }

    }
}
