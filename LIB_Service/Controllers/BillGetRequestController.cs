using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LIB.API.Persistence.Repositories;
using LIB.API.Domain;
using LIB.API.Application.Contracts.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace LIB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BillGetRequestController : ControllerBase
    {
        private readonly IBillGetRequestRepository _billGetRequestRepository;

        public BillGetRequestController(IBillGetRequestRepository billGetRequestRepository)
        {
            _billGetRequestRepository = billGetRequestRepository;
        }

        // POST api/billgetrequest
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ProcessTransaction([FromBody] BillGetRequestDto billGetRequestDto)
        {
            // Step 1: Validate that the request is not null
            if (billGetRequestDto == null)
            {
                return BadRequest("Error: Invalid request data: No data provided.");
            }

       
       

            // Step 3: Conditional validation for PhoneNumber or (ProviderId and UniqueCode)
            if (string.IsNullOrEmpty(billGetRequestDto.PhoneNumber) &&
                (string.IsNullOrEmpty(billGetRequestDto.ProviderId)|| string.IsNullOrEmpty(billGetRequestDto.UniqueCode)))
            {
                return BadRequest("Error: Either PhoneNumber or both ProviderId and UniqueCode are required.");
            }

            if (string.IsNullOrEmpty(billGetRequestDto.ReferenceNo))
            {
                return BadRequest("Error: ReferenceNo is required.");
            }

            if (billGetRequestDto.ReferenceNo.Length > 50)
            {
                return BadRequest("Error: ReferenceNo cannot exceed 50 characters.");
            }

            if (billGetRequestDto.TransactionDate == default(DateTime))
            {
                return BadRequest("Error: TransactionDate is required and must be a valid date.");
            }

            if (string.IsNullOrEmpty(billGetRequestDto.AccountNo))
            {
                return BadRequest("Error: AccountNo is required.");
            }

            bool isReferenceNoUnique = await _billGetRequestRepository.IsReferenceNoUniqueAsync(billGetRequestDto.ReferenceNo);
            if (!isReferenceNoUnique)
            {
                return BadRequest("Error: ReferenceNo must be unique.");
            }

            try
            {
                // Call the repository method to process the transaction
                var response = await _billGetRequestRepository.ProcessTransactionAsync(billGetRequestDto);

                // Return the processed response data
                return Ok(response); // ✅ Return processed data
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                return StatusCode(500, $"Error: Internal server error: {ex.Message}");
            }
        }


    }
}
