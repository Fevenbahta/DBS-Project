using LIB.API.Domain;
using LIB.API.Interfaces;
using LIB.API.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Controllers
{
    [ApiController]
    [Route("api/v3/transfers")]
    public class TransferController : ControllerBase
    {
        private readonly ITransferService _transferService;
        private readonly LIBAPIDbSQLContext _dbContext;
        private static readonly Random _random = new Random();
        public TransferController(ITransferService transferService, LIBAPIDbSQLContext dbContext)
        {
            _dbContext = dbContext;
            _transferService = transferService;
        }


      


        // 1️⃣ CREATE TRANSFER API
        [HttpPost]


        public async Task<IActionResult> CreateTransfer(
 
           [FromQuery] bool simulationIndicator,
           [FromBody] TransferRequest transferRequest)
        {
            // Validate the authorization token
            if (!HttpContext.Request.Headers.TryGetValue("Authorization", out var authorization) ||
            string.IsNullOrEmpty(authorization) || !authorization.ToString().StartsWith("Bearer "))
            {
                await LogInvalidRequestAndError(transferRequest, "SB_DS_001", "Authorization token is missing or invalid",simulationIndicator);

                var errorResponse = CreateErrorResponse("SB_DS_001", "Authorization token is missing or invalid", "Authorization");
                return StatusCode(403, errorResponse);
            }

            // Extract the token
            var token = authorization.ToString().Substring("Bearer ".Length).Trim();


            if (transferRequest == null)
            {
                await LogInvalidRequestAndError(null, "SB_DS_001", "Transfer request is null", simulationIndicator);

                return BadRequest(CreateErrorResponse("SB_DS_001", "Transfer request is null", "TransferRequest"));
            }

            if (!ModelState.IsValid)
            {
                var missingFields = new List<string>();

                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        missingFields.Add(state.Key);
                    }
                }
                var errorMessage = $"Missing or invalid fields: {string.Join(", ", missingFields)}";

                await LogInvalidRequestAndError(transferRequest, "SB_DS_002", errorMessage, simulationIndicator);

                return BadRequest(CreateErrorResponse("SB_DS_002", $"Missing or invalid fields: {string.Join(", ", missingFields)}", "TransferRequest"));
            }

            try
            {
                // Optionally, validate the token here if needed
                // For example, you might want to call a service to validate the token

                // Call the service to create the transfer
                var transferResponse = await _transferService.CreateTransferAsync(transferRequest, simulationIndicator, token);

                // Handle successful response from service
                if (transferResponse.IsSuccess && transferResponse.Data != null)
                {
                    // Deserialize the nested Data property into TransferPostResponseBody
                    var jsonData = JsonConvert.SerializeObject(transferResponse.Data);
                    var responseBody = JsonConvert.DeserializeObject<Response>(jsonData);

                    var innerDataJson = JsonConvert.SerializeObject(responseBody.Data);
                    var innerData = JsonConvert.DeserializeObject<TransferPostResponseBody>(innerDataJson);

                    var successResponse = new
                    {
                        id = innerData.Id,
                        status = innerData.Status
                    };

                    return CreatedAtAction(nameof(CreateTransfer), new { id = successResponse.id }, successResponse);
                }
             else if (!(transferResponse.IsSuccess) && transferResponse.ErrorCode == "SB_DS_009")
{
    // Deserialize transferResponse.Data if it's a JSON string
    var errorObject = JsonConvert.DeserializeObject<dynamic>(transferResponse.Data?.ToString() ?? "{}");

    // Define a concrete list type for feedbacks
    var feedbackList = new List<SBCPErrorFeedback>(); // Use the concrete type here

    if (errorObject?.feedbacks != null)
    {
        foreach (var f in (IEnumerable<dynamic>)errorObject.feedbacks)
        {
            var parameters = new List<SBCPErrorParameter>(); // Use SBCPErrorParameter here

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
                Parameters = parameters // Use the list of SBCPErrorParameter
            });
        }
    }

    // Construct the final response
    var errorResponse = new
    {
        returnCode = (string?)errorObject?.returnCode ?? "ERROR",
        ticketId = (string?)errorObject?.ticketId ?? "",
        traceId = (string?)errorObject?.traceId ?? "",
        feedbacks = feedbackList
    };

    return StatusCode(500, errorResponse);
}
   else
                {
                    return StatusCode(500, CreateErrorResponse("SB_DS_003", transferResponse.Message, "TransferService"));
                }
            }
            catch (Exception ex)
            {
                // Catch and return exception response
                return StatusCode(500, CreateErrorResponse("SB_DS_003", ex.Message, "TransferService"));
            }
        }

        private SBCPErrorResponseBody CreateErrorResponse(string code, string message, string source)
        {
            return new SBCPErrorResponseBody
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = Guid.NewGuid().ToString(),
                Feedbacks = new List<SBCPErrorFeedback>
        {
            new SBCPErrorFeedback
            {
                Code = code,
                Label = message,
                Severity = "ERROR",
                Type = "BUS",
                Source = source,
                Origin = "API",
                SpanId = "92e9013d",
                Parameters = new List<SBCPErrorParameter>
                {
                    new SBCPErrorParameter { Code = "0", Value = "N/A" }
                }
            }
        }
            };
        }


        // 2️⃣ CANCEL TRANSFER API
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelTransfer([FromBody] TransferCancellationRequest cancellationRequest)
        {
            if (cancellationRequest == null || cancellationRequest.Status != "CANCELLED")
            {
                var errorResponse = new SBCPErrorResponseBody
                {
                    ReturnCode = "ERROR",
                    TicketId = Guid.NewGuid().ToString(),
                    TraceId = Guid.NewGuid().ToString(),
                    Feedbacks = new List<SBCPErrorFeedback>
                    {
                        new SBCPErrorFeedback
                        {
                            Code = "SB_DS_001",
                            Label = "Invalid cancellation request",
                            Severity = "ERROR",
                            Type = "BUS",
                            Source = "CancellationRequest",
                            Origin = "API",
                            SpanId = "92e9013d",
                            Parameters = new List<SBCPErrorParameter>
                            {
                                new SBCPErrorParameter { Code = "0", Value = "Status must be CANCELLED" }
                            }
                        }
                    }
                };
                return BadRequest(errorResponse);
            }

            try
            {
                var result = await _transferService.CancelTransferAsync(cancellationRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorResponse = new SBCPErrorResponseBody
                {
                    ReturnCode = "ERROR",
                    TicketId = Guid.NewGuid().ToString(),
                    TraceId = Guid.NewGuid().ToString(),
                    Feedbacks = new List<SBCPErrorFeedback>
                    {
                        new SBCPErrorFeedback
                        {
                            Code = "SB_DS_001",
                            Label = ex.Message,
                            Severity = "ERROR",
                            Type = "SYS",
                            Source = "TransferService",
                            Origin = "API",
                            SpanId = "92e9013d",
                            Parameters = new List<SBCPErrorParameter>
                            {
                                new SBCPErrorParameter { Code = "0", Value = "N/A" }
                            }
                        }
                    }
                };
                return StatusCode(500, errorResponse);
            }
        }






        [HttpGet]
        public async Task<IActionResult> GetTransferStatus(
[FromQuery] Guid? accountId,
[FromQuery] decimal? amountFrom = null,
[FromQuery] decimal? amountTo = null,
[FromQuery] string? currency = null,
[FromQuery] DateTime? executionDateFrom = null, // ✅ Nullable
[FromQuery] DateTime? executionDateTo = null, // ✅ Nullable
[FromQuery] string? range = null,
[FromQuery] List<string>? statuses = null)
        {
            // Manual validation for AccountId if it's not valid
            if (accountId == Guid.Empty || !Guid.TryParse(accountId.ToString(), out _))
            {
                ModelState.AddModelError("accountId", "AccountId is required and must be a valid GUID.");
            }


            if (!ModelState.IsValid)
            {
                var missingFields = new List<string>();

                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        missingFields.Add(state.Key);
                    }
                }

                return BadRequest(CreateErrorResponse("SB_DS_002", $"Missing or invalid fields: {string.Join(", ", missingFields)}", "TransferRequest"));
            }
            try
            {
                var transferFilter = new TransferFilterParameters
                {
                    AccountId = accountId,
                    AmountFrom = amountFrom,
                    AmountTo = amountTo,
                    Currency = currency,
                    ExecutionDateFrom = executionDateFrom,
                    ExecutionDateTo = executionDateTo,
                    Range = range,
                    Statuses = statuses ?? new List<string>()
                };

                var transferStatus = await _transferService.GetTransferStatusAsync(transferFilter);

                if (transferStatus == null)
                {
                    return NotFound(new
                    {
                        returnCode = "ERROR",
                        ticketId = Guid.NewGuid().ToString(),
                        traceId = Guid.NewGuid().ToString(),
                        feedbacks = new List<object>
                {
                    new
                    {
                        code = "SB_DS_001",
                        label = "No transfers found.",
                        severity = "ERROR",
                        type = "BUS",
                        source = "GetTransferStatus",
                        origin = "API",
                        spanId = "92e9013d",
                        parameters = new List<object>
                        {
                            new { code = "0", value = "N/A" }
                        }
                    }
                }
                    });
                }

                return Ok(new { Status = "SUCCESS", Transfers = transferStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = Guid.NewGuid().ToString(),
                    feedbacks = new List<object>
            {
                new
                {
                    code = "SB_DS_002",
                    label = ex.Message,
                    severity = "ERROR",
                    type = "SYS",
                    source = "GetTransferStatus",
                    origin = "API",
                    spanId = "92e9013d",
                    parameters = new List<object>
                    {
                        new { code = "0", value = "N/A" }
                    }
                }
            }
                });
            }
        }



        private async Task LogInvalidRequestAndError(TransferRequest request, string errorCode, string errorMessage,bool simulationIndicator)
        {
            var requestId = Guid.NewGuid().ToString(); // Unique request identifier

            // Save the request details
            var errorLog = new ErrorLog
            {
                ticketId = GenerateRandomString(6),
                traceId = request.ReferenceId.ToString(),
                returnCode = "SB_DS_003",
                EventDate = DateTime.UtcNow,
                feedbacks = $"Error Invalid Request: {errorMessage}",
                TransactionType= simulationIndicator?"Simulation":"Real Transaction",
                      TransactionId = ""
            };

            _dbContext.ErrorLog.Add(errorLog);
            await _dbContext.SaveChangesAsync();



            Transaction transaction = null;
            TransactionSimulation transactionsimulation = null;

            // Create a new Transaction record
            if (simulationIndicator)
            {
                transactionsimulation = new TransactionSimulation
                {
                    accountId = request.AccountId,
                    referenceId = request.ReferenceId,
                    reservationId = Guid.NewGuid(),
                    amount = request.Amount.Value,
                    requestedExecutionDate = request.RequestedExecutionDate,
                    paymentType = request.PaymentInformation.PaymentType,
                    paymentScheme = request.PaymentInformation.PaymentScheme,
                    ReciverAccountId = request.PaymentInformation.PaymentScheme,
                    ReciverAccountIdType = request.PaymentInformation.Account.IdType,
                    bankId = request.PaymentInformation.Bank.Id,
                    bankIdType = request.PaymentInformation.Bank.IdType,
                    bankName = request.Payee.Bank.Name,
                    status = errorMessage,
                    cbsStatusMessage = null,
                    bankStatusMessage = null
                };
            }
            else
            {

                transaction = new Transaction
                {
                    accountId = request.AccountId,
                    referenceId = request.ReferenceId,
                    reservationId = Guid.NewGuid(),
                    amount = request.Amount.Value,
                    requestedExecutionDate = request.RequestedExecutionDate,
                    paymentType = request.PaymentInformation.PaymentType,
                    paymentScheme = request.PaymentInformation.PaymentScheme,
                    ReciverAccountId = request.PaymentInformation.PaymentScheme,
                    ReciverAccountIdType = request.PaymentInformation.Account.IdType,
                    bankId = request.PaymentInformation.Bank.Id,
                    bankIdType = request.PaymentInformation.Bank.IdType,
                    bankName = request.Payee.Bank.Name,
                    status = errorMessage,
                    cbsStatusMessage = null,
                    bankStatusMessage = null
                };
            }

          // Save transaction to the database
                if (simulationIndicator)
                {
                    _dbContext.TransactionSimulation.Add(transactionsimulation);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    _dbContext.Transaction.Add(transaction);
                    await _dbContext.SaveChangesAsync();
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

    }
}

