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

            // Step 2: Manually validate the fields in BillGetRequestDto
            if (billGetRequestDto.ProviderId <= 0)
            {
                return BadRequest("Error: ProviderId must be a positive integer.");
            }

            if (string.IsNullOrEmpty(billGetRequestDto.UniqueCode))
            {
                return BadRequest("Error: UniqueCode is required.");
            }

            if (billGetRequestDto.UniqueCode.Length > 50)
            {
                return BadRequest("Error: UniqueCode cannot exceed 50 characters.");
            }

            if (string.IsNullOrEmpty(billGetRequestDto.PhoneNumber) )
            {
                return BadRequest("Error: PhoneNumber is required ");
            }

            if (string.IsNullOrEmpty(billGetRequestDto.ReferenceNo))
            {
                return BadRequest("Error:  ReferenceNo is required.");
            }

            if (billGetRequestDto.ReferenceNo.Length > 50)
            {
                return BadRequest("Error: ReferenceNo cannot exceed 50 characters.");
            }

            if (billGetRequestDto.TransactionDate == default(DateTime))
            {
                return BadRequest("Error: TransactionDate is required and must be a valid date.");
            }

            if (billGetRequestDto.CustomerId <= 0)
            {
                return BadRequest("Error: CustomerId must be a positive integer.");
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
                return StatusCode(500, $"Error:  Internal server error: {ex.Message}");
            }
        }


    }
}
