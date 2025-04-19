using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LIB.API.Domain;
using LIB.API.Persistence.Repositories;
using LIB.API.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using LIB.API.Persistence;
using LIB.API.Application.DTOs;
using Org.BouncyCastle.Ocsp;

namespace LIB.API.Controllers
{
    [ApiController]

    [Route("api/v3/ECPayment")]

    public class ECPaymentController : ControllerBase
    {
        private readonly IECPaymentRepository _ecPaymentRepository;
        private readonly LIBAPIDbSQLContext _dbContext;
        private readonly IAirlinesOrderService _orderService;
        private readonly IConfirmOrderService _confirmOrderService;

        // Inject the repository into the controller
        public ECPaymentController(IECPaymentRepository ecPaymentRepository, LIBAPIDbSQLContext dbContext, IAirlinesOrderService orderService, 
            IConfirmOrderService confirmOrderService)
        {
            _ecPaymentRepository = ecPaymentRepository;
          _dbContext = dbContext;
            _orderService = orderService;
         _confirmOrderService = confirmOrderService;
        }

        // POST api/ecpayment
        [HttpPost]
        public async Task<IActionResult> CreatePaymentRequest([FromBody] ECPaymentRequestDTO request)
        {

            var MerchantCode = "526341";

            if (request == null)
            {
                string errorMessage = "Payment request data is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request?.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }
            var allowedTypes = new[] { "airlines", "school", "water", "dstv" };

            if (string.IsNullOrEmpty(request.BillerType) ||
                !allowedTypes.Contains(request.BillerType.Trim().ToLower()))
            {
                string errorMessage = "Error: BillerType must be one of the following: airlines, school, water, dstv.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            // Manually validate the fields in request
            if (request.PaymentAmount <= 0)
            {
                string errorMessage = "Error: PaymentAmount must be a positive integer.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.InvoiceId))
            {
                string errorMessage = "Error: InvoiceId is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.CustomerId))
            {
                string errorMessage = "Error: CustomerId is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.ReferenceNo))
            {
                string errorMessage = "Error: ReferenceNo is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.ProviderId) && request.ProviderId!= "Airlines")
            {
                string errorMessage = "Error: BillerId is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }
            if (string.IsNullOrEmpty(request.CustomerCode) && request.ProviderId != "Airlines")
            {
                string errorMessage = "Error: CustomerCode is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.Branch))
            {
                string errorMessage = "Error: Branch is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            //if (request.PaymentDate == default(DateTime))
            //{
            //    string errorMessage = "Error: PaymentDate is required and must be a valid date.";
            //    var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
            //    return BadRequest(errorResponse);
            //}

            if (string.IsNullOrEmpty(request.AccountNo))
            {
                string errorMessage = "Error: AccountNo is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }
            bool isReferenceNoUnique;
            if (request.BillerType == "airlines")
            {
                 isReferenceNoUnique = await _confirmOrderService.IsReferenceNoUniqueAsync(request.ReferenceNo);
                          if (!isReferenceNoUnique)
            {
                string errorMessage = "Error: ReferenceNo must be unique.";
                var errorResponse = await SaveErrorToAirlinesErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }
            
            }
            else
            {

                  isReferenceNoUnique = await _ecPaymentRepository.IsReferenceNoUniqueAsync(request.ReferenceNo);
                        if (!isReferenceNoUnique)
            {
                string errorMessage = "Error: ReferenceNo must be unique.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }


            }
         

             
            try
            {




                if (request.BillerType?.ToLower() == "airlines")
                {
                    var orderRequest = new OrderRequestDto
                    {
                        OrderId = request.CustomerCode, // Adjust based on actual mapping
                        ReferenceId = request.ReferenceNo // Adjust based on actual mapping
                    };

                    var orderResponse = await _orderService.FetchOrderAsync(orderRequest);

                    if (orderResponse == null)
                    {
                        string errorMessage = "Order not found or failed to fetch.";
                        var errorResponse = await SaveErrorToAirlinesErrorAsync(request.ReferenceNo, errorMessage, "OrderFetch", "ProcessTransaction");
                        return NotFound(errorResponse);
                    }

                    // Check the order status and handle accordingly
                    if (orderResponse.StatusCodeResponse ==10 )
                    {
                        string errorMessage = $"Order  {orderResponse.StatusCodeResponseDescription}.";
                        var errorResponse = await SaveErrorToAirlinesErrorAsync(request.ReferenceNo, errorMessage, "OrderStatus", "ProcessTransaction");
                        return BadRequest(errorResponse);
                    }


                    if (orderResponse.StatusCodeResponse !=10)

                    {
                        try
                        {
                            // Proceed with transfer
                            var responses = await _confirmOrderService.CreateTransferAsync(
                                request.PaymentAmount,
                                request.AccountNo,
                                request.CustomerCode,
                                request.ReferenceNo,
                                request.InvoiceId
                            );

                            return Ok(responses);
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorToAirlinesErrorAsync(request.CustomerCode, ex.Message, "CreateTransfer", request.ReferenceNo);

                             }

                    }


                }








                var (status, response) = await _ecPaymentRepository.CreateAndSendSoapRequestAsync(request);
                if (status == "Error")
                {
                      return BadRequest( response);


                }
                var result = new
                {
                    status = "Sucess",
                    response = response
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                var errorResponse = await SaveErrorToBillErrorAsync(request?.InvoiceId, errorMessage, "Exception", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }
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

            return (response);

        }

    }
}
