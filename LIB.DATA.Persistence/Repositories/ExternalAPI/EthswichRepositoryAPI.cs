using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DTO;
using IRepository;

namespace Repository
{
    public class EthswichRepositoryAPI: IEthswichRepositoryAPI
    {
        private readonly HttpClient _httpClient;

        public EthswichRepositoryAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<FinInsInsResponseDTO> CreateEthswichTransaction(decimal amount, string accountNo, string instId)
        {
            string url = "http://192.168.20.5:7003/webgate/SVWS/Transfer"; 
            var cuurenDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
           // var cuurenDate = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            string refNum = Helper.generateRandomID(10, "");
            string orginrefNum = Helper.generateRandomID(12, "ENQ");
            string soapRequest = $@"
                           <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ws=""http://ws.webgate.bpc.ru/"">
                        <soapenv:Header>
                        <ws:credentials>
                            <user>oeIoUEZGz4#</user>
                            <pass>$3mhGuOk2DB</pass>
                               </ws:credentials>
                                </soapenv:Header>
                                <soapenv:Body>
                                <ws:a2aCredit>
                                <a2aCredit>
                                <destAccount>
                                <instId>{instId}</instId>
                                <accountNumber>{accountNo}</accountNumber>
                                </destAccount>
                                <acqTerminalDetails>
                                <terminalId>EPOS05</terminalId>
                                 <!--Optional:-->
                                <terminalName>EthSwitch</terminalName>
                                <merchantId>ETSMER0001</merchantId>
                                <mcc>6011</mcc>
                                <acqInstId>231443</acqInstId>
                                 <!--Optional:-->
                                <street>Addis</street>
                                 <!--Optional:-->
                                <city>Addis</city>
                                 <!--Optional:-->
                                <state>Addis</state>
                                 <!--Optional:-->
                                <country>ETH</country>
                              <!--Optional:-->
                         <postalCode>1000</postalCode>
                         </acqTerminalDetails>
                         <sourceBin>231443</sourceBin>
                         <refNum>{refNum}</refNum>
                          <!--Optional:-->
                         <message>Funds Transfer</message>
                         <localTransactionDateTime>{cuurenDate}</localTransactionDateTime>
                        <sttlDate>{cuurenDate}</sttlDate>
                        <amount>
                          <amount>{amount}</amount>
                          <currency>230</currency>
                        </amount>
                        <!--Optional:-->
                        <origRefNum>{orginrefNum}</origRefNum>
                        </a2aCredit>
                        </ws:a2aCredit>
                        </soapenv:Body>
                        </soapenv:Envelope>";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                var contents = await response.Content.ReadAsStringAsync();

                string responseString = await response.Content.ReadAsStringAsync();
                XDocument doc = XDocument.Parse(responseString);
                XNamespace env = "http://schemas.xmlsoap.org/soap/envelope/";

                var Fault = doc.Descendants("faultstring").FirstOrDefault()?.Value;
                var ResponseCode = doc.Descendants("status").FirstOrDefault()?.Value;
                var refnum = doc.Descendants("refnum").FirstOrDefault()?.Value;
                string message = "";

                var ressponse = new FinInsInsResponseDTO()
                {
                    success = Fault == null ? true : false,
                    message = Fault == null ? ResponseCode : Fault,
                    FinInstransactionId = Fault == null ? refnum : ""
                };
                return ressponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw ex;
            }
        }
    }
}