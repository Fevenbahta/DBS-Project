using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LIB.API.Domain;
using LIB.API.Persistence.Repositories;
using LIB.API.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using LIB.API.Persistence;

namespace LIB.API.Controllers
{
    [ApiController]

    [Route("api/v3/ECPayment")]

    public class ECPaymentController : ControllerBase
    {
        private readonly IECPaymentRepository _ecPaymentRepository;
        private readonly LIBAPIDbSQLContext _dbContext;

        // Inject the repository into the controller
        public ECPaymentController(IECPaymentRepository ecPaymentRepository, LIBAPIDbSQLContext dbContext)
        {
            _ecPaymentRepository = ecPaymentRepository;
          _dbContext = dbContext;
        }

        // POST api/ecpayment
        [HttpPost]
        public async Task<IActionResult> CreatePaymentRequest([FromBody] ECPaymentRequestDTO request)
        {
            if (request == null)
            {
                string errorMessage = "Payment request data is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request?.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
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

            if (string.IsNullOrEmpty(request.ProviderId))
            {
                string errorMessage = "Error: BillerId is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.Branch))
            {
                string errorMessage = "Error: Branch is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (request.PaymentDate == default(DateTime))
            {
                string errorMessage = "Error: PaymentDate is required and must be a valid date.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.AccountNo))
            {
                string errorMessage = "Error: AccountNo is required.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            bool isReferenceNoUnique = await _ecPaymentRepository.IsReferenceNoUniqueAsync(request.ReferenceNo);
            if (!isReferenceNoUnique)
            {
                string errorMessage = "Error: ReferenceNo must be unique.";
                var errorResponse = await SaveErrorToBillErrorAsync(request.InvoiceId, errorMessage, "Validation", "CreatePaymentRequest");
                return BadRequest(errorResponse);
            }

            try
            {
                var (status, response) = await _ecPaymentRepository.CreateAndSendSoapRequestAsync(request);
                if (status == "Error")
                {
                      return BadRequest( response);


                }
                return Ok( response );
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

    }
}
