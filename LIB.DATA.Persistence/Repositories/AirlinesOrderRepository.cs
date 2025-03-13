using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using LIB.API.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace LIB.API.Persistence.Repositories
{
    public class AirlinesOrderRepository : IAirlinesOrderRepository
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly LIBAPIDbSQLContext _dbContext;

        public AirlinesOrderRepository(HttpClient httpClient, IConfiguration configuration, LIBAPIDbSQLContext dbContext)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<OrderResponseDto?> GetOrderAsync(string orderId, string shortCode,string refrence)
        {
            try
            {
                // Save request data to the database first
                var airlinesOrderRequest = new AirlinesOrder
                {
                    OrderId = orderId,
                    ShortCode = shortCode,
                    RequestDate = DateTime.UtcNow
                };

                _dbContext.airlinesorder.Add(airlinesOrderRequest);
                await _dbContext.SaveChangesAsync();

                // Use the provided public URL for the API
                string baseUrl = "https://ethiopiangatewaytest.azurewebsites.net";
                string url = $"{baseUrl}/Lion/api/V1.0/Lion/GetOrder?orderId={orderId}&shortCode={shortCode}";

                // Create and configure the request
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                //request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                //    Encoding.ASCII.GetBytes(":")) // Empty username and password
                //);



                // Send the request
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response data
                    var orderResponse = JsonConvert.DeserializeObject<OrderResponseDto>(responseBody);

                    // Update the order request with the response data
                    airlinesOrderRequest.Amount = orderResponse.Amount;
                    airlinesOrderRequest.TraceNumber = orderResponse.TraceNumber;
                    airlinesOrderRequest.StatusCodeResponse = orderResponse.StatusCodeResponse;
                    airlinesOrderRequest.StatusCodeResponseDescription = orderResponse.StatusCodeResponseDescription;
                    airlinesOrderRequest.ExpireDate = orderResponse.ExpireDate;
                    airlinesOrderRequest.CustomerName = orderResponse.CustomerName;
                    airlinesOrderRequest.MerchantId = orderResponse.MerchantId;
                    airlinesOrderRequest.MerchantCode = orderResponse.MerchantCode;
                    airlinesOrderRequest.MerchantName = orderResponse.MerchantName;
                    airlinesOrderRequest.Message = orderResponse.Message;

                    // Save the updated order data to the database
                    _dbContext.airlinesorder.Update(airlinesOrderRequest);
                    await _dbContext.SaveChangesAsync();

                    return orderResponse;
                }

                return null; // If the API call was not successful
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the process
                // Log the error to the AirlinesErrors table
                await LogErrorToAirlinesErrorAsync("GetOrderAsync", orderId, shortCode, ex.Message, "GetOrder", refrence);

                // Rethrow the exception to allow it to be handled elsewhere
                throw new Exception("Error occurred while getting the order.", ex);
            }
        }


        private async Task LogErrorToAirlinesErrorAsync(string methodName, string orderId, string shortCode, string errorMessage, string errorType,string refrence)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = methodName,  // Log the method name where the error occurred
                Origin = "AirlinesOrderRepository",  // This is where the error happened
                SpanId = orderId,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in {methodName} for OrderId: {orderId}, ShortCode: {shortCode}" }
        }
            };

            // Serialize the feedback object to JSON or a custom format
            string feedbackJson = JsonConvert.SerializeObject(feedback); 
            var errorRecord = new AirlinesError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = refrence,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _dbContext.airlineserror.Add(errorRecord);
            await _dbContext.SaveChangesAsync();
        }
    }
}

