using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    using System.Threading.Tasks;
    using global::LIB.API.Domain;

namespace LIB.API.Application.Contracts.Persistence
{
   

        public interface IECPaymentRepository
        {
        // Define the methods your repository will expose
        Task<(string Status, object Response)> CreateAndSendSoapRequestAsync(ECPaymentRequestDTO request);
        Task<bool> IsReferenceNoUniqueAsync(string referenceNo);
    }
    }


