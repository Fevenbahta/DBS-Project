using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using MySqlX.XDevAPI;

using System.Linq;
using System.Net;
using Newtonsoft.Json;
using DTO;
using IRepository;

namespace Repository
{
    public class MpesaRepositoryAPI: IMpesaRepositoryAPI
    {
        private readonly HttpClient _httpClient;
        public MpesaRepositoryAPI(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        private async Task<string> GenerateMpesaToken()
        {
            ServicePointManager.ServerCertificateValidationCallback =
         (sender, certificate, chain, sslPolicyErrors) => true;
            var url = "https://apisandbox.safaricom.et/v1/token/generate?grant_type=client_credentials"; 
            var username = "jgdPJACy5TPGniKsmxahZgjYsfCn9ILI2LZanmk5kukRycGd"; 
            var password = "nLIqp5u77VqO2TKshWYAOaG33sMiCkGh7g6NOcnLKFsoneFpwnwskDaOMWs4Vgc1"; 

            using (var client = new HttpClient())
            {
                // Set up Basic Authentication
                var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                try
                {
                    // Make the GET request
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Read the response content
                    var content = await response.Content.ReadAsStringAsync();
                    MpesaTokenRes res = JsonConvert.DeserializeObject<MpesaTokenRes>(content);
                    return res.access_token;
                }
                catch (HttpRequestException e)
                {
                    throw e;
                }
            }
        }

        public async Task<FinInsInsResponseDTO> CreateMpesaTransfer(decimal amount, string phoneNo)
        {
            string url = "https://api.ipg.safaricom.et:27000/iap/request/"; 
           string token  =  await GenerateMpesaToken();
            ServicePointManager.ServerCertificateValidationCallback =
        (sender, certificate, chain, sslPolicyErrors) => true;
            string Conversationid = Helper.generateRandomID(34, "B2C");
            string soapRequest = $@"
                            <soapenv:Envelope xmlns:soapenv="" http://schemas.xmlsoap.org/soap/envelope/ "" xmlns:SOAP-ENV="" http://schemas.xmlsoap.org/soap/envelope/ "" xmlns:req="" http://api-v1.gen.mm.vodafone.com/mminterface/request "" xmlns:v2="" http://www.huawei.com.cn/schema/common/v2_1 "">
                    <soapenv:Header>
                        <tns:RequestSOAPHeader xmlns:tns="" http://www.huawei.com/schema/osg/common/v2_1 "">
                            <tns:spId>107031</tns:spId>
                            <tns:spPassword>MzRmNzk5NWJmNGJkNzFmYzdkODYyMGZhNDhiYTgzYWFmZjliMTQxMTQwNTUyODZmMzJiZDFkNDhiZTIyYWY3OQ==</tns:spPassword>
                            <tns:timeStamp>20190910115015</tns:timeStamp>
                            <tns:serviceId>107031000</tns:serviceId>
                        </tns:RequestSOAPHeader>
                    </soapenv:Header>
                    <soapenv:Body>
                        <req:RequestMsg>
                            <![CDATA[
                <?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?><request xmlns="" http://api-v1.gen.mm.vodafone.com/mminterface/request ""><Transaction>
            <CommandID>BusinessPayment</CommandID><LanguageCode>0</LanguageCode><OriginatorConversationID>{Conversationid}</OriginatorConversationID><ConversationID/><Remark>Testing 21.1</Remark>
                    <Parameters><Parameter><Key>Amount</Key><Value>{amount}</Value></Parameter>
                </Parameters><ReferenceData><ReferenceItem><Key>BankTransactionID</Key><Value>FT12b756u45</Value></ReferenceItem>
                </ReferenceData>
                <Timestamp>2021-02-03UTC'08:46:43.0000000</Timestamp>
                </Transaction>
                <Identity><Caller><CallerType>2</CallerType>
                <ThirdPartyID>broker_4</ThirdPartyID>
                <Password>TgR7bxv4K0cCKhzJz0UTs0O+o8vyFrqN2kD8ioQAmi/l+pzfe4VL3+dEfvzIZZopppWqIqJpCDndLxeNU98dQZGWIWqxss2MkdVW6FYVK2kcyPfo3Ofq6mf5K23CmuSyBoWxSv/yZi8/qABWYbWKOmLm7Efo6g4eHo1hM4RJ3fGjX3/TKuf5D53l+UYXbXcQcp6Wk9nlfPp3/uNkeLwtOLxdF7mldxWLqza/o1FNwSppbcZtRe7QTURhW6E0wfzyVjyZ2ADXggpgaikFPVPU7+tAreWKu35zwbDaQlqR8buitmSey3hnWw11OLRcZxIjcSoDVtEC7Ncpz/ie4P2vSw==</Password>
                <CheckSum>CheckSum0</CheckSum>
                <ResultURL>https://apigee-listener.oat.mpesa.safaricomet.net/api/b2c/result</ResultURL>
                </Caller>
                <Initiator>
                    <IdentifierType>11</IdentifierType>
                <Identifier>testapi</Identifier>
                <SecurityCredential>:i+D+DDfvgu7xVglD+OrpjyLXpMDjQ59vibOU3qWNVNmT9MeRZn5IasvPLhNJWWJkRl1TtLVePYbfhKmgNNmIg1iRKGOBxnp8z2VZeri/TWLuSlir48Mf0SJ6gUGnf3YeE3AtzXj8VUH62WPuC4DTL1sXp14zJuJ2mDsS9w+a2xoyLphAXDhhvkqNsjavhhUj7TLYcvRMAN6ky7E4C3hMxM5Bdldkc/rGUSB0N9fPqlMq+GRm/G4zCvCOoqhlbFJyXHNkteX+F0FoqSVo5BFfemy+ICkxStZj9tHT1vukKMFOA4P8cBAUvZZJOcDEVfT+yMSGeoSR+sKQUf2xQVNt0Q==</SecurityCredential>
                <ShortCode>90000803</ShortCode>
                </Initiator>
                <PrimaryParty>
                    <IdentifierType>4</IdentifierType>
                    <Identifier>90000803</Identifier>
                    <ShortCode>90000803</ShortCode>
                    </PrimaryParty>
                    <ReceiverParty>
                        <IdentifierType>1</IdentifierType>
                        <Identifier>{phoneNo}</Identifier>
                        <ShortCode></ShortCode>
                        </ReceiverParty>
                        <AccessDevice>
                            <IdentifierType>1</IdentifierType>
                        <Identifier>172.31.180.37</Identifier>
                        </AccessDevice>
                </Identity><KeyOwner>1</KeyOwner>
                </request>]]>
                </req:RequestMsg>
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
                XNamespace req = "http://api-v1.gen.mm.vodafone.com/mminterface/request";

                var ResponseMsg = doc.Descendants(req + "ResponseMsg").FirstOrDefault()?.Value;

                XDocument ResponseMsgdoc = XDocument.Parse(ResponseMsg);
                XNamespace res = "http://api-v1.gen.mm.vodafone.com/mminterface/response";

                var ResponseCode = ResponseMsgdoc.Descendants(res + "ResponseCode").FirstOrDefault()?.Value;
                var ResponseDesc = ResponseMsgdoc.Descendants(res + "ResponseDesc").FirstOrDefault()?.Value;
                var ConversationID = ResponseMsgdoc.Descendants(res + "ConversationID").FirstOrDefault()?.Value;
                var successIndicator = ResponseMsgdoc.Descendants(res + "ServiceStatus").FirstOrDefault()?.Value;

                var resp = new FinInsInsResponseDTO()
                {
                    success = ResponseCode == "0" ? true : false,
                    message = ResponseDesc,
                    ConversationID = ConversationID
                };
                return resp;
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
