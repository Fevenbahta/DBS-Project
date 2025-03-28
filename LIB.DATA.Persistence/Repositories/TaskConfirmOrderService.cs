using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LIB.API.Application.DTOs;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace LIB.API.Persistence.Repositories
{
    public class TaskConfirmOrderService
    {
        private readonly LIBAPIDbSQLContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TaskConfirmOrderService(LIBAPIDbSQLContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }
        string shortcode = "526341";
        public async Task<bool> ProcessConfirmOrdersAsync()
        {
            var ordersToConfirm = await _context.confirmorders
                                                 .Where(o => o.StatusCodeResponse == 0) // assuming 0 means unconfirmed
                                                 .ToListAsync();

            if (ordersToConfirm == null || !ordersToConfirm.Any())
            {
                // No orders to process
                return false;
            }

            foreach (var order in ordersToConfirm)
            {
                var confirmOrderRequest = new ConfirmOrders
                {
                    OrderId = order.OrderId,
                    Amount = order.Amount,
                    Currency = "ETB",
                    Status = "1", // assuming '1' means confirmed
                    Remark = "Transfer Successful",
                    TraceNumber = order.TraceNumber,
                    ReferenceNumber = order.ReferenceNumber,
                    PaidAccountNumber = order.PaidAccountNumber,
                    PayerCustomerName = order.PayerCustomerName,
                    ShortCode = shortcode,
                    RequestDate = DateTime.UtcNow
                };

                bool confirmationResult = await ConfirmOrderAsync(confirmOrderRequest);

                if (confirmationResult)
                {
                    // Update the order status to 'Confirmed' (assuming '1' is confirmed)
                    order.Status = "1"; // confirmed status
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        private async Task<bool> ConfirmOrderAsync(ConfirmOrders confirmOrder)
        {
            string apiUrl = "https://ethiopiangatewaytest.azurewebsites.net/Lion/api/V1.0/Lion/ConfirmOrder";

            string username = "lionbanktest@ethiopianairlines.com";
            string password = "LI*&%@54778Ba";
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            var jsonContent = JsonSerializer.Serialize(confirmOrder);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log error if API call fails
                await LogErrorToAirlinesErrorAsync(
                    "API Error",
                    confirmOrder.OrderId.ToString(),
                    "Failed",
                    jsonResponse,
                    "ConfirmOrder",
                    confirmOrder.OrderId.ToString()
                );

                return false;
            }

            var confirmOrderResponse = JsonSerializer.Deserialize<ConfirmOrderResponseDto>(jsonResponse);

            if (confirmOrderResponse == null)
            {
                // Log error if the response deserialization fails
                await LogErrorToAirlinesErrorAsync(
                    "Deserialization Error",
                    confirmOrder.OrderId.ToString(),
                    "Failed",
                    "Invalid JSON response",
                    "ConfirmOrder",
                    confirmOrder.OrderId.ToString()
                );

                return false;
            }

            // Update order with API response
            confirmOrder.ExpireDate = confirmOrderResponse?.ExpireDate;
            confirmOrder.StatusCodeResponse = confirmOrderResponse?.StatusCodeResponse ?? 0;
            confirmOrder.StatusCodeResponseDescription = confirmOrderResponse?.StatusCodeResponseDescription ?? "Empty response";
            confirmOrder.CustomerName = confirmOrderResponse?.CustomerName ?? "Empty response";
            confirmOrder.MerchantId = confirmOrderResponse?.MerchantId ?? 0;
            confirmOrder.MerchantCode = confirmOrderResponse?.MerchantCode ?? "Empty response";
            confirmOrder.MerchantName = confirmOrderResponse?.MerchantName ?? "Empty response";
            confirmOrder.Message = confirmOrderResponse?.Message ?? "Empty response";
            confirmOrder.ResponseDate = DateTime.UtcNow;

            // Save updates to the database
            _context.Update(confirmOrder);
            await _context.SaveChangesAsync();

            return true;
        }

        // Error logging method
        private async Task LogErrorToAirlinesErrorAsync(string errorType, string orderId, string result, string errorMessage, string function, string refundReferenceCode)
        {
            // Implement logging logic here (e.g., write to a log file, database, or external service)
            await Task.CompletedTask;
        }
    }

}
