using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using LIB.API.Application.DTOs;

namespace LIB.API.Application.Contracts.Persistence
{
    public interface IAirlinesOrderService
    {
        Task<OrderResponseDto?> FetchOrderAsync(OrderRequestDto request);
        Task<bool> IsReferenceNoUniqueAsync(string referenceNo);
    }

}

