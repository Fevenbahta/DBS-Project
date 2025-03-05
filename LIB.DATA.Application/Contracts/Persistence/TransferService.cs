
using System.Threading.Tasks;
using LIB.API.Domain;
using Microsoft.AspNetCore.Mvc;

namespace LIB.API.Interfaces
{

    public interface ITransferService
    {

            Task<Response> CreateTransferAsync(TransferRequest request, bool simulationIndicator,string token);
            Task<TransferPostResponseBody> CancelTransferAsync(TransferCancellationRequest request);
        Task<List<TransferResponseBody>> GetTransferStatusAsync(TransferFilterParameters transferFilter);



    }

}
