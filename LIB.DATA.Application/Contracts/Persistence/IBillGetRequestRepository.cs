using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Domain;

namespace LIB.API.Application.Contracts.Persistence
{
    public interface IBillGetRequestRepository
    {
         Task<BillGetResponseDto> ProcessTransactionAsync(BillGetRequestDto billGetRequestDto);
        Task<bool> IsReferenceNoUniqueAsync(string referenceNo);
    }

}
