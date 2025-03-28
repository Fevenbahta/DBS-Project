using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DTO;
using LIB.API.Domain;
using LIB.API.Persistence.Repositories.ExternalAPI;
using Newtonsoft.Json;

public class HellocashRepositoryAPI : IHellocashRepositoryAPI
{
    private readonly HttpClient _httpClient;

    public HellocashRepositoryAPI(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<string> GetTokenAsync()
    {
        var tokenUrl = "https://testfinpay.anbesabank.com/api/Login/GetToken";

        var requestPayload = new
        {
            userCode = "MBK",
            Password = "FPWsnPmOYC/9jHfMzsKfuw=="
        };

        var jsonRequest = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(tokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (response.IsSuccessStatusCode && tokenResponse.Status == "SUCCESS")
            {
                return tokenResponse.Message; // Token
            }

            return null; // Return null if token request fails
        }
        catch
        {
            return null; // In case of any exception, return null
        }
    }

    public async Task<FinInsInsResponseDTO> CreateHellocashsTransfer(HellocashTransactionRequest request)
    {
        var token = await GetTokenAsync(); // Retrieve token

        if (token == null)
        {
            return new FinInsInsResponseDTO
            {
                success = false,
                message = "Failed to retrieve token",
                FinInstransactionId = string.Empty,
                ConversationID = string.Empty
            };
        }

        var url = "https://testfinpay.anbesabank.com/api/Transactions/SubmitTxn";

        var requestPayload = new
        {
            appcode = "MBKs",
            apppassphrase = "FPWsnPmOYC/9jHfMzsKfuw==",
            amount = request.Amount,
            debitaccount = request.DebitAccount,
            accname = request.AccName,
            destaccount = request.DestAccount,
            destaccname = request.DestAccName,
            destbankswiftcode = request.DestBankSwiftCode,
            originatorref = request.OriginatorRef,
            remarks = request.Remarks
        };

        var jsonRequest = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token);

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (!response.IsSuccessStatusCode || responseObject.SubmitTxnResponse.Successful == false)
            {
                return new FinInsInsResponseDTO
                {
                    success = false,
                    message = "Transaction failed",
                    FinInstransactionId = string.Empty,
                    ConversationID = string.Empty
                };
            }

            string referenceNumber = responseObject.SubmitTxnResponse.UtilAPISubmitTxnResponse.reference_number;

            // Now check the transaction status
            var checkStatusResponse = await CheckTransactionStatus(referenceNumber, token);

            return new FinInsInsResponseDTO
            {
                success = checkStatusResponse.success,
                message = checkStatusResponse.message,
                FinInstransactionId = referenceNumber,
                ConversationID = string.Empty
            };
        }
        catch (Exception ex)
        {
            return new FinInsInsResponseDTO
            {
                success = false,
                message = $"Exception occurred: {ex.Message}",
                FinInstransactionId = string.Empty,
                ConversationID = string.Empty
            };
        }
    }

    private async Task<FinInsInsResponseDTO> CheckTransactionStatus(string referenceNumber, string token)
    {
        var url = "https://testfinpay.anbesabank.com/api/Transactions/CheckStatus";

        var requestPayload = new
        {
            appcode = "MBKs",
            apppassphrase = "FPWsnPmOYC/9jHfMzsKfuw==",
            refno = referenceNumber
        };

        var jsonRequest = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token);

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (response.IsSuccessStatusCode && responseObject.CheckStatusResponse.Successful == true)
            {
                return new FinInsInsResponseDTO
                {
                    success = true,
                    message = "HellocashS Transaction successful",
                    FinInstransactionId = referenceNumber,
                    ConversationID = string.Empty
                };
            }
            else
            {
                return new FinInsInsResponseDTO
                {
                    success = false,
                    message = "HellocashS Transaction failed",
                    FinInstransactionId = string.Empty,
                    ConversationID = string.Empty
                };
            }
        }
        catch (Exception ex)
        {
            return new FinInsInsResponseDTO
            {
                success = false,
                message = $"Transaction status check failed: {ex.Message}",
                FinInstransactionId = string.Empty,
                ConversationID = string.Empty
            };
        }
    }
}
