using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Application.DTOs;

namespace LIB.API.Application.Contracts.Persistence
{

    public interface IAirlinesOrderRepository
    {
        Task<OrderResponseDto?> GetOrderAsync(OrderRequestDto request);
    }

}
