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

            try
            {
                // Call the repository to send the JSON request and get the response
                var response = await _ecPaymentRepository.CreateAndSendSoapRequestAsync(request);

                // Return the response from the external API or any relevant data
                return Ok(new { success = true, response });
            }
            catch (System.Exception ex)
            {
                // Handle errors (e.g., log the error, return an error message)
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
