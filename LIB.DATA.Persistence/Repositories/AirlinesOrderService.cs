using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LIB.API.Persistence.Repositories
{

    public class AirlinesOrderService : IAirlinesOrderService
    {
        private readonly IAirlinesOrderRepository _orderRepository;
        private readonly LIBAPIDbSQLContext _context;

        public AirlinesOrderService(IAirlinesOrderRepository orderRepository, LIBAPIDbSQLContext context)
        {
            _orderRepository = orderRepository;
            _context = context;
        }

        public async Task<OrderResponseDto?> FetchOrderAsync(OrderRequestDto request)
        {
            return await _orderRepository.GetOrderAsync(request.OrderId,request.ReferenceId);
        }
        public async Task<bool> IsReferenceNoUniqueAsync(string referenceNo)
        {
            // Check if the ReferenceNo already exists in the database
            var existingRequest = await _context.airlinesorder
                .FirstOrDefaultAsync(b => b.ReferenceId == referenceNo);

            return existingRequest == null; // Return true if not found, false otherwise
        }
    }
}
