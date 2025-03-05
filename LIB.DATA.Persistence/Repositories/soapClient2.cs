using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LIB.API.Persistence.Repositories
{
    public class SoapClient2
    {
        private readonly HttpClient _httpClient;

        public SoapClient2(HttpClient httpClient)
        {

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            // Pass the handler to httpclient(from you are calling api)
            HttpClient client = new HttpClient(clientHandler);
            _httpClient = client;
        }
        public async Task<string> SendSoapRequestAsync(string url, string xmlRequest, string soapAction)
        {
            try
            {
                var content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("SOAPAction", soapAction);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode(); // Ensure the request was successful

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // Handle specific status codes if needed
                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        throw new Exception("Service temporarily unavailable.");
                    }
                    else
                    {
                        throw new HttpRequestException($"Failed to send SOAP request. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending SOAP request.", ex);
            }
        }

        public (bool isSuccess, string reason) IsSuccessfulResponse(string xmlResponse)
        {
            var xdoc = XDocument.Parse(xmlResponse);

            // Check for <statusCode>0</statusCode>
            var statusCodeElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusCode");
            if (statusCodeElement == null || statusCodeElement.Value != "0")
            {
                var addtlInfElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "AddtlInf");
                return (false, addtlInfElement != null ? addtlInfElement.Value : "Invalid statusCode");
            }

            // Check for <statusCode>0</statusCode>

            var availableBalanceElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "availableBalance");
           
            if (availableBalanceElement == null || availableBalanceElement.Value == "0")
            {
                var addtlInfElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "AddtlInf");
                return (false, addtlInfElement != null ? addtlInfElement.Value : "Invalid availableBalance");
            }
            // Load the XML string into an XDocument


            return (true, availableBalanceElement.Value)
;        }



        /*
            public bool IsSuccessfulResponse(string xmlResponse)
            {
               var xdoc = XDocument.Parse(xmlResponse);

                // Check for <statusCode>0</statusCode>
                var statusCodeElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "statusCode");
                if (statusCodeElement == null || statusCodeElement.Value != "0") return false;
                // Load the XML string into an XDocument
                XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());
                nsManager.AddNamespace("fjs1", "http://soprabanking.com/amplitude");
                nsManager.AddNamespace("iso", "urn:iso:std:iso:20022:tech:xsd:pain.002.001.03DB");

                // Find the <fjs1:pain002> element
                var pain002Element = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "pain002" && e.Name.Namespace == "http://soprabanking.com/amplitude");
                if (pain002Element == null) return false;

                // Extract the XML within <fjs1:pain002> element
                string pain002Xml = pain002Element.Value;

                // Load the extracted XML as a new XDocument
                XDocument extractedXmlDoc = XDocument.Parse(pain002Xml);

                // Check for the desired elements within the extracted XML using the namespace manager
                var dtldStsElement = extractedXmlDoc.XPathSelectElement("//iso:DtldSts", nsManager);
                if (dtldStsElement == null || dtldStsElement.Value != "ACSP") return false;

                var pmtInfStsElement = extractedXmlDoc.XPathSelectElement("//iso:PmtInfSts", nsManager);
                if (pmtInfStsElement == null || pmtInfStsElement.Value != "ACSP") return false;

                var txStsElement = extractedXmlDoc.XPathSelectElement("//iso:TxSts", nsManager);
                if (txStsElement == null || txStsElement.Value != "ACSP") return false;


                return true;



            }*/
    }
}
