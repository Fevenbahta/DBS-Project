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
using Microsoft.IdentityModel.Tokens;

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
        public async Task<List<BillGetResponseDto>> ProcessTransactionAsync(BillGetRequestDto billGetRequestDto)
        {
            var billGetRequest = new BillGetRequest
            {
                BillerType = billGetRequestDto.BillerType,
                ReqProviderId = billGetRequestDto.ProviderId ?? "",  // If null, set to ""
                UniqueCode = billGetRequestDto.UniqueCode ?? "",
                PhoneNumber = billGetRequestDto.PhoneNumber ?? "",
                ReferenceNo = billGetRequestDto.ReferenceNo,
                ReqTransactionDate = billGetRequestDto.TransactionDate,
                AccountNo = billGetRequestDto.AccountNo ?? "",
                ResTransactionDate = DateTime.UtcNow
            };
            var soapResponse = "";
            List<BillGetResponseDto> responseList = new List<BillGetResponseDto>();
            var status = "";
            if (!string.IsNullOrEmpty(billGetRequestDto.ProviderId) && !string.IsNullOrEmpty(billGetRequestDto.UniqueCode))

            {
                // Call SOAP request for ProviderId and UniqueCode

                soapResponse = await CallSoapApiAsyncWithProviderIdAndUniqueCode(billGetRequestDto);
                status = ParseSoapResponseWithProviderIdAndUniqueCode(soapResponse, out var providerId, out var invoiceId,
                                        out var invoiceIdentificationValue, out var invoiceAmount,
                                        out var currencyAlphaCode, out var currencyDesignation, out var customerName, out var providerName, out string uniqueCode);

                billGetRequest.Status = status;
                billGetRequest.ResponseError = soapResponse; // Store raw response for troubleshooting if necessary
                billGetRequest.ResProviderId = new List<string> { providerId };
                billGetRequest.InvoiceId = new List<int> { invoiceId };
                billGetRequest.InvoiceIdentificationValue = new List<string> { invoiceIdentificationValue };
                billGetRequest.InvoiceAmount = new List<decimal> { invoiceAmount };
                billGetRequest.CurrencyAlphaCode = new List<string> { currencyAlphaCode };
                billGetRequest.CurrencyDesignation = new List<string> { currencyDesignation };
                billGetRequest.CustomerName = new List<string> { customerName };
                billGetRequest.ProviderName = new List<string> { providerName };


                // Step 4: Save the data to the database
                await _context.BillGetRequests.AddAsync(billGetRequest);
                await _context.SaveChangesAsync();

                // Add each parsed response to the response list
                responseList.Add(new BillGetResponseDto
                {
                    Status = status,
                    ProviderId = providerId,
                    InvoiceId = invoiceId,
                    InvoiceIdentificationValue = invoiceIdentificationValue,
                    InvoiceAmount = invoiceAmount,
                    CurrencyAlphaCode = currencyAlphaCode,
                    CurrencyDesignation = currencyDesignation,
                    CustomerName = customerName,
                    ProviderName = providerName,
                    CustomerCode= uniqueCode
                });
                return responseList; // or handle as needed

            }
            else
            {
                // Step 1: Create a list to store the response data
                List<BillGetResponseDto> responseLists = new List<BillGetResponseDto>();

                // Assuming `billGetRequestDto` is properly initialized with required data
                // Step 2: Call SOAP request for PhoneNumber
                soapResponse = await CallSoapApiAsyncWithPhoneNumber(billGetRequestDto);

                // Step 3: Process the SOAP response and parse values using ParseSoapResponseWithPhoneNo
                string statuss = ParseSoapResponseWithPhoneNo(soapResponse, out List<BillGetResponseDto> invoices);

                // Check if status is OK (0) and if invoices are found
                if (statuss == "0" && invoices != null && invoices.Count > 0)
                {
                    // Map the first invoice (or process all invoices if needed)
                    var invoice = invoices; // Modify as necessary to handle multiple invoices

                    // Store parsed data into the `billGetRequest` object
                    billGetRequest.Status = statuss;
                    billGetRequest.ResponseError = soapResponse; // Store raw response for troubleshooting if necessary
                    billGetRequest.ResProviderId = invoices.SelectMany(i => i.ProviderId.Select(p => p.ToString())).ToList();

                    // Assigning InvoiceId as a List<int>
                    // Assuming `invoices` is a List<BillGetResponseDto>

                    // Assigning InvoiceId as a List<int>
                    billGetRequest.InvoiceId = invoices.Select(i => i.InvoiceId).ToList();

                    // Assigning InvoiceIdentificationValue as a List<string>
                    billGetRequest.InvoiceIdentificationValue = invoices.Select(i => i.InvoiceIdentificationValue).ToList();

                    // Assigning InvoiceAmount as a List<decimal>
                    billGetRequest.InvoiceAmount = invoices.Select(i => i.InvoiceAmount).ToList();

                    // Assigning CurrencyAlphaCode as a List<string>
                    billGetRequest.CurrencyAlphaCode = invoices.Select(i => i.CurrencyAlphaCode).ToList();

                    // Assigning CurrencyDesignation as a List<string>
                    billGetRequest.CurrencyDesignation = invoices.Select(i => i.CurrencyDesignation).ToList();

                    // Assigning CustomerName as a List<string?>
                    billGetRequest.CustomerName = invoices.Select(i => i.CustomerName).ToList();

                    // Assigning ProviderName as a List<string?>
                    billGetRequest.ProviderName = invoices.Select(i => i.ProviderName).ToList();


                    // Step 4: Save the data to the database
                    await _context.BillGetRequests.AddAsync(billGetRequest);
                    await _context.SaveChangesAsync();

                    // Add the invoice details to the response list
                    responseLists.AddRange(invoices); // Add all invoices or select specific ones

                    // Return the list of processed response data
                    return responseLists;
                }
                else
                {
                    // Handle error scenario
                    Console.WriteLine($"Error processing SOAP response: {status}");
                    return responseLists; // or handle as needed
                }
            }
        }

        // Custom logic to parse the SOAP response (Example) for multiple responses
        private string ParseSoapResponse(string soapResponse, out string providerId, out int invoiceId,
                                            out string invoiceIdentificationValue, out decimal invoiceAmount,
                                            out string currencyAlphaCode, out string currencyDesignation)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);

            // ? Register the namespace
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

            // ? Use namespace manager for selecting nodes
            var statusNode = xmlDoc.SelectSingleNode("//fjs1:responseStatus/fjs1:statusCode", nsManager);
            var status = statusNode?.InnerText ?? "Unknown";

            // Extract multiple invoices if statusCode == 0 (successful response)
            if (status == "0")
            {
                var invoiceNodes = xmlDoc.SelectNodes("//fjs1:getECInvoiceListResponse/fjs1:invoice", nsManager);

                foreach (XmlNode invoiceNode in invoiceNodes)
                {
                    providerId = invoiceNode.SelectSingleNode("fjs1:providerId", nsManager)?.InnerText ?? string.Empty;
                    var invoiceIdNode = invoiceNode.SelectSingleNode("fjs1:invoiceId", nsManager);
                    var invoiceAmountNode = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:amount1", nsManager);
                    var currencyNode = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:currency/fjs1:currency", nsManager);

                    // Parsing invoice information
                    invoiceId = int.TryParse(invoiceIdNode?.InnerText, out var parsedInvoiceId) ? parsedInvoiceId : 0;
                    invoiceAmount = decimal.TryParse(invoiceAmountNode?.InnerText, out var parsedAmount) ? parsedAmount : 0.00m;
                    currencyAlphaCode = currencyNode?.SelectSingleNode("fjs1:alphaCode", nsManager)?.InnerText ?? string.Empty;
                    currencyDesignation = currencyNode?.SelectSingleNode("fjs1:designation", nsManager)?.InnerText ?? string.Empty;

                    // Debugging output
                    Console.WriteLine($"Invoice ID: {invoiceId}, Invoice Amount: {invoiceAmount}, Currency: {currencyAlphaCode} {currencyDesignation}");
                }

                return status;
            }

            return "Error in response:invalid Provider code";
        }

        private async Task<string> CallSoapApiAsyncWithPhoneNumber(BillGetRequestDto requestDto)
        {
            // Prepare the SOAP request XML
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            string soapRequest = $@"
<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:amp='http://soprabanking.com/amplitude'>
    <soapenv:Header/>
    <soapenv:Body>
        <amp:getECInvoiceListRequestFlow>
            <amp:requestHeader>
                <amp:requestId>req1</amp:requestId>
                <amp:serviceName>getECInvoiceList</amp:serviceName>
                <amp:timestamp>{timestamp}</amp:timestamp>
                <amp:originalName>TELEBIRR</amp:originalName>
                <amp:languageCode>002</amp:languageCode>
                <amp:userCode>TELEBIRR</amp:userCode>
            </amp:requestHeader>
            <amp:getECInvoiceListRequest>
                <amp:customerIdentification>
                    <amp:providerComponentIdentification>
                        <amp:identifierNumber>3</amp:identifierNumber>
                        <amp:identifierValue>{requestDto.PhoneNumber}</amp:identifierValue>
                    </amp:providerComponentIdentification>
                </amp:customerIdentification>
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

            // Add SOAPAction header if required
            requestMessage.Headers.Add("SOAPAction", "\"getECInvoiceList\"");
            // ? Ensure the Content-Type header is correct
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

            // ? Add Accept header
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

            // Send the SOAP request
            var response = await httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }






        private async Task<string> CallSoapApiAsyncWithProviderIdAndUniqueCode(BillGetRequestDto requestDto)
        {
            // Prepare the SOAP request XML using the provided format for ProviderId and UniqueCode
            string soapRequest = $@"
    <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:amp='http://soprabanking.com/amplitude'>
        <soapenv:Header/>
        <soapenv:Body>
            <amp:getECInvoiceDetailRequestFlow>
                <amp:requestHeader>
                    <amp:requestId>req1</amp:requestId>
                    <amp:serviceName>getECInvoiceDetail</amp:serviceName>
                    <amp:timestamp>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}</amp:timestamp>
                    <amp:originalName>TELEBIRR</amp:originalName>
                    <amp:languageCode>002</amp:languageCode>
                    <amp:userCode>TELEBIRR</amp:userCode>
                </amp:requestHeader>

                <amp:getECInvoiceDetailRequest>
                    <amp:providerInvoiceIdentification>
                        <amp:providerId>{requestDto.ProviderId}</amp:providerId>
                        <amp:invoiceIdentification>
                            <amp:providerComponentIdentification>
                                <amp:identifierNumber>1</amp:identifierNumber>
                                <amp:identifierValue>{requestDto.UniqueCode}</amp:identifierValue>
                            </amp:providerComponentIdentification>
                        </amp:invoiceIdentification>
                    </amp:providerInvoiceIdentification>
                </amp:getECInvoiceDetailRequest>
            </amp:getECInvoiceDetailRequestFlow>
        </soapenv:Body>
    </soapenv:Envelope>";

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var httpClient = new HttpClient(handler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://10.1.7.85:8095/getECInvoiceDetail")
            {
                Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
            };

            // ? Add SOAPAction header if required
            requestMessage.Headers.Add("SOAPAction", "\"getECInvoiceDetail\"");

            // ? Ensure the Content-Type header is correct
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml");

            // ? Add Accept header
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/xml"));

            // Send the SOAP request
            var response = await httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }

        private string ParseSoapResponseWithPhoneNo(string soapResponse, out List<BillGetResponseDto> invoices)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);

            // Register the namespaces
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("fjs1", "http://soprabanking.com/amplitude");
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");

            // Initialize the list to hold invoice details
            invoices = new List<BillGetResponseDto>();

            // Extract the status code
            var statusNode = xmlDoc.SelectSingleNode("//fjs1:responseStatus/fjs1:statusCode", nsManager);
            var status = statusNode?.InnerText ?? "Unknown";

            // Extract invoice details if the response status is OK (statusCode == 0)
            if (status == "0")
            {
                var invoiceNodes = xmlDoc.SelectNodes("//fjs1:getECInvoiceListResponse/fjs1:invoice", nsManager);

                if (invoiceNodes != null)
                {
                    foreach (XmlNode invoiceNode in invoiceNodes)
                    {
                        var invoice = new BillGetResponseDto
                        {
                            ProviderId = invoiceNode.SelectSingleNode("fjs1:providerId", nsManager)?.InnerText ?? string.Empty,
                            InvoiceId = int.TryParse(invoiceNode.SelectSingleNode("fjs1:invoiceId", nsManager)?.InnerText, out var parsedInvoiceId) ? parsedInvoiceId : 0,
                            InvoiceAmount = decimal.TryParse(invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:amount1", nsManager)?.InnerText, out var parsedAmount) ? parsedAmount : 0.00m,
                            CurrencyAlphaCode = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:currency/fjs1:currency/fjs1:alphaCode", nsManager)?.InnerText ?? string.Empty,
                            CurrencyDesignation = invoiceNode.SelectSingleNode("fjs1:invoiceAmount/fjs1:currency/fjs1:currency/fjs1:designation", nsManager)?.InnerText ?? string.Empty,
                            InvoiceIdentificationValue = invoiceNode.SelectSingleNode("fjs1:invoiceIdentification/fjs1:providerComponentIdentification/fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty,
                            CustomerName = GetCustomerName(invoiceNode, nsManager),
                            ProviderName = GetProviderName(invoiceNode, nsManager),
                            CustomerCode = GetUniqueCode(invoiceNode, nsManager)
                        };

                        invoices.Add(invoice);
                    }

                    // Debugging output
                    foreach (var invoice in invoices)
                    {
                        Console.WriteLine($"Invoice ID: {invoice.InvoiceId}, Provider ID: {invoice.ProviderId}, Customer Name: {invoice.CustomerName}, Provider Name: {invoice.ProviderName}, Invoice Amount: {invoice.InvoiceAmount}, Currency: {invoice.CurrencyAlphaCode} {invoice.CurrencyDesignation}, Invoice Identification: {invoice.InvoiceIdentificationValue}");
                    }

                    return status;
                }
            }

            return "Error in response: invalid Phone Number";
        }
        
                    private string GetUniqueCode(XmlNode invoiceNode, XmlNamespaceManager nsManager)
        {
            var Nodes = invoiceNode.SelectNodes("fjs1:invoiceIdentification/fjs1:providerComponentIdentification", nsManager);
            if (Nodes != null)
            {
                foreach (XmlNode node in Nodes)
                {
                    var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                    if (identifierNumber == "1") // Identifier number 2 corresponds to the name
                    {
                        return node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                    }
                }
            }
            return string.Empty;
        }
        private string GetCustomerName(XmlNode invoiceNode, XmlNamespaceManager nsManager)
        {
            var customerNodes = invoiceNode.SelectNodes("fjs1:customerIdentification/fjs1:providerComponentIdentification", nsManager);
            if (customerNodes != null)
            {
                foreach (XmlNode node in customerNodes)
                {
                    var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                    if (identifierNumber == "2") // Identifier number 2 corresponds to the name
                    {
                        return node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                    }
                }
            }
            return string.Empty;
        }

        private string GetProviderName(XmlNode invoiceNode, XmlNamespaceManager nsManager)
        {
            var providerNodes = invoiceNode.SelectNodes("fjs1:customerIdentification/fjs1:providerComponentIdentification", nsManager);
            if (providerNodes != null)
            {
                foreach (XmlNode node in providerNodes)
                {
                    var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                    if (identifierNumber == "4") // Identifier number 4 corresponds to the provider name
                    {
                        return node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                    }
                }
            }
            return string.Empty;
        }




        private string ParseSoapResponseWithProviderIdAndUniqueCode(string soapResponse, out string providerId, out int invoiceId,
                         out string invoiceIdentificationValue, out decimal invoiceAmount,
                         out string currencyAlphaCode, out string currencyDesignation, out string customerName, out string providerName,out string uniqueCode)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);

            // Register the namespaces
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
            customerName = string.Empty;
            providerName = string.Empty;
            uniqueCode=string.Empty;
            // Extract the status code
            var statusNode = xmlDoc.SelectSingleNode("//fjs1:responseStatus/fjs1:statusCode", nsManager);
            var status = statusNode?.InnerText ?? "Unknown";

            // Extract invoice details if the response status is OK (statusCode == 0)
            if (status == "0")
            {
                var invoiceNode = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceDetailResponse/fjs1:invoiceId", nsManager);
                if (invoiceNode != null)
                {
                    providerId = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceDetailResponse/fjs1:providerId", nsManager)?.InnerText ?? string.Empty;
                    invoiceId = int.TryParse(invoiceNode.InnerText, out var parsedInvoiceId) ? parsedInvoiceId : 0;

                    var invoiceAmountNode = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceDetailResponse/fjs1:invoiceAmount/fjs1:amount", nsManager);
                    invoiceAmount = decimal.TryParse(invoiceAmountNode?.InnerText, out var parsedAmount) ? parsedAmount : 0.00m;

                    var currencyNode = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceDetailResponse/fjs1:invoiceAmount/fjs1:currency/fjs1:currency", nsManager);
                    currencyAlphaCode = currencyNode?.SelectSingleNode("fjs1:alphaCode", nsManager)?.InnerText ?? string.Empty;
                    currencyDesignation = currencyNode?.SelectSingleNode("fjs1:designation", nsManager)?.InnerText ?? string.Empty;

                    // Extracting invoice identification values
                    var invoiceIdentificationNode = xmlDoc.SelectSingleNode("//fjs1:getECInvoiceDetailResponse/fjs1:invoiceIdentification", nsManager);
                    invoiceIdentificationValue = invoiceIdentificationNode?.SelectSingleNode("fjs1:providerComponentIdentification/fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;

                    // Extract customer name (identifierNumber = 2)
                    var customerNodes = xmlDoc.SelectNodes("//fjs1:customerIdentification/fjs1:providerComponentIdentification", nsManager);
                    if (customerNodes != null)
                    {
                        foreach (XmlNode node in customerNodes)
                        {
                            var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                            if (identifierNumber == "2") // Identifier number 2 corresponds to the name
                            {
                                customerName = node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                                break; // Stop after finding the name
                            }
                        }
                    }

                    // Extract provider name (identifierNumber = 4)
                    var providerNodes = xmlDoc.SelectNodes("//fjs1:customerIdentification/fjs1:providerComponentIdentification", nsManager);
                    if (providerNodes != null)
                    {
                        foreach (XmlNode node in providerNodes)
                        {
                            var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                            if (identifierNumber == "4") // Identifier number 4 corresponds to the provider name
                            {
                                providerName = node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                                break; // Stop after finding the provider name
                            }
                        }
                    }
            var invoiceIdentification = xmlDoc.SelectNodes("//fjs1:invoiceIdentification/fjs1:providerComponentIdentification", nsManager);

                    if (invoiceIdentification != null)
                    {

                        foreach (XmlNode node in providerNodes)
                        {
                            var identifierNumber = node.SelectSingleNode("fjs1:identifierNumber", nsManager)?.InnerText;
                            if (identifierNumber == "1") // Identifier number 4 corresponds to the provider name
                            {
                                uniqueCode = node.SelectSingleNode("fjs1:identifierValue", nsManager)?.InnerText ?? string.Empty;
                                break; // Stop after finding the provider name
                            }
                        }
                    }
                        // Debugging output
                        Console.WriteLine($"Invoice ID: {invoiceId}, Provider ID: {providerId}, Customer Name: {customerName}, Provider Name: {providerName}, Invoice Amount: {invoiceAmount}, Currency: {currencyAlphaCode} {currencyDesignation}, Invoice Identification: {invoiceIdentificationValue}");

                    return status;
                }
            }

            return "Error in response: invalid Provider code or unique code";
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

