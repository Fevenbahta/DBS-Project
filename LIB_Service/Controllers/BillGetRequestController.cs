using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LIB.API.Persistence.Repositories;
using LIB.API.Domain;
using LIB.API.Application.Contracts.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using LIB.API.Persistence;
using LIB.API.Application.DTOs;
using System.Configuration.Provider;
using Mysqlx.Crud;

namespace LIB.API.Controllers
{
    
    [ApiController]
    [Route("api/v3/BillRequest")]
    public class BillRequestController : ControllerBase
    {
        private readonly IBillGetRequestRepository _billGetRequestRepository;
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IAirlinesOrderService _orderService;

        public BillRequestController(IBillGetRequestRepository billGetRequestRepository, LIBAPIDbSQLContext dbContext, IAirlinesOrderService orderService)
        {
            _billGetRequestRepository = billGetRequestRepository;
            _dbContext = dbContext;
            _orderService = orderService;
        }

        // POST api/billgetrequest
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ProcessTransaction([FromBody] BillGetRequestDto billGetRequestDto)
        {


            var MerchantCode = "526341";
            // Step 1: Validate that the request is not null
            var allowedTypes = new[] { "airlines", "school", "water", "dstv" };

            if (string.IsNullOrEmpty(billGetRequestDto.BillerType) ||
                !allowedTypes.Contains(billGetRequestDto.BillerType.Trim().ToLower()))
            {
                string errorMessage = "Error: BillerType must be one of the following: airlines, school, water, dstv.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.UniqueCode, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (billGetRequestDto == null)
            {
                string errorMessage = "Error: Invalid request data: No data provided.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto?.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
                return BadRequest(errorResponse);
            }

            // Step 2: Conditional validation for PhoneNumber or (ProviderId and UniqueCode)
            if (string.IsNullOrEmpty(billGetRequestDto.PhoneNumber) &&
                (string.IsNullOrEmpty(billGetRequestDto.ProviderId) || string.IsNullOrEmpty(billGetRequestDto.UniqueCode)))
            {
                string errorMessage = "Error: Either PhoneNumber or both ProviderId and UniqueCode are required.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(billGetRequestDto.ReferenceNo))
            {
                string errorMessage = "Error: ReferenceNo is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
                return BadRequest(errorResponse);
            }

            if (billGetRequestDto.ReferenceNo.Length > 50)
            {
                string errorMessage = "Error: ReferenceNo cannot exceed 50 characters.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
                return BadRequest(errorResponse);
            }

        

            if (string.IsNullOrEmpty(billGetRequestDto.AccountNo))
            {
                string errorMessage = "Error: AccountNo is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
                return BadRequest(errorResponse);
            }

            //bool isReferenceNoUnique = await _billGetRequestRepository.IsReferenceNoUniqueAsync(billGetRequestDto.ReferenceNo);
            //if (!isReferenceNoUnique)
            //{
            //    string errorMessage = "Error: ReferenceNo must be unique.";
            //    var errorResponse = await SaveErrorToAirlinesErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "ProcessTransaction");
            //    return BadRequest(errorResponse);
            //}


            bool isReferenceNoUnique;
            if (billGetRequestDto.BillerType == "airlines")
            {
                isReferenceNoUnique = await _orderService.IsReferenceNoUniqueAsync(billGetRequestDto.ReferenceNo);
                if (!isReferenceNoUnique)
                {
                    string errorMessage = "Error: ReferenceNo must be unique.";
                    var errorResponse = await SaveErrorToAirlinesErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "CreatePaymentRequest");
                    return BadRequest(errorResponse);
                }

            }
            else
            {

                isReferenceNoUnique = await _billGetRequestRepository.IsReferenceNoUniqueAsync(billGetRequestDto.ReferenceNo);
                if (!isReferenceNoUnique)
                {
                    string errorMessage = "Error: ReferenceNo must be unique.";
                    var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "Validation", "CreatePaymentRequest");
                    return BadRequest(errorResponse);
                }


            }


            try
            {

                if (billGetRequestDto.BillerType?.ToLower() == "airlines")
                {
                    var orderRequest = new OrderRequestDto
                    {
                        OrderId = billGetRequestDto.UniqueCode, // Adjust based on actual mapping
                        ReferenceId = billGetRequestDto.ReferenceNo, // Adjust based on actual mapping
                           BillerType = billGetRequestDto.BillerType, // Adjust based on actual mapping
                        PhoneNumber = billGetRequestDto.PhoneNumber,
                           AccountNo = billGetRequestDto.AccountNo, // Adjust based on actual mapping
                            };

                    var orderResponse = await _orderService.FetchOrderAsync(orderRequest);

                    if (orderResponse == null)
                    {
                        string errorMessage = "Order not found or failed to fetch.";
                        var errorResponse = await SaveErrorToAirlinesErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "OrderFetch", "ProcessTransaction");
                        return NotFound(errorResponse);
                    }

                    // Check the order status and handle accordingly
                    if (orderResponse.StatusCodeResponse != 1)
                    {
                        string errorMessage = $"Order is {orderResponse.StatusCodeResponseDescription}.";
                        var errorResponse = await SaveErrorToAirlinesErrorAsync(billGetRequestDto.ReferenceNo, errorMessage, "OrderStatus", "ProcessTransaction");
                        return BadRequest(errorResponse);
                    }


                    if (orderResponse.StatusCodeResponse == 1)

                    {
                        List<BillGetResponseDto> responseList = new List<BillGetResponseDto>();

                        responseList.Add(new BillGetResponseDto
                        {
                            Status = orderResponse.StatusCodeResponse.ToString(),
                            ProviderId = MerchantCode,
                            InvoiceId = orderResponse.TraceNumber,
                            InvoiceIdentificationValue = orderResponse.LionTransactionNo,
                            InvoiceAmount = orderResponse.Amount,
                            CurrencyAlphaCode = "001",
                            CurrencyDesignation = "ETB",
                            CustomerName = orderResponse.CustomerName,
                            CustomerCode = orderResponse.OrderId,
                            ProviderName = orderResponse.MerchantName,
                          
                        });
                        return Ok(responseList);

                    }

                }


                // Call the repository method to process the transaction
                var response = await _billGetRequestRepository.ProcessTransactionAsync(billGetRequestDto);

                // Return the processed response data
                return Ok(response); // ✅ Return processed data
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error: Internal server error: {ex.Message}";
                var errorResponse = await SaveErrorToBillErrorAsync(billGetRequestDto?.ReferenceNo, errorMessage, "Exception", "ProcessTransaction");
                return StatusCode(500, errorResponse);
            }
        }

        private async Task<object> SaveErrorToAirlinesErrorAsync(string referenceNo, string errorMessage, string errorType, string reference)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = "Controller",  // Log the method name where the error occurred
                Origin = errorType,  // This is where the error happened
                SpanId = referenceNo,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in controller for ReferenceNo: {referenceNo}" }
        }
            };

            // Serialize the feedback object to JSON or a custom format
            string feedbackJson = JsonConvert.SerializeObject(feedback);
            var errorRecord = new AirlinesError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = reference,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _dbContext.airlineserror.Add(errorRecord);
            await _dbContext.SaveChangesAsync();

            // Return the error structure that will be used in the response
            var response = new
            {
                returnCode = "ERROR",
                ticketId = errorRecord.TicketId,
                traceId = reference,
                feedbacks = new List<object> { feedback }
            };

            return ( response);

        }
        private async Task<object> SaveErrorToBillErrorAsync(string orderId, string errorMessage, string errorType, string reference)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = "Controller",  // Log the method name where the error occurred
                Origin = errorType,  // This is where the error happened
                SpanId = orderId,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in controller for OrderId: {orderId}" }
        }
            };

            // Serialize the feedback object to JSON
            string feedbackJson = JsonConvert.SerializeObject(feedback);
            var errorRecord = new BillError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = reference,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _dbContext.billerror.Add(errorRecord);
            await _dbContext.SaveChangesAsync();

            var response = new
            {
                returnCode = "ERROR",
                ticketId = errorRecord.TicketId,
                traceId = reference,
                feedbacks = new List<object> { feedback }
            };

            return (response);  // Explicitly return HTTP 500
        }


    }
}
