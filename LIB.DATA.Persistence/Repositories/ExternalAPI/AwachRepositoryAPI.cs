using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using DTO;
using Google.Protobuf.WellKnownTypes;
using IRepository;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Repository
{
    public class AwachRepositoryAPI: IAwachRepositoryAPI
    {
        private readonly HttpClient _httpClient;

        public AwachRepositoryAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FinInsInsResponseDTO> CreateAwachTransfer(decimal amount, string accountNo)
        {
            string url = "http://172.16.100.17:8990/AWACH-INT/services"; // Replace with your SOAP endpoint URL
            string messageId = Helper.generateRandomID(35, "msg");
            string TrnsNo = Helper.generateRandomID(10, "trn"); ;

            string soapRequest = $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:awac=""http://temenos.com/AWACH-INT"" xmlns:fun=""http://temenos.com/FUNDSTRANSFERAWACH"">
                   <soapenv:Header/>
                   <soapenv:Body>
                      <awac:banktoAWACHft>
                         <WebRequestCommon>
                            <!--Optional:-->
                            <company></company>
                          <password>F15i#4YTyrTlib</password>
                             <userName>LIBBNK1</userName>
                         </WebRequestCommon>
                         <OfsFunction>
                            <!--Optional:-->
                            <activityName> </activityName>
                            <!--Optional:-->
                            <assignReason> </assignReason>
                            <!--Optional:-->
                            <dueDate> </dueDate>
                            <!--Optional:-->
                            <extProcess> </extProcess>
                            <!--Optional:-->
                            <extProcessID> </extProcessID>
                            <!--Optional:-->
                            <gtsControl> </gtsControl>
                            <!--Optional:-->
                            <messageId>{messageId}</messageId>
                            <!--Optional:-->
                            <noOfAuth>0</noOfAuth>
                            <!--Optional:-->
                            <owner> </owner>
                            <!--Optional:-->
                            <replace> </replace>
                            <!--Optional:-->
                            <startDate> </startDate>
                            <!--Optional:-->
                            <user> </user>
                         </OfsFunction>
                         <FUNDSTRANSFERAWACHType id="" "">
                            <!--Optional:-->
                            <fun:TRANSACTIONTYPE>AC</fun:TRANSACTIONTYPE>
                            <!--Optional:-->
                            <fun:DEBITACCTNO>{accountNo}</fun:DEBITACCTNO>
                            <!--Optional:-->
                            <fun:DEBITCURRENCY>ETB</fun:DEBITCURRENCY>
                            <!--Optional:-->
                            <fun:DEBITTHEIRREF>{TrnsNo} </fun:DEBITTHEIRREF>
                            <!--Optional:-->
                            <fun:CREDITTHEIRREF>ft324</fun:CREDITTHEIRREF>
                            <!--Optional:-->
                            <fun:CREDITACCTNO>1000000139</fun:CREDITACCTNO>
                            <!--Optional:-->
                            <fun:CREDITCURRENCY>ETB</fun:CREDITCURRENCY>
                            <!--Optional:-->
                            <fun:CREDITAMOUNT>{amount}</fun:CREDITAMOUNT>
                            <!--Optional:-->
                            <fun:gORDERINGCUST g=""1"">
                               <!--Zero or more repetitions:-->
                               <fun:ORDERINGCUST></fun:ORDERINGCUST>
                            </fun:gORDERINGCUST>
                         </FUNDSTRANSFERAWACHType>
                      </awac:banktoAWACHft>
                   </soapenv:Body>
                </soapenv:Envelope>
                ";

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();



                XDocument doc = XDocument.Parse(responseString);
                XNamespace ns4 = "http://temenos.com/FUNDSTRANSFER"; // Define the namespace for ns4

                var transactionId = doc.Descendants("transactionId").FirstOrDefault()?.Value;
                var successIndicator = doc.Descendants("successIndicator").FirstOrDefault()?.Value;
                string message = "";
                if(successIndicator == "T24Error")
                {
                    message = doc.Descendants("messages").FirstOrDefault()?.Value;
                }
                var res  = new FinInsInsResponseDTO()
                {
                    success = successIndicator == "Success" ? true : false,
                    message = message =="" ?successIndicator : message,
                    FinInstransactionId = transactionId
                };
                return res;
            }
            
            catch (Exception ex)
            {
                var res = new FinInsInsResponseDTO()
                {
                    success = true,
                    message = "pending",
                    FinInstransactionId = ""
                };
                return res;
            }
        }


    }
}
