using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LIB.API.Domain;
using LIB.API.Persistence.Repositories;
using LIB.API.Application.Contracts.Persistence;

namespace LIB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ECPaymentController : ControllerBase
    {
        private readonly IECPaymentRepository _ecPaymentRepository;

        // Inject the repository into the controller
        public ECPaymentController(IECPaymentRepository ecPaymentRepository)
        {
            _ecPaymentRepository = ecPaymentRepository;
        }

        // POST api/ecpayment
        [HttpPost]
        public async Task<IActionResult> CreatePaymentRequest([FromBody] ECPaymentRequestDTO request)
        {
            if (request == null)
            {
                return BadRequest("Payment request data is required.");
            }


            // Step 2: Manually validate the fields in request
            if (request.PaymentAmount <= 0)
            {
                return BadRequest("Error: PaymentAmount must be a positive integer.");
            }

            if (string.IsNullOrEmpty(request.InvoiceId))
            {
                return BadRequest("Error: InvoiceId is required.");
            }
          

            if (string.IsNullOrEmpty(request.CustomerCode))
            {
                return BadRequest("Error: UniqueCode is required ");
            }

            if (string.IsNullOrEmpty(request.ReferenceNo))
            {
                return BadRequest("Error:  ReferenceNo is required.");
            }
         if (string.IsNullOrEmpty(request.ProviderId))
               {
          return BadRequest("Error:  BillerId is required.");
                }
        if (string.IsNullOrEmpty(request.Currency))
        {
    return BadRequest("Error:  Currency is required.");
         }
          if (string.IsNullOrEmpty(request.Branch))
      {
    return BadRequest("Error:  Branch is required.");
          }

      if (request.PaymentDate == default(DateTime))
            {
                return BadRequest("Error: PaymentDate is required and must be a valid date.");
            }

                     if (string.IsNullOrEmpty(request.AccountNo))
            {
                return BadRequest("Error:  AccountNo is required.");
            }




            bool isReferenceNoUnique = await _ecPaymentRepository.IsReferenceNoUniqueAsync(request.ReferenceNo);
            if (!isReferenceNoUnique)
            {
                return BadRequest("Error: ReferenceNo must be unique.");
            }

            try
            {
                var (status, response) = await _ecPaymentRepository.CreateAndSendSoapRequestAsync(request);

                return Ok(new { Status = status, Id = response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    }
}
