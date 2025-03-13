using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.DTOs;

namespace LIB.API.Persistence.Repositories
{

    public class AirlinesOrderService : IAirlinesOrderService
    {
        private readonly IAirlinesOrderRepository _orderRepository;

        public AirlinesOrderService(IAirlinesOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderResponseDto?> FetchOrderAsync(OrderRequestDto request)
        {
            return await _orderRepository.GetOrderAsync(request.OrderId, request.ShortCode,request.ReferenceId);
        }
    }
}
