using System;
using System.Buffers;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.Contracts.Persistent;
using LIB.API.Application.DTOs;
using LIB.API.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;

namespace LIB.API.Persistence.Repositories
{
    public class ConfirmOrderRepository : IConfirmOrderRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LIBAPIDbSQLContext _context;
        private readonly IConfiguration _configuration;
        private readonly SoapClient _soapClient;
        private readonly IDetailRepository _detailRepository;

        // Constructor to inject dependencies
        public ConfirmOrderRepository(IHttpClientFactory httpClientFactory, LIBAPIDbSQLContext context, IConfiguration configuration,
             SoapClient soapClient, IDetailRepository detailRepository)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _configuration = configuration;
          _soapClient = soapClient;
            _detailRepository = detailRepository;
        }

        // Method to confirm order asynchronously
        public async Task<TransactionResponseDto> CreateTransferAsync(
        decimal Amount,
        string DAccountNo,
        string OrderId,
        string ReferenceNo,
        string traceNumber,
        string merchantCode)
        {
            try
            {
                var userDetails = await _detailRepository.GetUserDetailsByAccountNumberAsync(DAccountNo);

                if (userDetails == null || string.IsNullOrEmpty(userDetails.BRANCH))
                {
                    await LogErrorToAirlinesErrorAsync("UserDetailsCheck", DAccountNo, "Account Number is invalid", "", "CreateTransfer", ReferenceNo);
                    throw new Exception("User Account Number  is Invalid. Transaction aborted.");
                }

                string DAccountBranch = userDetails?.BRANCH;
                string DAccountName = userDetails?.FULL_NAME;
                string CAccountNo = "24101900001";

                bool transferSuccess = await CreateTransferAsync(Amount, DAccountNo, DAccountBranch, CAccountNo, ReferenceNo);

                if (!transferSuccess)
                {
                    await LogErrorToAirlinesErrorAsync("Transfer", DAccountNo, "reason", "", "CreateTransfer", ReferenceNo);
                    throw new Exception("Transfer failed. Transaction aborted.");
                }

                var confirmOrder = new ConfirmOrders
                {
                    OrderId = OrderId,
                    Amount = Amount,
                    Currency = "ETB",
                    Status = 1,
                    Remark = "Transfer Successful",
                    TraceNumber = traceNumber,
                    ReferenceNumber = ReferenceNo,
                    PaidAccountNumber = DAccountNo,
                    PayerCustomerName = DAccountName,
                    ShortCode = merchantCode,
                    RequestDate = DateTime.UtcNow
                };

                await _context.confirmorders.AddAsync(confirmOrder);
                await _context.SaveChangesAsync();

                string baseUrl = "https://ethiopiangatewaytest.azurewebsites.net/";
                string url = $"{baseUrl}Lion/api/V1.0/Lion/ConfirmOrder";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                var jsonContent = JsonSerializer.Serialize(confirmOrder);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(request);

               

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var confirmOrderResponse = JsonSerializer.Deserialize<ConfirmOrderResponseDto>(jsonResponse);

                confirmOrder.ExpireDate = confirmOrderResponse?.ExpireDate;
                confirmOrder.StatusCodeResponse = confirmOrderResponse?.StatusCodeResponse ?? 0;
                confirmOrder.StatusCodeResponseDescription = confirmOrderResponse?.StatusCodeResponseDescription ?? "Empty response";
                confirmOrder.CustomerName = confirmOrderResponse?.CustomerName ?? "Empty response";
                confirmOrder.MerchantId = confirmOrderResponse?.MerchantId ?? 0;
                confirmOrder.MerchantCode = confirmOrderResponse?.MerchantCode ?? "Empty response";
                confirmOrder.MerchantName = confirmOrderResponse?.MerchantName ?? "Empty response";
                confirmOrder.Message = confirmOrderResponse?.Message ?? "Empty response";
                confirmOrder.ResponseDate = DateTime.UtcNow;

                _context.confirmorders.Update(confirmOrder);
                await _context.SaveChangesAsync();

                // Return only the StatusCodeResponse and OrderId
                return new TransactionResponseDto
                {
                    Status ="Successful Transaction",
                       Id = ReferenceNo
                };
            }
            catch (Exception ex)
            {
                await LogErrorToAirlinesErrorAsync("ConfirmOrderAsync", DAccountNo, "ShortCode", ex.Message, "ConfirmOrder", ReferenceNo);
                throw new Exception("Error in ConfirmOrderAsync: " + ex.Message);
            }
        }


        public async Task<bool> CreateTransferAsync(
           decimal Amount,
           string DAccountNo,
           string DAccountBranch,
           string CAccountNo,
           string RefrenceNo)
        {
            // Generate unique identifiers for the transfer
            string requestId = GenerateRequestId();
            string msgId = GenerateMsgId();
            string pmtInfId = GeneratePmtInfId();
            string instrId = GenerateInstrId();
            string endToEndId = GenerateEndToEndId();

            // Get current timestamp
            var formattedDate = GetCurrentTimestamp();

            // Define SOAP Action
            string soapAction = "createTransfer";

            // Create the XML request (same as your current code)
            string xmlRequest = $@"

        <soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:amp='http://soprabanking.com/amplitude'>
          <soapenv:Header/>
          <soapenv:Body>
            <amp:createTransferRequestFlow>
              <amp:requestHeader>
                <amp:requestId>{requestId}</amp:requestId>
                <amp:serviceName>createTransfer</amp:serviceName>
                <amp:timestamp>{formattedDate}</amp:timestamp>
                <amp:originalName>SACCOAPP</amp:originalName>
                <amp:userCode>SACCO</amp:userCode>
              </amp:requestHeader>
              <amp:createTransferRequest>
                <amp:canal>SACCOPAYIN</amp:canal>
                <amp:pain001><![CDATA[
                <Document xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns='urn:iso:std:iso:20022:tech:xsd:pain.001.001.03DB'>
                  <CstmrCdtTrfInitn>
                    <GrpHdr>
                      <MsgId>{msgId}</MsgId>
                      <CreDtTm>{formattedDate}</CreDtTm>
                      <NbOfTxs>1</NbOfTxs>
                      <CtrlSum>{Amount}</CtrlSum>
                      <InitgPty/>
                      <DltPrvtData>
                        <FlwInd>PROD</FlwInd>
                        <DltPrvtDataDtl>
                          <PrvtDtInf>SACCOPAYIN</PrvtDtInf>
                          <Tp>
                            <CdOrPrtry>
                              <Cd>CHANNEL</Cd>
                            </CdOrPrtry>
                          </Tp>
                        </DltPrvtDataDtl>
                      </DltPrvtData>
                    </GrpHdr>
                    <PmtInf>
                      <PmtInfId>{pmtInfId}</PmtInfId> <!-- Unique PmtInfId for each request -->
                      <PmtMtd>TRF</PmtMtd>
                      <BtchBookg>0</BtchBookg>
                      <NbOfTxs>1</NbOfTxs>
                      <CtrlSum>{Amount}</CtrlSum>
                      <DltPrvtData>
                        <OrdrPrties>
                          <Tp>IMM</Tp>
                          <Md>CREATE</Md>
                        </OrdrPrties>
                      </DltPrvtData>
                      <PmtTpInf>
                        <InstrPrty>NORM</InstrPrty>
                        <SvcLvl>
                          <Prtry>INTERNAL</Prtry>
                        </SvcLvl>
                      </PmtTpInf>
                      <ReqdExctnDt>1901-01-01</ReqdExctnDt>
                      <Dbtr>
                      </Dbtr>
                      <DbtrAcct>
                        <Id>
                          <Othr>
                            <Id>{DAccountNo}</Id>
                            <SchmeNm>
                              <Prtry>BKCOM_ACCOUNT</Prtry>
                            </SchmeNm>
                          </Othr>
                        </Id>
                        <Ccy>ETB</Ccy>
                      </DbtrAcct>
                      <DbtrAgt>
                        <FinInstnId>
                          <Nm>BANQUE</Nm>
                          <Othr>
                            <Id>00011</Id>
                            <SchmeNm>
                              <Prtry>ITF_DELTAMOP_IDETAB</Prtry>
                            </SchmeNm>
                          </Othr>
                        </FinInstnId>
                        <BrnchId>
                          <Id>{DAccountBranch}</Id>
                          <Nm>Agence</Nm>
                        </BrnchId>
                      </DbtrAgt>
                      <CdtTrfTxInf>
                        <PmtId>
                          <InstrId>{instrId}</InstrId>
                          <EndToEndId>{endToEndId}</EndToEndId>
                        </PmtId>
                        <Amt>
                          <InstdAmt Ccy='ETB'>{Amount}</InstdAmt>
                        </Amt>
                        <CdtrAgt>
                          <FinInstnId>
                            <Nm>BANQUE</Nm>
                            <Othr>
                              <Id>00011</Id>
                              <SchmeNm>
                                <Prtry>ITF_DELTAMOP_IDETAB</Prtry>
                              </SchmeNm>
                            </Othr>
                          </FinInstnId>
                          <BrnchId>
                            <Id>00018</Id>
                            <Nm>Agence</Nm>
                          </BrnchId>
                        </CdtrAgt>
                        <Cdtr>
                        </Cdtr>
                        <CdtrAcct>
                          <Id>
                            <Othr>
                              <Id>{CAccountNo}</Id>
                              <SchmeNm>
                                <Prtry>BKCOM_ACCOUNT</Prtry>
                              </SchmeNm>
                            </Othr>
                          </Id>
                          <Ccy>ETB</Ccy>
                        </CdtrAcct>
                        <RmtInf>
                          <Ustrd>{pmtInfId}</Ustrd>
                        </RmtInf>
                      </CdtTrfTxInf>
                    </PmtInf>
                  </CstmrCdtTrfInitn>
                </Document>
                ]]></amp:pain001>
              </amp:createTransferRequest>
            </amp:createTransferRequestFlow>
          </soapenv:Body>
        </soapenv:Envelope>";


            // Send the SOAP request
            var soapResponse = await _soapClient.SendSoapRequestAsync("https://10.1.7.85:8095/createTransfer", xmlRequest, soapAction);

            // Handle the SOAP response
            var (isSuccess, reason) = _soapClient.IsSuccessfulResponse(soapResponse);

              // Save transfer data to AirlinesTransferTable
            var transferRecord = new AirlinesTransfer
            {
                RequestId = RefrenceNo,
                MsgId = msgId,
                PmtInfId = pmtInfId,
                InstrId = instrId,
                EndToEndId = endToEndId,
                Amount = Amount,
                DAccountNo = DAccountNo,
                CAccountNo = CAccountNo,
                DAccountBranch = DAccountBranch,
                ResponseStatus = isSuccess ? "Success" : "Failed", // Set status based on success or failure
                TransferDate = DateTime.UtcNow,  // Current timestamp for when the transfer was initiated
                   ErrorReason = isSuccess ? null : reason  // Store error message if the transfer fails
            };
     if (!isSuccess)
            {
                // Log the error if the response is not successful

               await LogErrorToAirlinesErrorAsync("Transfer", DAccountNo, "reason", reason, "CreateTransfer", RefrenceNo);
            }

     
            // Save the transfer record to the database
            await _context.airlinestransfer.AddAsync(transferRecord);
            await _context.SaveChangesAsync();
            return isSuccess;
        }


        private async Task LogErrorToAirlinesErrorAsync(string methodName, string orderId, string shortCode, string errorMessage, string errorType, string reference)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = errorMessage,
                Severity = "ERROR",
                Type = "BUS",
                Source = methodName,  // Log the method name where the error occurred
                Origin = "AirlinesConfirmOrderRepository",  // This is where the error happened
                SpanId = orderId,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in {methodName} for OrderId: {orderId}, ShortCode: {shortCode}" }
        }
            };

            // Serialize the feedback object to JSON or a custom format
            string feedbackJson = JsonSerializer.Serialize(feedback);

            var errorRecord = new AirlinesError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = reference,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = errorType
            };

            _context.airlineserror.Add(errorRecord);
            await _context.SaveChangesAsync();
        }


        private string GenerateRequestId() => GenerateNumericId(17);

        private string GenerateMsgId() => GenerateAlphanumericId(24);


        private string GeneratePmtInfId()
        {
            string alphanumericId = GenerateAlphanumericId(20); // Generate 17 alphanumeric characters
            return "AIR" + alphanumericId;
        }


        private string GenerateInstrId() => GenerateAlphanumericId(32);

        private string GenerateEndToEndId() => GenerateAlphanumericId(30);

        private string GenerateNumericId(int length)
        {
            Random random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => (char)('0' + random.Next(10))).ToArray());
        }

        private string GenerateAlphanumericId(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        private string GetCurrentTimestamp() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");








    }
}
