using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using global::LIB.API.Domain;
    using LIB.API.Domain;
    using System.Threading.Tasks;

namespace LIB.API.Application.Contracts.Persistence
{

    namespace LIB.API.Repositories
    {
        public interface IRefundRepository
        {
            Task<bool> ProcessRefundAsync(RefundRequest refundRequest);
            Task<bool> IsReferenceNoUniqueAsync(string referenceNo);
        }
    }

}
