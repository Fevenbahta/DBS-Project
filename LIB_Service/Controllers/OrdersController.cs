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
using LIB.API.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]  // Ensures the endpoint requires a valid token

        [HttpPost("get-order")]
        public async Task<IActionResult> GetOrder([FromBody] OrderRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OrderId) || string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                await SaveErrorToAirlinesErrorAsync(request?.OrderId, "OrderId is required.", "GetOrder", request?.ReferenceId);
                return BadRequest(GenerateErrorResponse("SB_DS_001", "OrderId is required.", "Request Validation", "Invalid Input"));
            }

            bool isReferenceNoUnique = await _orderService.IsReferenceNoUniqueAsync(request.ReferenceId);
            if (!isReferenceNoUnique)
            {
                await SaveErrorToAirlinesErrorAsync(request.OrderId, "Error: ReferenceNo must be unique.", "GetOrder", request.ReferenceId);
                return NotFound(GenerateErrorResponse("SB_DS_002", "Error: ReferenceNo must be unique.", "GetOrder", "ReferenceNo not unique"));
            }

            var order = await _orderService.FetchOrderAsync(request);
            if (order == null)
            {
                await SaveErrorToAirlinesErrorAsync(request.OrderId, "Order not found or failed to fetch.", "GetOrder", request.ReferenceId);
                return NotFound(GenerateErrorResponse("SB_DS_003", "Order not found or failed to fetch.", "Order Fetching", "Order not found"));
            }

            // If order is Expired (2), Already Paid (3), or Pending (1), return the error format
            if (order.StatusCodeResponse != 0)
            {
                return BadRequest(GenerateErrorResponse("SB_DS_004", $"Order is {order.StatusCodeResponseDescription}.", "Order Processing", order.OrderId));
            }

            return Ok(order);
        }

        private object GenerateErrorResponse(string errorCode, string message, string source, string parameterValue)
        {
            return new
            {
                returnCode = "ERROR",
                ticketId = Guid.NewGuid().ToString(),
                traceId = HttpContext.TraceIdentifier,
                feedbacks = new[]
                {
            new
            {
                code = errorCode,
                label = message,
                severity = "ERROR",
                type = "BUS",
                source = source,
                origin = "OrdersController",
                spanId = HttpContext.TraceIdentifier,
                parameters = new[]
                {
                    new { code = "0", value = parameterValue }
                }
            }
        }
            };
        }





        // Confirm Order

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]  // Ensures the endpoint requires a valid token

        [AllowAnonymous]
        [HttpPost("CreateTransfer")]

        public async Task<IActionResult> CreateTransfer([FromBody] CreateBody body)
        {
            ModelState.Clear();
            List<string> errorMessages = new List<string>();

            // Validate input fields
            if (body.Amount <= 0) errorMessages.Add("Amount must be greater than zero.");
            if (string.IsNullOrEmpty(body.DAccountNo)) errorMessages.Add("DAccountNo is required.");
            if (string.IsNullOrEmpty(body.OrderId)) errorMessages.Add("OrderId is required.");
            if (string.IsNullOrEmpty(body.ReferenceNo)) errorMessages.Add("ReferenceNo is required.");
            if (string.IsNullOrEmpty(body.TraceNumber)) errorMessages.Add("TraceNumber is required.");

            bool isReferenceNoUnique = await _confirmOrderService.IsReferenceNoUniqueAsync(body.ReferenceNo);
            if (!isReferenceNoUnique)
            {
                errorMessages.Add("Error: ReferenceNo must be unique.");
            }

            // If validation errors exist, return BadRequest
            if (errorMessages.Any())
            {
                return BadRequest(GenerateErrorResponse("SB_DS_001", "Validation failed", "Order Create Transfer", string.Join(", ", errorMessages)));
            }

            // Step 1: Fetch Order Status Before Processing Transfer
            var orderRequest = new OrderRequestDto
            {
                OrderId = body.OrderId,
                ReferenceId = body.ReferenceNo
            };

            var order = await _orderService.FetchOrderAsync(orderRequest);

            if (order == null)
            {
                return NotFound(GenerateErrorResponse("SB_DS_003", "Order not found or failed to fetch.", "CreateTransfer", "Order not found"));
            }

            // Step 2: Ensure Order Status Code is 0 Before Proceeding
            if (order.StatusCodeResponse != 0)
            {
                return BadRequest(GenerateErrorResponse("SB_DS_004", $"Order is {order.StatusCodeResponseDescription}. Cannot proceed with transfer.", "CreateTransfer", order.OrderId));
            }

            try
            {
                // Proceed with transfer
                var response = await _confirmOrderService.CreateTransferAsync(
                    body.Amount,
                    body.DAccountNo,
                    body.OrderId,
                    body.ReferenceNo,
                    body.TraceNumber
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                await SaveErrorToAirlinesErrorAsync(body.OrderId, ex.Message, "CreateTransfer", body.ReferenceNo);

                return StatusCode(500, GenerateErrorResponse("SB_DS_003", ex.Message, "Order CreateTransfer", "Internal server error"));
            }
        }

        private async Task SaveErrorToAirlinesErrorAsync( string orderId,string errorMessage, string errorType,string refrence)
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
