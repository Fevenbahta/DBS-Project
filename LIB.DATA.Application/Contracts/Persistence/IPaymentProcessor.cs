using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Domain;

namespace LIB.API.Application.Contracts.Persistence
{
    public interface IPaymentProcessor
    {
        Task<Response> ProcessPaymentAsync(TransferRequest request, bool simulationIndicator);
        Task<Response> ProcessPaymentAsyncRtgs(TransferRequest request, bool simulationIndicator,string account,string name);


    }


}
