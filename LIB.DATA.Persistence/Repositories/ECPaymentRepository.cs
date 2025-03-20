using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.Extensions.Http;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Azure;

namespace LIB.API.Persistence.Repositories
{
    public class ECPaymentRepository : IECPaymentRepository
    {
        private readonly LIBAPIDbSQLContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public ECPaymentRepository(LIBAPIDbSQLContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // Method to create and send the SOAP request
        public async Task<(string Status, string Response)> CreateAndSendSoapRequestAsync(ECPaymentRequestDTO request)
        {
            // Build the SOAP request body
            string soapRequest = BuildSoapRequest(request);

            // Send SOAP request to the external service
            string responseXml = await CallSoapApiAsync(soapRequest);

            // Parse the response and save it to the database
            var (status, responseMessage) = await SaveRequestResponseAsync(request, responseXml);

            // Return the status and response message
            return (status, responseMessage);
        }


        // Save the request and response to the database
        private async Task<(string Status, string Response)> SaveRequestResponseAsync(ECPaymentRequestDTO request, string responseXml)
        {
            // Parse the SOAP response
            var responseObj = XElement.Parse(responseXml);

            var statusCodeNode = responseObj.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusCode");
            string statusCode = statusCodeNode?.Value ?? "Unknown";

            // Initialize default values
            string paymentId = "N/A";
            string status = "Error";
            string responseError = "No error";

            // Check if statusCode is -1 (indicating an error)
            if (statusCode == "-1")
            {
                var errorMessageNode = responseObj.Descendants().FirstOrDefault(e => e.Name.LocalName == "line");
                responseError = errorMessageNode?.Value ?? "Unknown error occurred";
            }
            else
            {
                // Extract paymentId and paymentStatus only if no error
                var paymentIdNode = responseObj.Descendants().FirstOrDefault(e => e.Name.LocalName == "paymentId");
                paymentId = paymentIdNode?.Value ?? "Unknown";

                var statusNode = responseObj.Descendants().FirstOrDefault(e => e.Name.LocalName == "paymentStatus");
                status = statusNode?.Value ?? "Error";

                responseError = "No error";
            }

            var paymentRecord = new ECPaymentRecords
            {
                InvoiceId = request.InvoiceId,
                ReferenceNo = request.ReferenceNo,
                CustomerCode = request.CustomerCode,
                Reason = request.Reason,
                PaymentAmount = request.PaymentAmount,
                PaymentDate = request.PaymentDate,
                Branch = request.Branch,
                Currency = request.Currency,
                AccountNo = request.AccountNo,
                ProviderId = request.ProviderId,
                Status = status,  // Store the extracted payment status
                ResponseId = paymentId,  // Store the extracted payment ID,
                ResponseError= responseError,
                Response = responseObj.ToString(),
            };

            _context.ECPaymentRecords.Add(paymentRecord);
            await _context.SaveChangesAsync();

            return (status, paymentId); // Return extracted values
        }

        // Build the SOAP request body
        private string BuildSoapRequest(ECPaymentRequestDTO request)
        {
            // Construct SOAP envelope
            var soapEnvelope = $@"
            <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:amp='http://soprabanking.com/amplitude'>
                <soapenv:Header/>
                <soapenv:Body>
                    <amp:createECPaymentV2RequestFlow>
                        <amp:requestHeader>
                            <amp:requestId>req12</amp:requestId>
                            <amp:serviceName>createECPaymentV2</amp:serviceName>
                            <amp:timestamp>2025-03-20T13:52:01</amp:timestamp>
                            <amp:originalName>TELEBIRR</amp:originalName>
                            <amp:languageCode>002</amp:languageCode>
                            <amp:userCode>TELEBIRR</amp:userCode>
                        </amp:requestHeader>
                        <amp:createECPaymentV2Request>
                            <amp:providerId>{request.ProviderId}</amp:providerId>
                            <amp:invoiceId>{request.InvoiceId}</amp:invoiceId>
                            <amp:customerCode>{request.CustomerCode}</amp:customerCode>
                            <amp:debitedAccount>
                                <amp:branch>{request.Branch}</amp:branch>
                                <amp:currency>{request.Currency}</amp:currency>
                                <amp:account>{request.AccountNo}</amp:account>
                            </amp:debitedAccount>
                            <amp:reason>{request.Reason}</amp:reason>
                            <amp:paymentAmount>{request.PaymentAmount}</amp:paymentAmount>
                            <amp:paymentDate>{request.PaymentDate:yyyy-MM-dd}</amp:paymentDate>
                            <amp:inputBranchCode>{request.Branch}</amp:inputBranchCode>
                            <amp:paymentChannelIdentification>
                                <amp:paymentUseChannel>5</amp:paymentUseChannel>
                            </amp:paymentChannelIdentification>
                            <amp:deferredPayment>0</amp:deferredPayment>
                            <amp:pendingPayment>0</amp:pendingPayment>
                        </amp:createECPaymentV2Request>
                    </amp:createECPaymentV2RequestFlow>
                </soapenv:Body>
            </soapenv:Envelope>";

            return soapEnvelope;
        }

        // Call SOAP API asynchronously and return the response
        private async Task<string> CallSoapApiAsync(string soapRequest)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            using var httpClient = new HttpClient(handler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://10.1.7.85:8095/createECPaymentV2")
            {
                Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
            };

            // ✅ Add SOAPAction header
            requestMessage.Headers.Add("SOAPAction", "\"createECPaymentV2\"");

            // ✅ Ensure the Content-Type header is correct
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

            // ✅ Add Accept header
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

            // ✅ Send the request
            var response = await httpClient.SendAsync(requestMessage);

            // ✅ Read and return the response
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> IsReferenceNoUniqueAsync(string referenceNo)
        {
            // Check if the ReferenceNo already exists in the database
            var existingRequest = await _context.ECPaymentRecords
                .FirstOrDefaultAsync(b => b.ReferenceNo == referenceNo);

            return existingRequest == null; // Return true if not found, false otherwise
        }

    }
}
