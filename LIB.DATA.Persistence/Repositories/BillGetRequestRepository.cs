using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using global::LIB.API.Domain;
    using LIB.API.Domain;
using LIB.API.Application.Contracts.Persistence;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace LIB.API.Persistence.Repositories
{
    
  
        public class BillGetRequestRepository : IBillGetRequestRepository
        {
            private readonly HttpClient _httpClient;
            private readonly LIBAPIDbSQLContext _context;

            public BillGetRequestRepository(HttpClient httpClient, LIBAPIDbSQLContext context)
            {
                _httpClient = httpClient;
                _context = context;
            }
        public async Task<BillGetResponseDto> ProcessTransactionAsync(BillGetRequestDto billGetRequestDto)
        {
            var billGetRequest = new BillGetRequest
            {
                BillerType = billGetRequestDto.BillerType,
                ReqProviderId = billGetRequestDto.ProviderId.ToString(),
                UniqueCode = billGetRequestDto.UniqueCode,
                PhoneNumber = billGetRequestDto.PhoneNumber,
                ReferenceNo = billGetRequestDto.ReferenceNo,
                ReqTransactionDate = billGetRequestDto.TransactionDate,
                CustomerId = billGetRequestDto.CustomerId.ToString(),
                ResTransactionDate = DateTime.UtcNow
            };

            // Step 2: Call the SOAP API
            var soapResponse = await CallSoapApiAsync(billGetRequestDto);

            // Step 3: Process the SOAP response and map values to the entity
            var status = ParseSoapResponse(soapResponse, out var providerId, out var invoiceId,
                                            out var invoiceIdentificationValue, out var invoiceAmount,
                                            out var currencyAlphaCode, out var currencyDesignation);

            billGetRequest.Status = status;
            billGetRequest.ResponseError = soapResponse; // Store raw response for troubleshooting if necessary
            billGetRequest.ResProviderId = providerId;
            billGetRequest.InvoiceId = invoiceId;
            billGetRequest.InvoiceIdentificationValue = invoiceIdentificationValue;
            billGetRequest.InvoiceAmount = invoiceAmount;
            billGetRequest.CurrencyAlphaCode = currencyAlphaCode;
            billGetRequest.CurrencyDesignation = currencyDesignation;

            // Step 4: Save the data to the database
            await _context.BillGetRequests.AddAsync(billGetRequest);
            await _context.SaveChangesAsync();

            // Return the processed response data
            return new BillGetResponseDto
            {
                Status = status,
                ProviderId = providerId,
                InvoiceId = invoiceId,
                InvoiceIdentificationValue = invoiceIdentificationValue,
                InvoiceAmount = invoiceAmount,
                CurrencyAlphaCode = currencyAlphaCode,
                CurrencyDesignation = currencyDesignation
            };
        }
        // SOAP API call logic
        private async Task<string> CallSoapApiAsync(BillGetRequestDto requestDto)
            {
                // Prepare the SOAP request XML using the provided format
                string soapRequest = $@"
                <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:amp='http://soprabanking.com/amplitude'>
                    <soapenv:Header/>
                    <soapenv:Body>
                        <amp:getECInvoiceListRequestFlow>
                            <amp:requestHeader>
                                <amp:requestId>req1</amp:requestId>
                                <amp:serviceName>getECInvoiceList</amp:serviceName>
                                <amp:timestamp>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}</amp:timestamp>
                                <amp:originalName>TELEBIRR</amp:originalName>
                                <amp:languageCode>002</amp:languageCode>
                                <amp:userCode>TELEBIRR</amp:userCode>
                            </amp:requestHeader>
                            <amp:getECInvoiceListRequest>
                                <amp:invoiceIdentification>
                                    <amp:providerComponentIdentification>
                                        <amp:identifierNumber>1</amp:identifierNumber>
                                        <amp:identifierValue>{requestDto.ProviderId}</amp:identifierValue>
                                    </amp:providerComponentIdentification>
                                </amp:invoiceIdentification>
                            </amp:getECInvoiceListRequest>
                        </amp:getECInvoiceListRequestFlow>
                    </soapenv:Body>
                </soapenv:Envelope>";
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var httpClient = new HttpClient(handler); 

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://10.1.7.85:8095/getECInvoiceList")
                {
                    Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
                };

            // ✅ Add SOAPAction header if required
            requestMessage.Headers.Add("SOAPAction", "\"getECInvoiceList\"");

            // ✅ Ensure the Content-Type header is correct
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

            // ✅ Add Accept header
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));


            // Send the SOAP request
            var response = await httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }

        // Custom logic to parse the SOAP response (Example)
        // Custom logic to parse the SOAP response (Example)
        private string ParseSoapResponse(string soapResponse, out string providerId, out int invoiceId,
                                        out string invoiceIdentificationValue, out decimal invoiceAmount,
                                        out string currencyAlphaCode, out string currencyDesignation)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);

            // ✅ Register the namespace
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("fjs1", "http://soprabanking.com/amplitude");
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");

            // Initialize output variables
            providerId = string.Empty;
            invoiceId = 0;
            invoiceIdentificationValue = string.Empty;
            invoiceAmount = 0.00m;
            currencyAlphaCode = string.Empty;
            currencyDesignation = string.Empty;

            // ✅ Use namespace manager for selecting nodes
            var statusNode = xmlDoc.SelectSingleNode("//fjs1:responseStatus/fjs1:statusCode", nsManager);
            var status = statusNode?.InnerText ?? "Unknown";

            // Extract invoice details if the response status is OK (statusCode == 0)
            if (status == "0")
            {
                var invoiceNode = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceListResponse/fjs1:invoice", nsManager);

                if (invoiceNode != null)
                {
                    providerId = invoiceNode.SelectSingleNode("fjs1:providerId", nsManager)?.InnerText ?? string.Empty;
                    var invoiceIdNode = invoiceNode.SelectSingleNode("fjs1:invoiceId", nsManager);
                    var invoiceAmountNode = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:amount1", nsManager);
                    var currencyNode = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:currency/fjs1:currency", nsManager);

                    // ✅ Parsing invoice information
                    invoiceId = int.TryParse(invoiceIdNode?.InnerText, out var parsedInvoiceId) ? parsedInvoiceId : 0;
                    invoiceAmount = decimal.TryParse(invoiceAmountNode?.InnerText, out var parsedAmount) ? parsedAmount : 0.00m;
                    currencyAlphaCode = currencyNode?.SelectSingleNode("fjs1:alphaCode", nsManager)?.InnerText ?? string.Empty;
                    currencyDesignation = currencyNode?.SelectSingleNode("fjs1:designation", nsManager)?.InnerText ?? string.Empty;

                    // Debugging output
                    Console.WriteLine($"Invoice ID: {invoiceId}, Invoice Amount: {invoiceAmount}, Currency: {currencyAlphaCode} {currencyDesignation}");

                    return status;
                }
            }

            return "Error in response:invalid Provider code";
        }




        public async Task<bool> IsReferenceNoUniqueAsync(string referenceNo)
        {
            // Check if the ReferenceNo already exists in the database
            var existingRequest = await _context.BillGetRequests
                .FirstOrDefaultAsync(b => b.ReferenceNo == referenceNo);

            return existingRequest == null; // Return true if not found, false otherwise
        }

    }



}


