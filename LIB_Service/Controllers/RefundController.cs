
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    using global::LIB.API.Application.Contracts.Persistence.LIB.API.Repositories;
    using global::LIB.API.Domain;
using Microsoft.AspNetCore.Authorization;

    namespace LIB.API.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class RefundController : ControllerBase
        {
            private readonly IRefundRepository _refundRepository;

            public RefundController(IRefundRepository refundRepository)
            {
                _refundRepository = refundRepository;
            }
      
        
        [AllowAnonymous]
        [HttpPost("ProcessRefund")]
            public async Task<IActionResult> ProcessRefund([FromBody] RefundRequest request)
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        ResponseCode = 0,
                        ResponseCodeDescription = "Invalid request body",
                        Status = "Failure"
                    });
                }

            List<string> errorMessages = new List<string>();

            // Manually validate each property
            if (request.Amount <= 0)
            {
                errorMessages.Add("Amount must be greater than zero.");
            }

            if (string.IsNullOrEmpty(request.Currency) || request.Currency.Length != 3)
            {
                errorMessages.Add("Currency must be a 3-letter code.");
            }

            if (string.IsNullOrEmpty(request.ShortCode))
            {
                errorMessages.Add("ShortCode is required.");
            }

            if (string.IsNullOrEmpty(request.FirstName))
            {
                errorMessages.Add("First Name is required.");
            }

            if (string.IsNullOrEmpty(request.LastName))
            {
                errorMessages.Add("Last Name is required.");
            }

            if (string.IsNullOrEmpty(request.OrderId))
            {
                errorMessages.Add("Order ID is required.");
            }

            if (string.IsNullOrEmpty(request.RefundAccountNumber))
            {
                errorMessages.Add("Refund Account Number is required.");
            }

            if (string.IsNullOrEmpty(request.RefundFOP))
            {
                errorMessages.Add("Refund FOP (Form of Payment) is required.");
            }

            if (string.IsNullOrEmpty(request.RefundReferenceCode))
            {
                errorMessages.Add("Refund Reference Code is required.");
            }

            if (string.IsNullOrEmpty(request.ReferenceNumber))
            {
                errorMessages.Add("Reference Number is required.");
            }

            // If there are any validation errors, return a bad request response with the errors
            if (errorMessages.Any())
            {
                return BadRequest(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = request.ReferenceNumber,
                    feedbacks = new[]
                    {
                    new
                    {
                        code = "SB_DS_001",
                        label = "Validation failed",
                        severity = "ERROR",
                        type = "SYS",
                        source = "Refund Request",
                        origin = "RefundController",
                        spanId = HttpContext.TraceIdentifier,
                        parameters = errorMessages.Select(msg => new { code = "0", value = msg }).ToArray()
                    }
                }
                });
            }

            // Call the repository method to process the refund
            bool isRefundProcessed = await _refundRepository.ProcessRefundAsync(request);

                if (isRefundProcessed)
                {
                    // Success response
                    return Ok(new
                    {
                        ResponseCode = 1,
                        ResponseCodeDescription = "Successfully Refunded",
                        Status = "Success"
                    });
                }
                else
                {
                    // Failure response
                    return Ok(new
                    {
                        ResponseCode = 0,
                        ResponseCodeDescription = "Refund processing failed due to an error",
                        Status = "Failure"
                    });
                }
            }
        }
    }


