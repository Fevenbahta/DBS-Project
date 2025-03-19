using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.DTOs;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Mysqlx.Crud;


    namespace LIB.API.Persistence.Repositories
    {
        public class ConfirmOrderService : IConfirmOrderService
        {
            private readonly IConfirmOrderRepository _confirmOrderRepository;

            public ConfirmOrderService(IConfirmOrderRepository confirmOrderRepository)
            {
                _confirmOrderRepository = confirmOrderRepository;
            }

            public async Task<TransactionResponseDto> CreateTransferAsync(decimal Amount, string DAccountNo, string OrderId, string ReferenceNo, string traceNumber, string merchantCode)
        {
                try
                {
                // Call repository method to handle the full flow (saving request, calling API, saving response)
                    return await _confirmOrderRepository.CreateTransferAsync( Amount,  DAccountNo,  OrderId,  ReferenceNo,  traceNumber,  merchantCode);
            }
            catch (Exception ex)
            {
                // Handle errors or log them as needed
                    throw new Exception("Error in ConfirmOrderAsync: " + ex.Message);
                }
            }
        }
    }



