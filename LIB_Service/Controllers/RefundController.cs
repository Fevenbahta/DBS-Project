
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

    using global::LIB.API.Application.Contracts.Persistence.LIB.API.Repositories;
    using global::LIB.API.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]  // Ensures the endpoint requires a valid token

        [HttpPost("ProcessRefund")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequest request)
        {
            try
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

                // Validate each property
                if (request.Amount <= 0) errorMessages.Add("Invalid Amount");
                if (string.IsNullOrEmpty(request.Currency) || request.Currency.Length != 3) errorMessages.Add("Currency must be a 3-letter code.");
                if (string.IsNullOrEmpty(request.FirstName)) errorMessages.Add("First Name is required.");
                if (string.IsNullOrEmpty(request.LastName)) errorMessages.Add("Last Name is required.");
                if (string.IsNullOrEmpty(request.OrderId)) errorMessages.Add("Order ID is required.");
                if (string.IsNullOrEmpty(request.RefundFOP)) errorMessages.Add("Refund FOP (Form of Payment) is required.");
                if (string.IsNullOrEmpty(request.RefundReferenceCode)) errorMessages.Add("Refund Reference Code is required.");
                if (string.IsNullOrEmpty(request.ReferenceNumber)) errorMessages.Add("Reference Number is required.");

                bool isReferenceNoUnique = await _refundRepository.IsReferenceNoUniqueAsync(request.RefundReferenceCode);
                if (!isReferenceNoUnique) errorMessages.Add("Error: Request Already Exist.");

                // If validation fails, return structured error response
                if (errorMessages.Any())
                {
                    return BadRequest(new
                    {
                        ResponseCode = 0,
                        ResponseCodeDescription = string.Join("; ", errorMessages), // Combine errors into a single string
                        Status = "Failure"
                    });
                }

                // Process the refund
                bool isRefundProcessed = await _refundRepository.ProcessRefundAsync(request);
                return Ok(new
                {
                    ResponseCode = isRefundProcessed ? 1 : 0,
                    ResponseCodeDescription = isRefundProcessed ? "Successfully Refunded" : "Refund processing failed due to an error",
                    Status = isRefundProcessed ? "Success" : "Failure"
                });
            }
            catch (Exception ex)
            {
                // Log the error properly
                Console.WriteLine($"Exception in ProcessRefund: {ex.Message}"); // Replace with ILogger

                return StatusCode(500, new
                {
                    ResponseCode = 0,
                    ResponseCodeDescription = "An internal server error occurred: " + ex.Message,
                    Status = "Failure"
                });
            }
        }
    }
}


