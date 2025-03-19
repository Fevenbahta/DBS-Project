using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Domain;
using Microsoft.Extensions.Http;
using System.Xml.Linq;

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
        public async Task<string> CreateAndSendSoapRequestAsync(ECPaymentRequestDTO request)
        {
            // Build the SOAP request body
            string soapRequest = BuildSoapRequest(request);

            // Send SOAP request to the external service
            string responseXml = await CallSoapApiAsync(soapRequest);

            // Parse the response and save it to the database
            await SaveRequestResponseAsync(request, responseXml);

            return responseXml;  // You can return the response if needed
        }

        // Save the request and response to the database
        private async Task SaveRequestResponseAsync(ECPaymentRequestDTO request, string responseXml)
        {
            // Parse the SOAP response (you may need to customize this based on actual response format)
            var responseObj = XElement.Parse(responseXml);
            var status = responseObj.Descendants("status").FirstOrDefault()?.Value ?? "Unknown";

            var paymentRecord = new ECPaymentRecords
            {
                InvoiceId = request.InvoiceId,
                ReferenceNo = request.ReferenceNo,
                UniqueCode = request.UniqueCode,
                Reason = request.Reason,
                PaymentAmount = request.PaymentAmount,
                PaymentDate = request.PaymentDate,
                Branch = request.Branch,
                Currency = request.Currency,
                Account = request.Account,
                BillerId = request.BillerId,
                Status = status,  // Set status from response
                Response = responseXml  // Store the full response for debugging or logging purposes
            };

            _context.ECPaymentRecords.Add(paymentRecord);
            await _context.SaveChangesAsync();
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
                            <amp:timestamp>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}</amp:timestamp>
                            <amp:originalName>TELEBIRR</amp:originalName>
                            <amp:languageCode>002</amp:languageCode>
                            <amp:userCode>TELEBIRR</amp:userCode>
                        </amp:requestHeader>
                        <amp:createECPaymentV2Request>
                            <amp:providerId>6</amp:providerId>
                            <amp:invoiceId>{request.InvoiceId}</amp:invoiceId>
                            <amp:customerCode>{request.UniqueCode}</amp:customerCode>
                            <amp:debitedAccount>
                                <amp:branch>{request.Branch}</amp:branch>
                                <amp:currency>{request.Currency}</amp:currency>
                                <amp:account>{request.Account}</amp:account>
                            </amp:debitedAccount>
                            <amp:reason>{request.Reason}</amp:reason>
                            <amp:paymentAmount>{request.PaymentAmount}</amp:paymentAmount>
                            <amp:paymentDate>{request.PaymentDate:yyyy-MM-dd}</amp:paymentDate>
                            <amp:inputBranchCode>{request.Branch}</amp:inputBranchCode>
                            <amp:paymentChannelIdentification>
                                <amp:paymentUseChannel>{request.BillerId}</amp:paymentUseChannel>
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
            var client = _httpClientFactory.CreateClient();

            // Define the SOAP endpoint
            var url = "https://10.1.7.85:8095/createECPaymentV2"; // Replace with your actual SOAP service URL
            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            // Send POST request
            var response = await client.PostAsync(url, content);

            // Ensure the request is successful
            response.EnsureSuccessStatusCode();

            // Return the response body as string
            return await response.Content.ReadAsStringAsync();
        }
    }
}
