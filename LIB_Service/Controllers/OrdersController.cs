using Microsoft.AspNetCore.Mvc;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.DTOs;
using LIB.API.Domain;
using System;
using System.Threading.Tasks;
using LIB.API.Persistence;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Mysqlx.Crud;

namespace LIB.API.Controllers
{
    [ApiController]
    [Route("api/v3/")]
    public class OrdersController : ControllerBase
    {
        private readonly IAirlinesOrderService _orderService;
        private readonly IConfirmOrderService _confirmOrderService;
        private readonly LIBAPIDbSQLContext _dbContext;

        public OrdersController(IAirlinesOrderService orderService, IConfirmOrderService confirmOrderService, LIBAPIDbSQLContext dbContext)
        {
            _orderService = orderService;
            _confirmOrderService = confirmOrderService;
            _dbContext = dbContext;
        }

        // Get Order
        [HttpGet("get-order")]
        public async Task<IActionResult> GetOrder([FromQuery] OrderRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.ShortCode) || string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                // Log the error into the AirlinesError table and return the BadRequest response
                await SaveErrorToAirlinesErrorAsync(request.OrderId, request.ShortCode, "OrderId and ShortCode are required.", "GetOrder", request.ReferenceId);

                return BadRequest(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),  // You can generate a unique ticket ID for each error
                    traceId = HttpContext.TraceIdentifier,
                    feedbacks = new[]
                    {
                        new
                        {
                            code = "SB_DS_001",
                            label = "OrderId and ShortCode are required.",
                            severity = "ERROR",
                            type = "BUS",
                            source = "Request Validation",
                            origin = "OrdersController",
                            spanId = HttpContext.TraceIdentifier,
                            parameters = new[]
                            {
                                new { code = "0", value = "Invalid Input" }
                            }
                        }
                    }
                });
            }

            var order = await _orderService.FetchOrderAsync(request);
            if (order == null)
            {
                // Log the error into the AirlinesError table and return the NotFound response
                await SaveErrorToAirlinesErrorAsync(request.OrderId, request.ShortCode, "Order not found or failed to fetch.", "GetOrder", request.ReferenceId);

                return NotFound(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = HttpContext.TraceIdentifier,
                    feedbacks = new[]
                    {
                        new
                        {
                            code = "SB_DS_002",
                            label = "Order not found or failed to fetch.",
                            severity = "ERROR",
                            type = "BUS",
                            source = "Order Fetching",
                            origin = "OrdersController",
                            spanId = HttpContext.TraceIdentifier,
                            parameters = new[]
                            {
                                new { code = "0", value = "Order not found" }
                            }
                        }
                    }
                });
            }

            return Ok(order);
        }

        // Confirm Order
        [AllowAnonymous]
        [HttpPost("CreateTransfer")]
        public async Task<IActionResult> CreateTransfer([FromBody] CreateBody body)
        {
            ModelState.Clear();
            // Initialize a list to collect error messages
            List<string> errorMessages = new List<string>();

            // Manually validate each property of the CreateBody model
            if (body.Amount <= 0)
            {
                errorMessages.Add("Amount must be greater than zero.");
            }
       

            if (string.IsNullOrEmpty(body.DAccountNo))
            {
                errorMessages.Add("DAccountNo is required.");
            }

            if (string.IsNullOrEmpty(body.OrderId))
            {
                errorMessages.Add("OrderId is required.");
            }

            if (string.IsNullOrEmpty(body.ReferenceNo))
            {
                errorMessages.Add("ReferenceNo is required.");
            }

      

            if (string.IsNullOrEmpty(body.TraceNumber))
            {
                errorMessages.Add("TraceNumber is required.");
            }

            if (string.IsNullOrEmpty(body.MerchantCode))
            {
                errorMessages.Add("MerchantCode is required.");
            }

            // If there are any validation errors, return a bad request response with the errors
            if (errorMessages.Any())
            {
                return BadRequest(new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = body.ReferenceNo,
                    feedbacks = new[]
                    {
                new
                {
                    code = "SB_DS_001", // Custom error code for validation error
                    label = "Validation failed",
                    severity = "ERROR",
                    type = "SYS",
                    source = "Order Create Transfer",
                    origin = "OrdersController",
                    spanId = HttpContext.TraceIdentifier,
                    parameters = errorMessages.Select(msg => new { code = "0", value = msg }).ToArray()
                }
            }
                });
            }

            try
            {
                // Call the service to handle transfer and order confirmation using the provided parameters
                var response = await _confirmOrderService.CreateTransferAsync(
                    body.Amount,
                    body.DAccountNo,
                    body.OrderId,
                    body.ReferenceNo,
                    body.TraceNumber,
                    body.MerchantCode
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the error into the AirlinesError table and return the logged error message.
                await SaveErrorToAirlinesErrorAsync(
                    body.OrderId,
                    body.MerchantCode,
                    ex.Message,
                    "CreateOrder",
                    body.ReferenceNo
                );

                return StatusCode(500, new
                {
                    returnCode = "ERROR",
                    ticketId = Guid.NewGuid().ToString(),
                    traceId = body.ReferenceNo,
                    feedbacks = new[]
                    {
                new
                {
                    code = "SB_DS_003",
                    label = ex.Message,
                    severity = "ERROR",
                    type = "SYS",
                    source = "Order CreateTransfer",
                    origin = "OrdersController",
                    spanId = HttpContext.TraceIdentifier,
                    parameters = new[] { new { code = "0", value = "Internal server error" } }
                }
            }
                });
            }
        }
        // Method to save error details into the AirlinesError table

        private async Task SaveErrorToAirlinesErrorAsync( string orderId, string shortCode, string errorMessage, string errorType,string refrence)
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
            new { Code = "0", Value = $"Error in controller for OrderId: {orderId}, ShortCode: {shortCode}" }
        }
            };

            // Serialize the feedback object to JSON or a custom format
            string feedbackJson = JsonConvert.SerializeObject(feedback);
            var errorRecord = new AirlinesError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = refrence,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _dbContext.airlineserror.Add(errorRecord);
            await _dbContext.SaveChangesAsync();
        }


    }
}
