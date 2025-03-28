using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using System.Text.Json;
    using LIB.API.Domain;


namespace LIB.API.Persistence.Repositories
{


    public class TaskRefundService
    {
        private readonly LIBAPIDbSQLContext _context;
        private const string MerchantCode = "562341";

        public TaskRefundService(LIBAPIDbSQLContext context)
        {
            _context = context;
        }

        // Method to process refunds
        public async Task<bool> ProcessRefundsAsync()
        {
            // Fetch refunds with status 0 from the database
            var refundsToProcess = await _context.confirmRefunds
                                                 .Where(r => r.ResponseStatus == "0")
                                                 .ToListAsync();

            if (refundsToProcess == null || !refundsToProcess.Any())
            {
                // No refunds to process
                return false;
            }

            foreach (var refund in refundsToProcess)
            {
                // Create the RefundConfirmationRequest from the database record
                var refundRequest = new RefundConfirmationRequest
                {
                    Shortcode = refund.ShortCode,
                    Amount = refund.Amount,
                    Currency = refund.Currency,
                    OrderId = refund.OrderId,
                    RefundReferenceCode = refund.RefundReferenceCode,
                    RefundAccountNumber = refund.RefundAccountNumber,
                    RefundDate = DateTime.Parse(refund.RefundDate),
                    BankRefundReference = refund.BankRefundReference,
                    RefundFOP = refund.RefundFOP,
                    Status = refund.Status,  // Ensure status is valid for API call
                    Remark = refund.Remark,
                    AccountHolderName = refund.AccountHolderName
                };

                // Call the ConfirmRefundAsync method
                var refundSuccess = await ConfirmRefundAsync(refundRequest);

                if (refundSuccess)
                {
                    // Optionally update the database record to indicate success
                    refund.Status = "1";  // Assuming 1 indicates success
                    _context.Update(refund);
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        // Method to call the ConfirmRefund API
        private async Task<bool> ConfirmRefundAsync(RefundConfirmationRequest refundRequest)
        {
            string apiUrl = "https://ethiopiangatewaytest.azurewebsites.net/Lion/api/V1.0/Lion/ConfirmRefund";

            // Encode username and password for Basic Authentication
            string username = "lionbanktest@ethiopianairlines.com";
            string password = "LI*&%@54778Ba";
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

            var confirmationPayload = new
            {
                shortcode = MerchantCode,
                Amount = refundRequest.Amount,
                currency = refundRequest.Currency,
                OrderId = refundRequest.OrderId,
                RefundReferenceCode = refundRequest.RefundReferenceCode,
                RefundAccountNumber = refundRequest.RefundAccountNumber,
                refundDate = refundRequest.RefundDate.ToString("yyyy-MM-dd"),
                bankRefundReference = refundRequest.BankRefundReference,
                refundFOP = refundRequest.RefundFOP,
                Status = refundRequest.Status,  // 1 for Success, 0 for Failure
                Remark = refundRequest.Remark,
                AccountHolderName = refundRequest.AccountHolderName,
            };

            string jsonPayload = JsonSerializer.Serialize(confirmationPayload);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Add Basic Auth header
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        // Log the error internally (but don't display it)
                        await LogErrorToAirlinesErrorAsync(
                            "API Error",
                            refundRequest.RefundAccountNumber,
                            "Failed",
                            responseString,
                            "ConfirmRefund",
                            refundRequest.RefundReferenceCode
                        );

                        return false; // If API call fails, return false
                    }

                    var jsonResponse = JsonSerializer.Deserialize<RefundConfirmationResponse>(responseString);
                    if (jsonResponse == null)
                    {
                        // Log the error internally (but don't display it)
                        await LogErrorToAirlinesErrorAsync(
                            "Deserialization Error",
                            refundRequest.RefundAccountNumber,
                            "Failed",
                            "Invalid JSON response",
                            "ConfirmRefund",
                            refundRequest.RefundReferenceCode
                        );

                        return false; // If JSON response deserialization fails, return false
                    }

                    var confirmRefund = new ConfirmRefund
                    {
                        ShortCode = refundRequest.Shortcode ?? "Unknown",
                        Amount = refundRequest.Amount,
                        Currency = refundRequest.Currency ?? "N/A",
                        OrderId = refundRequest.OrderId ?? "N/A",
                        RefundReferenceCode = refundRequest.RefundReferenceCode ?? "N/A",
                        RefundAccountNumber = refundRequest.RefundAccountNumber ?? "N/A",
                        RefundDate = refundRequest.RefundDate.ToString("yyyy-MM-dd"),
                        BankRefundReference = refundRequest.BankRefundReference ?? "N/A",
                        RefundFOP = refundRequest.RefundFOP ?? "N/A",
                        Status = jsonResponse?.Status == 1 ? "1" : "0",
                        Remark = refundRequest.Remark ?? "N/A",
                        AccountHolderName = refundRequest.AccountHolderName ?? "N/A",
                        ResponseRefundReferenceCode = jsonResponse?.refundReferenceCode ?? "N/A",
                        ResponseBankRefundReference = jsonResponse?.bankRefundReference ?? "N/A",
                        ResponseAmount = jsonResponse?.Amount?.ToString() ?? "0",
                        ResponseStatus = jsonResponse?.Status.ToString() ?? "N/A",
                        ResponseMessage = jsonResponse?.message ?? "N/A",
                        CreatedDate = DateTime.UtcNow,
                    };

                    _context.confirmRefunds.Add(confirmRefund);
                    await _context.SaveChangesAsync();

                    return jsonResponse.Status == 1; // Return true if the status is "Success"
                }
            }
            catch (Exception ex)
            {
                // Log the error internally (but don't display it)
                await LogErrorToAirlinesErrorAsync(
                    "Exception Occurred",
                    refundRequest.RefundAccountNumber,
                    "Failed",
                    ex.Message,
                    "ConfirmRefund",
                    refundRequest.RefundReferenceCode
                );

                return false; // Return false if an exception occurs
            }
        }

        // Method to log errors internally
        private async Task LogErrorToAirlinesErrorAsync(string errorType, string refundAccountNumber, string result, string errorMessage, string function, string refundReferenceCode)
        {
            // Implement logging logic here (e.g., write to a log file, database, or external service)
            // This is just a placeholder for your logging mechanism
            await Task.CompletedTask;
        }
    }
}
