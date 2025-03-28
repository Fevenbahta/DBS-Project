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
using Newtonsoft.Json;

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
        public async Task<(string Status, object Response)> CreateAndSendSoapRequestAsync(ECPaymentRequestDTO request)
        {
            try
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
            catch (Exception ex)
            {
                // Capture any error and log it through the centralized error method
                var errorResponse = await SaveErrorToBillErrorAsync(
                    request.InvoiceId,
                    ex.Message,
                    ex.GetType().Name,
                    request.ReferenceNo ?? "N/A"  // use a default if no reference available
                );

                // Return an error tuple with a JSON string of the error response
                return ("Error",errorResponse);
            }
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
            string status = "500";
            string responseError = "No error";

            // Check if statusCode is -1 (indicating an error)
            if (statusCode == "-1")
            {
                var errorMessageNode = responseObj.Descendants().FirstOrDefault(e => e.Name.LocalName == "line");
                responseError = errorMessageNode?.Value ?? "Unknown error occurred";
                await SaveErrorToBillErrorAsync(request?.ReferenceNo, responseError, "Exception", "EcpaymentrequestAsync");

                throw new Exception(responseError);

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
                CustomerId = request.CustomerId,
                Reason = request.Reason,
                PaymentAmount = request.PaymentAmount,
                PaymentDate = request.PaymentDate,
                Branch = request.Branch,
                Currency = "001",
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
                            <amp:customerCode>{request.CustomerId}</amp:customerCode>
                            <amp:debitedAccount>
                                <amp:branch>{request.Branch}</amp:branch>
                                <amp:currency>001</amp:currency>
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
        private async Task<object> SaveErrorToBillErrorAsync(string orderId, string errorMessage, string errorType, string reference)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = "Controller",  // Log the method name where the error occurred
                Origin = errorType,  // This is where the error happened
                SpanId = orderId,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in controller for OrderId: {orderId}" }
        }
            };

            // Serialize the feedback object to JSON
            string feedbackJson = JsonConvert.SerializeObject(feedback);
            var errorRecord = new BillError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = reference,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _context.billerror.Add(errorRecord);
            await _context.SaveChangesAsync();

            var response = new
    {
        returnCode = "ERROR",
        ticketId = errorRecord.TicketId,
        traceId = reference,
        feedbacks = new List<object> { feedback }
    };

    return ( response);  // Explicitly return HTTP 500

        }

        public async Task<bool> IsReferenceNoUniqueAsync(string referenceNo)
        {
            // Check if the ReferenceNo already exists in the database
            var existingRequest = await _context.ECPaymentRecords
                .FirstOrDefaultAsync(b => b.ReferenceNo == referenceNo);

            return existingRequest == null; // Return true if not found, false otherwise
        }


        private async Task LogErrorToAirlinesErrorAsync(string methodName, string orderId, string errorMessage, string errorType, string refrence)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = methodName,  // Log the method name where the error occurred
                Origin = "ECpaymentRepository",  // This is where the error happened
                SpanId = orderId,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in {methodName} for RefNo: {orderId}" }
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

            _context.airlineserror.Add(errorRecord);
            await _context.SaveChangesAsync();
        }


    }
}
