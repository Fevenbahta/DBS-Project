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

        public async Task<OrderResponseDto?> GetOrderAsync(string orderId,string refrence)
        {
            try
            {
                string shortCode = "12345";

                // Save request data to the database first
                var airlinesOrderRequest = new AirlinesOrder
                {
                    OrderId = orderId,
                    ShortCode = shortCode,
                    ReferenceId= refrence,
                    RequestDate = DateTime.UtcNow
                };

                _dbContext.airlinesorder.Add(airlinesOrderRequest);
                await _dbContext.SaveChangesAsync();

                // Use the provided public URL for the API
                string baseUrl = "https://ethiopiangatewaytest.azurewebsites.net";
                string url = $"{baseUrl}/Lion/api/V1.0/Lion/GetOrder?orderId={orderId}&shortCode={shortCode}";

                // Encode username and password for Basic Authentication
                string username = "lionbanktest@ethiopianairlines.com";
                string password = "LI*&%@54778Ba";
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

                // Create and configure the request
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                // Send the request
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();


                // Deserialize the response data
                var orderResponse = JsonConvert.DeserializeObject<OrderResponseDto>(responseBody);
                var safeOrderResponse = new OrderResponseDto
                {
                    Amount = orderResponse.Amount ,  // Default to 0 if Amount is null
                    TraceNumber = orderResponse.TraceNumber ?? "",  // Default to empty string if TraceNumber is null
                    StatusCodeResponse = orderResponse.StatusCodeResponse,  // Default to 0 if StatusCodeResponse is null
                    StatusCodeResponseDescription = orderResponse.StatusCodeResponseDescription ?? "",  // Default to empty string if StatusCodeResponseDescription is null
                    ExpireDate = orderResponse.ExpireDate ?? DateTime.MinValue,  // Default to DateTime.MinValue if ExpireDate is null
                    CustomerName = orderResponse.CustomerName ?? "",  // Default to empty string if CustomerName is null
                    MerchantId = orderResponse.MerchantId,  // Default to 0 if MerchantId is null
                    MerchantCode = orderResponse.MerchantCode ?? "",  // Default to empty string if MerchantCode is null
                    MerchantName = orderResponse.MerchantName ?? "",  // Default to empty string if MerchantName is null
                    Message = orderResponse.Message ?? "",  // Default to empty string if Message is null
                    UtilityName = orderResponse.UtilityName ?? "",  // Default to empty string if UtilityName is null
                    LionTransactionNo = orderResponse.LionTransactionNo ?? "",  // Default to empty string if LionTransactionNo is null
                    BusinessErrorCode = orderResponse.BusinessErrorCode ?? "",  // Default to empty string if BusinessErrorCode is null
                    StatusCode = orderResponse.StatusCode ,  // Default to 0 if StatusCode is null
                    Status = orderResponse.Status ,  // Default to empty string if Status is null
                    MessageList = orderResponse.MessageList ?? "",  // Default to empty string if MessageList is null
                    Errors = orderResponse.Errors ?? ""  // Default to empty string if Errors is null
                };
                // Update the order request with the response data
                airlinesOrderRequest.Amount = orderResponse.Amount;  // If Amount is null, default to 0
                airlinesOrderRequest.TraceNumber = orderResponse.TraceNumber ?? "";  // If TraceNumber is null, default to empty string
                airlinesOrderRequest.StatusCodeResponse = orderResponse.StatusCodeResponse;  // If StatusCodeResponse is null, default to 0
                airlinesOrderRequest.StatusCodeResponseDescription = orderResponse.StatusCodeResponseDescription ?? "";  // If StatusCodeResponseDescription is null, default to empty string
                airlinesOrderRequest.ExpireDate = orderResponse.ExpireDate;  // If ExpireDate is null, default to DateTime.MinValue
                airlinesOrderRequest.CustomerName = orderResponse.CustomerName ?? "";  // If CustomerName is null, default to empty string
                airlinesOrderRequest.MerchantId = orderResponse.MerchantId ;  // If MerchantId is null, default to 0
                airlinesOrderRequest.MerchantCode = orderResponse.MerchantCode ?? "";  // If MerchantCode is null, default to empty string
                airlinesOrderRequest.MerchantName = orderResponse.MerchantName ?? "";  // If MerchantName is null, default to empty string
                airlinesOrderRequest.Message = orderResponse.Message ?? "";  // If Message is null, default to empty string
                airlinesOrderRequest.UtilityName = orderResponse.UtilityName ?? "";  // If UtilityName is null, default to empty string
                airlinesOrderRequest.LionTransactionNo = orderResponse.LionTransactionNo ?? "";  // If LionTransactionNo is null, default to empty string
                airlinesOrderRequest.BusinessErrorCode = orderResponse.BusinessErrorCode ?? "";  // If BusinessErrorCode is null, default to empty string
                airlinesOrderRequest.StatusCode = orderResponse.StatusCode ;  // If StatusCode is null, default to 0
                airlinesOrderRequest.Status = orderResponse.Status;  // If Status is null, default to empty string
                airlinesOrderRequest.MessageList = orderResponse.MessageList ?? "";  // If MessageList is null, default to empty string
                airlinesOrderRequest.Errors = orderResponse.Errors ?? "";  // If Errors is null, default to empty string




                // Save the updated order data to the database
                _dbContext.airlinesorder.Update(airlinesOrderRequest);
                    await _dbContext.SaveChangesAsync();

                    return safeOrderResponse;
                          }
            catch (Exception ex)
            {
                // Handle any errors that occur during the process
                // Log the error to the AirlinesErrors table
                await LogErrorToAirlinesErrorAsync("GetOrderAsync", orderId, ex.Message, orderId, refrence);

                // Rethrow the exception to allow it to be handled elsewhere
                throw new Exception("Error occurred while getting the order.", ex);
            }
        }


        private async Task LogErrorToAirlinesErrorAsync(string methodName, string orderId, string errorMessage, string errorType,string refrence)
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
            new { Code = "0", Value = $"Error in {methodName} for OrderId: {orderId}" }
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

