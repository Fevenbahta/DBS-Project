using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Domain;
using LIB.API.Interfaces;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using LIB.API.Persistence;
using LIB.API.Persistence.Repositories;
using System.Net;
using System.Net.Http;
using LIB.API.Application.Contracts.Persistence;
using System.DirectoryServices.Protocols;
namespace LIB.API.Services
{
    public class TransferService : ITransferService
    {
        private static readonly ConcurrentDictionary<Guid, TransferPostResponseBody> Transfers = new();
        private static readonly ConcurrentDictionary<Guid, TransferResponseBody> Transfered = new();
        private readonly List<TransferResponseBody> _transfers;
        private static readonly Random _random = new Random();

        private readonly LIBAPIDbSQLContext _dbContext;
        private static readonly HttpClient _httpClient = new HttpClient();


        private readonly PaymentProcessorFactory _paymentProcessorFactory;

        public TransferService(LIBAPIDbSQLContext dbContext, PaymentProcessorFactory paymentProcessorFactory)
        {
            _dbContext = dbContext;
            _paymentProcessorFactory = paymentProcessorFactory;

        }

        public async Task<Response> CreateTransferAsync(TransferRequest request, bool simulationIndicator, string token)
        {
            // Determine credited account based on PaymentScheme
            string creditedAccount = request.PaymentInformation.Account.Id;

            if (request.PaymentInformation.PaymentScheme == "MPESAWALLET")
                creditedAccount = "11510306001";
            else if (request.PaymentInformation.PaymentScheme == "TELEBIRR")
                creditedAccount = "22100300001";
            else if (request.PaymentInformation.PaymentScheme == "ETHSWICH")
                creditedAccount = "11510305000";

            else if (request.PaymentInformation.PaymentScheme == "MPESATRUST")
                creditedAccount = "00112258646";
            else if (request.PaymentInformation.PaymentScheme == "AWACH")
                creditedAccount = "00311925933";
            else if (request.PaymentInformation.PaymentScheme == "FANA")
                creditedAccount = "00310095104";
            else if (request.PaymentInformation.PaymentScheme == "HELLOCASH")
                creditedAccount = "00310095104";

            //var accountApiUrl = $"https://localhost/api/v3/accounts/{request.PaymentInformation.Account.Id}";
            //var accountResponse = await _httpClient.GetStringAsync(accountApiUrl);
            //var accountData = JsonConvert.DeserializeObject<dynamic>(accountResponse);

            //string debitedAccount = accountData?.accountNumber ?? "Debited Account Not Found";
            //string accountHolderName = accountData?.holderName ?? "Name Not Found";


            string debitedAccount = "000040030000706050";
           string accountHolderName = "BINEGA TESFA WELDEGEBRIEL";




            Transaction transaction = null;
            TransactionSimulation transactionsimulation = null;

            // Create a new Transaction record
            if (simulationIndicator) {
                transactionsimulation = new TransactionSimulation
                {
                    accountId = request.AccountId,
                    referenceId = request.ReferenceId,
                    reservationId = Guid.NewGuid(),
                    amount = request.Amount.Value,
                    requestedExecutionDate = request.RequestedExecutionDate,
                    paymentType = request.PaymentInformation.PaymentType,
                    paymentScheme = request.PaymentInformation.PaymentScheme,
                    ReciverAccountId = creditedAccount,
                    ReciverAccountIdType = request.PaymentInformation.Account.IdType,
                    bankId = request.PaymentInformation.Bank.Id,
                    bankIdType = request.PaymentInformation.Bank.IdType,
                    bankName = "Anbesa Bank",
                    status = "Pending",
                    cbsStatusMessage = null,
                    bankStatusMessage = null,
                    //AccountNumber = debitedAccount,
                    //AccountHolderName = accountHolderName,
                };
            }
            else {  
                
                 transaction = new Transaction
            {
                accountId = request.AccountId,
                referenceId = request.ReferenceId,
                reservationId = Guid.NewGuid(),
                amount = request.Amount.Value,
                requestedExecutionDate = request.RequestedExecutionDate,
                paymentType = request.PaymentInformation.PaymentType,
                paymentScheme = request.PaymentInformation.PaymentScheme,
                ReciverAccountId = creditedAccount,
                ReciverAccountIdType = request.PaymentInformation.Account.IdType,
                bankId = request.PaymentInformation.Bank.Id,
                bankIdType = request.PaymentInformation.Bank.IdType,
                bankName = "Anbesa Bank",
                status = "Pending",
                cbsStatusMessage = null,
                bankStatusMessage = null,
                     //AccountNumber = debitedAccount,
                     //AccountHolderName = accountHolderName,
                 };
        }
        
            try
            {

                if (request.PaymentInformation.PaymentScheme == "RTGS")
                {
                    // If the simulationIndicator is true, process with simulation
                    if (simulationIndicator)
                    {
                        transactionsimulation.status = "SUCCESS";
                        transactionsimulation.cbsStatusMessage = "Rtgs Transaction successful (Simulation)";
                        var apiResponse = new TransferPostResponseBody
                        {
                            Id = request.ReferenceId,
                            Status = "RTGS transaction successful(simulation)"
                            // Populate other properties as needed
                        };

                        _dbContext.TransactionSimulation.Add(transactionsimulation);
                        await _dbContext.SaveChangesAsync();

                        return new Response
                        {
                            IsSuccess = true,
                            Data = apiResponse
                        };
                    }
                    else
                    {

          
                        var paymentProcessor = _paymentProcessorFactory.GetPaymentProcessor(request.PaymentInformation.PaymentScheme);
                      var   apiResponseBody = await paymentProcessor.ProcessPaymentAsyncRtgs(request, simulationIndicator, debitedAccount, accountHolderName);




                        if (apiResponseBody.IsSuccess)
                        {

                            transaction.status = "SUCCESS";
                            transaction.cbsStatusMessage = "RTGS Transaction successful";

                            transaction.status = "SUCCESS";
                            transaction.bankStatusMessage = " Transaction successful";
                            _dbContext.Transaction.Add(transaction);
                            await _dbContext.SaveChangesAsync();

                            return new Response
                        {
                            IsSuccess = true,
                            Data = apiResponseBody
                        }; 
                        }
                        else
                        {

                            transaction.status = "Failed";
                            transaction.bankStatusMessage = " Transaction Failed";
                            _dbContext.Transaction.Add(transaction);
                            await _dbContext.SaveChangesAsync();
                            var errorLog = new ErrorLog
                            {
                                ticketId = GenerateRandomString(6),
                                traceId = request.ReferenceId.ToString(),
                                returnCode = "SB_DS_003",
                                EventDate = DateTime.UtcNow,
                                feedbacks = $"Error processing RTGS transaction: ",
                                TransactionType =  "RTGS Real Transaction",
                                TransactionId = ""

                            };

                            _dbContext.ErrorLog.Add(errorLog);
                            await _dbContext.SaveChangesAsync();

                            return new Response
                            {
                                IsSuccess = false,
                                ErrorCode = "SB_DS_003",
                                Message = "Error processing RTGS transaction"
                            };
                        }



                    }

                }


                // Save transaction to the database
                if (simulationIndicator) {     
                    _dbContext.TransactionSimulation.Add(transactionsimulation);
                await _dbContext.SaveChangesAsync(); }
                else {            _dbContext.Transaction.Add(transaction);
                await _dbContext.SaveChangesAsync();}
         


                // Prepare request body for the external API
                var externalTransferRequest = new
                {
                    accountId = request.AccountId,
                    referenceId = request.ReferenceId,
                    amount = new
                    {
                        currency = request.Amount.Currency,
                        value = request.Amount.Value
                    },
                    requestedExecutionDate = request.RequestedExecutionDate,
                    paymentInformation = new
                    {
                        paymentType = request.PaymentInformation.PaymentType,
                        paymentScheme = request.PaymentInformation.PaymentScheme,
                        account = new
                        {
                            id = creditedAccount,
                            idType = request.PaymentInformation.Account.IdType
                        },
                        bank = new
                        {
                            id = request.PaymentInformation.Bank.Id,
                            idType = request.PaymentInformation.Bank.IdType
                        }
                    }
                };

                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri("https://api.anbesabank.et/api/v3/");
                    var requestUrl = $"transfers?simulationIndicator={simulationIndicator.ToString().ToLower()}";
                    var content = new StringContent(JsonConvert.SerializeObject(externalTransferRequest), Encoding.UTF8, "application/json");

                    // Set the Authorization header
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    var httpResponse = await httpClient.PostAsync(requestUrl, content);


                    if (simulationIndicator)
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        try
                        {

                            //          var requestBody = new
                            //{
                            //    accountDebited = debitedAccount,
                            //    accountCredited = request.PaymentInformation.Account.Id,
                            //    amount = request.Amount.Value,
                            //    transactionId = request.ReferenceId,
                            //    Fullname = accountHolderName,
                            //};



                            var requestBody = new
                            {
                                accountDebited = "00312365168",
                                accountCredited = request.PaymentInformation.Account.Id,
                                amount = request.Amount.Value,
                                transactionId = request.ReferenceId,
                                Fullname = "Test"
                            };

                            var jsonRequest = JsonConvert.SerializeObject(requestBody);

                            // Handler to ignore SSL certificate errors (for testing only)
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                            };

                            using (var httpClient1 = new HttpClient(handler))
                            {
                                var content1 = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                                var apiResponse1 = await httpClient1.PostAsync("https://10.1.22.198:3060/generate-receipt", content1);

                                apiResponse1.EnsureSuccessStatusCode(); // Throws an exception if the response is not successful

                                var apiResponseBody1 = await apiResponse1.Content.ReadAsStringAsync();

                                // Deserialize the response
                                var responseObject = JsonConvert.DeserializeObject<dynamic>(apiResponseBody1);

                                // Check if the response message indicates success
                                bool isReceiptSuccessful = responseObject?.message == "Receipt generated successfully";

                                transactionsimulation.receiptStatus = isReceiptSuccessful ? "SUCCESS" : "FAILED";

                                // Handle success or failure here
                                       }
                        }
                        catch (HttpRequestException ex)
                        {
                            Console.WriteLine($"Error processing transaction: {ex.Message}");
                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                            }



                            // Log the error in the database
                            var errorLog = new ErrorLog
                            {
                                ticketId = GenerateRandomString(6),
                                traceId = request.ReferenceId.ToString(),
                                returnCode = "SB_DS_003",
                                EventDate = DateTime.UtcNow,
                                feedbacks = $"Error processing transaction: {ex.InnerException.Message}",
                                TransactionType = simulationIndicator ? "Simulation" : "Real Transaction",
                                TransactionId = ""

                            };

                            _dbContext.ErrorLog.Add(errorLog);
                            await _dbContext.SaveChangesAsync();

                            return new Response
                            {
                                IsSuccess = false,
                                ErrorCode = "SB_DS_003",
                                Message = $"Error processing transaction: {ex.InnerException.Message}"
                            };






                        }


                    }



                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var apiResponse = await httpResponse.Content.ReadAsStringAsync();
                        var apiResponseBody = JsonConvert.DeserializeObject<dynamic>(apiResponse);

                        // Update transaction status in database

                        if (simulationIndicator)
                        {


      // Update the transaction status in the simulation table

                                // Update the transaction status in the simulation table
                                transactionsimulation.status =  "SUCCESS";




                            // Update the transaction status in the simulation table
                            transactionsimulation.status = (string?)apiResponseBody?.returnCode ?? "SUCCESS";
                            transactionsimulation.cbsStatusMessage = "CBS Transaction successful (Simulation)";

                            // Save changes to the simulation table
                            _dbContext.TransactionSimulation.Update(transactionsimulation);
                            await _dbContext.SaveChangesAsync();

                         
                        }
                        else {  
                            transaction.status = (string?)apiResponseBody?.returnCode ?? "SUCCESS";
                        transaction.cbsStatusMessage = "CBS Transaction successful";
                        _dbContext.Transaction.Update(transaction);
                        await _dbContext.SaveChangesAsync();
                       if (request.PaymentInformation.PaymentScheme== "AWACH"|| request.PaymentInformation.PaymentScheme == "MPESAWALLET"||
                                request.PaymentInformation.PaymentScheme == "MPESATRUST"|| request.PaymentInformation.PaymentScheme == "TELEBIRR"||
                                request.PaymentInformation.PaymentScheme == "ETSWICH"|| request.PaymentInformation.PaymentScheme == "HELLOCASH")
                            {
                                var paymentProcessor = _paymentProcessorFactory.GetPaymentProcessor(request.PaymentInformation.PaymentScheme);
                                apiResponseBody = await paymentProcessor.ProcessPaymentAsync(request, simulationIndicator);


                            }

                        }



                        if (apiResponseBody.IsSuccess)
                        {

                         
                            return new Response
                            {
                                IsSuccess = true,
                                Data = apiResponseBody
                            };
                        }
                        else
                        {

                                  var errorLog = new ErrorLog
                            {
                                ticketId = GenerateRandomString(6),
                                traceId = request.ReferenceId.ToString(),
                                returnCode = "SB_DS_003",
                                EventDate = DateTime.UtcNow,
                                feedbacks = $"Error processing transaction: ",
                                TransactionType = " Real Transaction",
                                TransactionId = ""

                            };

                            _dbContext.ErrorLog.Add(errorLog);
                            await _dbContext.SaveChangesAsync();

                            return new Response
                            {
                                IsSuccess = false,
                                ErrorCode = "SB_DS_003",
                                Message = $"Error processing transaction for {request.PaymentInformation.PaymentScheme}:{apiResponseBody.Message}"
                            };
                        }

                    }
                    else
                    {
                        //var apiResponse = await httpResponse.Content.ReadAsStringAsync();
                        //var apiResponseBody = JsonConvert.DeserializeObject<dynamic>(apiResponse);

                        //var paymentProcessor = _paymentProcessorFactory.GetPaymentProcessor(request.PaymentInformation.PaymentScheme);
                        //apiResponseBody = await paymentProcessor.ProcessPaymentAsync(request, simulationIndicator);




                        var errorContent = await httpResponse.Content.ReadAsStringAsync();

                        if (simulationIndicator)
                        {
                            // Update the transaction status in the simulation table
                            transactionsimulation.status = "FAILED";
                            transactionsimulation.cbsStatusMessage = "CBS Transaction failed (Simulation)";

                            // Save changes to the simulation table
                            _dbContext.TransactionSimulation.Update(transactionsimulation);
                            await _dbContext.SaveChangesAsync();


                        }
                        else {
                            transaction.status = "FAILED";
                        transaction.cbsStatusMessage = $"CBS Transaction failed:";
                        _dbContext.Transaction.Update(transaction);
                        await _dbContext.SaveChangesAsync();

                  }





                              var errorObject = JsonConvert.DeserializeObject<dynamic>(errorContent);
                        string ticketId = errorObject?.ticketId ?? "NoTicketId";
                        string traceId = errorObject?.traceId ?? "DefaultTraceId";

                        // Prepare to store feedbacks with mapped parameters
                        var feedbackList = new List<SBCPErrorFeedback>();

                        if (errorObject?.feedbacks != null)
                        {
                            foreach (var f in (IEnumerable<dynamic>)errorObject.feedbacks)
                            {
                                var parameters = new List<SBCPErrorParameter>();

                                if (f.parameters != null)
                                {
                                    foreach (var parameter in (IEnumerable<dynamic>)f.parameters)
                                    {
                                        parameters.Add(new SBCPErrorParameter
                                        {
                                            Code = (string?)parameter.code ?? "0", // Default to "0" if null
                                            Value = (string?)parameter.value ?? "" // Default to "" if null
                                        });
                                    }
                                }

                                feedbackList.Add(new SBCPErrorFeedback
                                {
                                    Code = (string?)f.code ?? "",
                                    Label = (string?)f.label ?? "",
                                    Severity = (string?)f.severity ?? "",
                                    Type = (string?)f.type ?? "",
                                    SpanId = (string?)f.spanId ?? "",
                                    Origin = (string?)f.origin ?? "",
                                    Parameters = parameters // Assign mapped parameters
                                });
                            }
                        }

                        // Serialize feedbacks to JSON string
                        string feedbacks = JsonConvert.SerializeObject(feedbackList) ?? "No feedbacks available";

                        var errorLog = new ErrorLog
                        {
                            ticketId = ticketId,
                            traceId = traceId,
                            returnCode = "SB_DS_009",
                            EventDate = DateTime.UtcNow,
                            feedbacks = feedbacks,
                            TransactionType = simulationIndicator ? "Simulation" : "Real Transaction",
                                                  TransactionId = ""

                        };

                        // Assuming you want to save the errorLog to the database or handle it further
          

                        _dbContext.ErrorLog.Add(errorLog);
                        await _dbContext.SaveChangesAsync();
                        return new Response
                        {
                            IsSuccess = false,
                            ErrorCode = "SB_DS_009",
                            Message = $"External API call failed: {errorContent}",
                            Data = errorContent
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error in the database
                var errorLog = new ErrorLog
                {
                    ticketId = GenerateRandomString(6),
                    traceId = request.ReferenceId.ToString(),
                    returnCode = "SB_DS_003",
                    EventDate = DateTime.UtcNow,
                    feedbacks = $"Error processing transaction: {ex.Message}",
                    TransactionType = simulationIndicator ? "Simulation" : "Real Transaction",
                                          TransactionId = ""

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
        private async Task<string> GetTokenAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var tokenUrl = "https://idp.dbs-cust-delivery.dbsdev.sbcp.io/auth/realms/neo-customer/protocol/openid-connect/token";
                var tokenRequestContent = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("client_id", "service-dbs-transaction"),
            new KeyValuePair<string, string>("client_secret", "7fe6d638a0b88d6a12824547fb843664"),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

                try
                {
                    var response = await httpClient.PostAsync(tokenUrl, tokenRequestContent);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        return tokenResponse?.access_token; // Return the token
                    }
                    else
                    {
                        return null; // Return null if failed to get token
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception or handle as necessary
                    return null;
                }
            }
        }

        public async Task<TransferPostResponseBody> CancelTransferAsync(TransferCancellationRequest request)
        {
            foreach (var key in Transfers.Keys)
            {
                if (Transfers.TryGetValue(key, out var transfer) && transfer.Status == "PENDING")
                {
                    transfer.Status = "CANCELLED";
                    return await Task.FromResult(transfer);
                }
            }

            return null;
        }
        public TransferService()
        {
            // Sample data - In real-world applications, this would be fetched from a database
            _transfers = new List<TransferResponseBody>
        {
            new TransferResponseBody
            {
                Id = Guid.Parse("2368ee8c-f1eb-4015-9f5f-088df298fba2"),
                Status = "ACCEPTED",
                AccountId = Guid.Parse("e79e5aa4-8da1-4f36-83b9-812ea880597b"),
                ReservationId = Guid.Parse("b5cbcc94-9633-4f1f-81a3-001eebd6358b"),
                ReferenceId = Guid.Parse("2368ee8c-f1eb-4015-9f5f-088df298fba2"),
                Amount = new Amount
                {
                    Currency = "EUR",
                    Value = 5000
                },
                ExecutionDate = DateTime.Parse("2023-02-13"),
                Subject = "Money for you",
                Payee = new Payee
                {
                    Contact = new Contact
                    {
                        Name = "John Doe",
                        AddressLine1 = "Main Street 123",
                        AddressLine2 = "Apartment 42",
                        ZipCode = "12345",
                        City = "New York",
                        CountryCode = "DE"
                    },
                    Bank = new Bank
                    {
                        Name = "Maze Bank",
                        AddressLine1 = "Main Street 123",
                        AddressLine2 = "Apartment 42",
                        ZipCode = "12345",
                        City = "New York",
                        CountryCode = "DE",
                        BranchCode = "123456"
                    }
                },
                PaymentInformation = new PaymentInformation
                {
                    PaymentType = "LOCAL_TRANSFER",
                    PaymentScheme = "INTERNAL",
                    Account = new PaymentAccount
                    {
                        Id = "DE33500105179452792191",
                        IdType = "IBAN"
                    },
                    Bank = new BankInformation
                    {
                        Id = "ABAGATWWXXX",
                        IdType = "BIC"
                    }
                },
                CustomFields = new List<CustomField>
                {
                    new CustomField { Name = "Subscription Identifier", Value = "2021-07-16", DataType = "DATE" }
                }
            }
            // Add more sample data as needed
        };
        }

        public async Task<List<TransferResponseBody>> GetTransferStatusAsync(TransferFilterParameters transferFilter)
        {
            // Filter based on the parameters
            //var filteredTransfers = _transfers.AsQueryable();

            //if (transferFilter.AccountId!=null)
            //    filteredTransfers = filteredTransfers.Where(t => t.AccountId == transferFilter.AccountId);

            //if (transferFilter.AmountFrom.HasValue)
            //    filteredTransfers = filteredTransfers.Where(t => t.Amount.Value >= transferFilter.AmountFrom.Value);

            //if (transferFilter.AmountTo.HasValue)
            //    filteredTransfers = filteredTransfers.Where(t => t.Amount.Value <= transferFilter.AmountTo.Value);

            //if (!string.IsNullOrEmpty(transferFilter.Currency))
            //    filteredTransfers = filteredTransfers.Where(t => t.Amount.Currency.Equals(transferFilter.Currency, StringComparison.OrdinalIgnoreCase));

            //if (transferFilter.ExecutionDateFrom!=null)
            //    filteredTransfers = filteredTransfers.Where(t => t.ExecutionDate >= transferFilter.ExecutionDateFrom);
            //if (transferFilter.ExecutionDateTo!=null)
            //    filteredTransfers = filteredTransfers.Where(t => t.ExecutionDate <= transferFilter.ExecutionDateTo);


            //if (transferFilter.Statuses != null && transferFilter.Statuses.Any())
            //    filteredTransfers = filteredTransfers.Where(t => transferFilter.Statuses.Contains(t.Status));

            //// Implement range filtering (pagination logic) here if needed
            //if (!string.IsNullOrEmpty(transferFilter.Range))
            //{
            //    var rangeParts = transferFilter.Range.Split('-');
            //    if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out var startIndex) && int.TryParse(rangeParts[1], out var endIndex))
            //    {
            //        filteredTransfers = filteredTransfers.Skip(startIndex).Take(endIndex - startIndex + 1);
            //    }
            //}

            //return await Task.FromResult(filteredTransfers.ToList());
            return await Task.FromResult(_transfers);

        }
    }

}



















