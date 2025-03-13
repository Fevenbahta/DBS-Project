using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

public class SoapClient
{
    private readonly HttpClient _httpClient;

    public SoapClient(HttpClient httpClient)
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

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
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

        // Load the XML string into an XDocument
        XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());
        nsManager.AddNamespace("fjs1", "http://soprabanking.com/amplitude");
        nsManager.AddNamespace("iso", "urn:iso:std:iso:20022:tech:xsd:pain.002.001.03DB");

        // Find the <fjs1:pain002> element
        var pain002Element = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "pain002" && e.Name.Namespace == "http://soprabanking.com/amplitude");
        if (pain002Element == null)
        {
            var addtlInfElement = xdoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "AddtlInf");
            return (false, addtlInfElement != null ? addtlInfElement.Value : "Missing pain002 element");
        }

        // Extract the XML within <fjs1:pain002> element
        string pain002Xml = pain002Element.Value;

        // Load the extracted XML as a new XDocument
        XDocument extractedXmlDoc = XDocument.Parse(pain002Xml);

        // Check for the desired elements within the extracted XML using the namespace manager
        var dtldStsElement = extractedXmlDoc.XPathSelectElement("//iso:DtldSts", nsManager);
        if (dtldStsElement == null || dtldStsElement.Value != "ACSP")
        {
            var addtlInfElement = extractedXmlDoc.XPathSelectElement("//iso:AddtlInf", nsManager);
            return (false, addtlInfElement != null ? addtlInfElement.Value : "DtldSts check failed");
        }

        var pmtInfStsElement = extractedXmlDoc.XPathSelectElement("//iso:PmtInfSts", nsManager);
        if (pmtInfStsElement == null || pmtInfStsElement.Value != "ACSP")
        {
            var addtlInfElement = extractedXmlDoc.XPathSelectElement("//iso:AddtlInf", nsManager);
            return (false, addtlInfElement != null ? addtlInfElement.Value : "PmtInfSts check failed");
        }

        var txStsElement = extractedXmlDoc.XPathSelectElement("//iso:TxSts", nsManager);
        if (txStsElement == null || txStsElement.Value != "ACSP")
        {
            var addtlInfElement = extractedXmlDoc.XPathSelectElement("//iso:AddtlInf", nsManager);
            return (false, addtlInfElement != null ? addtlInfElement.Value : "TxSts check failed");
        }

        return (true, null);
    }



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
