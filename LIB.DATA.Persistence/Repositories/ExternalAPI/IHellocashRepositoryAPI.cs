using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO;
using LIB.API.Domain;

namespace LIB.API.Persistence.Repositories.ExternalAPI
{
    public interface IHellocashRepositoryAPI
    {
        Task<FinInsInsResponseDTO> CreateHellocashsTransfer(HellocashTransactionRequest request);

       // Task<FinInsInsResponseDTO> SubmitTransactionAsync(decimal amount, string phoneNo);

    }

}
