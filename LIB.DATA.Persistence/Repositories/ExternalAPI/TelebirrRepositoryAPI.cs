using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using DTO;
using Google.Protobuf.WellKnownTypes;
using IRepository;
using static Google.Protobuf.Reflection.UninterpretedOption.Types;

namespace Repository
{
    public class TelebirrRepositoryAPI : ITelebirrRepositoryAPI
    {
        private readonly HttpClient _httpClient;

        public TelebirrRepositoryAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<FinInsInsResponseDTO> CreateTelebirrTransaction(decimal amount, string phoneNo)
        {
            string url = "http://10.180.79.13:30001/payment/services/APIRequestMgrService"; // Replace with your SOAP endpoint URL
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            string conversationId = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString();
            Random random = new Random();
            int randomNumberLength = 15 - conversationId.Length;
            conversationId = conversationId + random.Next((int)Math.Pow(10, randomNumberLength - 1), (int)Math.Pow(10, randomNumberLength)).ToString();
            string soapRequest = $@"
                            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:api=""http://cps.huawei.com/cpsinterface/api_requestmgr"" xmlns:req=""http://cps.huawei.com/cpsinterface/request"" xmlns:com=""http://cps.huawei.com/cpsinterface/common"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <api:Request>
                         <req:Header>
                            <req:Version>1.0</req:Version>
                            <req:CommandID>InitTrans_DepositfromBankOrg</req:CommandID>
                            <req:OriginatorConversationID>{conversationId}</req:OriginatorConversationID>
                            <req:Caller>
                               <req:CallerType>2</req:CallerType>
                               <req:ThirdPartyID>Lion bank</req:ThirdPartyID>
                               <req:Password>oLEB29nY+Xp9mWaWPsPZj+IPakY82Tc09KO4JLUP2fU=</req:Password>
                               <req:ResultURL>http://10.180.73.190:8099/mock</req:ResultURL>
                            </req:Caller>
                            <req:KeyOwner>1</req:KeyOwner>
                            <req:Timestamp>${timestamp}</req:Timestamp>
                         </req:Header>
                         <req:Body>
                            <req:Identity>
                               <req:Initiator>
                                  <req:IdentifierType>12</req:IdentifierType>
                                  <req:Identifier>0002101</req:Identifier>
                                  <req:SecurityCredential>o3Z1jvD4dIemW8VaX5c42GfGH+WU0lfKI6QiSF2qlIA=</req:SecurityCredential>
                                  <req:ShortCode>00021</req:ShortCode>
                               </req:Initiator>
                               <req:ReceiverParty>
                                  <req:IdentifierType>1</req:IdentifierType>
                                  <req:Identifier>${phoneNo}</req:Identifier>
                               </req:ReceiverParty>
                            </req:Identity>
                            <req:TransactionRequest>
                               <req:Parameters>
                                  <req:Amount>${amount}</req:Amount>
                                  <req:Currency>ETB</req:Currency>
                               </req:Parameters>
                            </req:TransactionRequest>
                            <req:Remark>test</req:Remark>
                         </req:Body>
                      </api:Request>
                   </soapenv:Body>
                </soapenv:Envelope> ";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();

                XDocument doc = XDocument.Parse(responseString);
                XNamespace res = "http://cps.huawei.com/cpsinterface/response"; // Define the namespace for ns4

                var ResponseCode = doc.Descendants(res + "ResponseCode").FirstOrDefault()?.Value;
                var ResponseDesc = doc.Descendants(res + "ResponseDesc").FirstOrDefault()?.Value;
                var ConversationID = doc.Descendants(res + "ConversationID").FirstOrDefault()?.Value;
                var successIndicator = doc.Descendants(res + "ServiceStatus").FirstOrDefault()?.Value;
                string message = "";

                var ressponse = new FinInsInsResponseDTO()
                {
                    success = ResponseCode == "0" ? true : false,
                    message = ResponseDesc,
                    ConversationID = ConversationID
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