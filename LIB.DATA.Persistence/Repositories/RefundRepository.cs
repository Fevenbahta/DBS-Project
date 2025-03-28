using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LIB.API.Application.Contracts.Persistence;
using LIB.API.Application.Contracts.Persistence.LIB.API.Repositories;
using LIB.API.Application.Contracts.Persistent;
using LIB.API.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LIB.API.Persistence.Repositories
{

        public class RefundRepository : IRefundRepository
    {
            private readonly SoapClient _soapClient;
            private readonly LIBAPIDbSQLContext _context;
        private readonly IDetailRepository _detailRepository;

        public RefundRepository(SoapClient soapClient, LIBAPIDbSQLContext context,IDetailRepository detailRepository)
            {
                _soapClient = soapClient;
                _context = context;
            _detailRepository = detailRepository;
        }
      string  MerchantCode = "526341";


            public async Task<bool> ProcessRefundAsync(
            RefundRequest refundRequest)
            {

            var transaction = await _context.airlinestransfer
                .FirstOrDefaultAsync(t => t.ReferenceNo == refundRequest.RefundReferenceCode
                                        && t.ResponseStatus == "Success"
                                        && t.MerchantCode == MerchantCode
                                        && t.OrderId == refundRequest.OrderId);

            if (transaction == null)
            {
                await LogErrorToAirlinesErrorAsync("ReferenceNotFound", refundRequest.RefundAccountNumber, "Reference number not found in AirlinesTransaction", "", "ProcessRefund", refundRequest.ReferenceNumber);
                throw new Exception("Reference number not found or status is not 'success' in AirlinesTransaction.");
            }


            // Step 2: Check if the transaction amount is less than the sum of the refund amount + requested refund amount
            var refunds = await _context.refunds
      .Where(r => r.RefundReferenceCode == refundRequest.RefundReferenceCode
       && r.OrderId == refundRequest.OrderId
                && r.ShortCode ==MerchantCode)
      .OrderByDescending(r => r.TransferDate)  // Get latest refunds first
      .ToListAsync();


            // Calculate total refunded amount for this reference number
            decimal totalRefundedAmount = refunds.Sum(r => r.Amount);

            // Check if the new refund request exceeds the transaction amount
            if (totalRefundedAmount + refundRequest.Amount > transaction.Amount)
            {
                await LogErrorToAirlinesErrorAsync(
                    "AmountExceeded",
                    refundRequest.RefundAccountNumber,
                    "Refund amount exceeds transaction amount",
                    "",
                    "ProcessRefund",
                    refundRequest.ReferenceNumber
                );

                throw new Exception("Refund amount exceeds the transaction amount.");
            }















            // Call the CreateTransferAsync method to process the SOAP request
            var userDetails = await _detailRepository.GetUserDetailsByAccountNumberAsync(refundRequest.RefundAccountNumber);

            if (userDetails == null || string.IsNullOrEmpty(userDetails.BRANCH))
            {
                await LogErrorToAirlinesErrorAsync("UserDetailsCheck", refundRequest.RefundAccountNumber, "Account Number  is Invalid", "", "CreateTransfer", refundRequest.ReferenceNumber);
                throw new Exception("User Account Number  is Invalid. Transaction aborted.");
            }

            string CAccountBranch = userDetails?.BRANCH;


            string dAccountBranch ="00003";
            string DAccountNo = "00310049462";

                bool isTransferSuccess = await CreateTransferAsync(refundRequest.Amount, DAccountNo, dAccountBranch, refundRequest.RefundAccountNumber, CAccountBranch, refundRequest.RefundReferenceCode, refundRequest);

                if (!isTransferSuccess)
                {
                    // If transfer failed, log the error in the database
                    await LogErrorToAirlinesErrorAsync("Transfer", refundRequest.RefundAccountNumber, "Transfer Failed", "SOAP request failed", "CreateTransfer", refundRequest.RefundReferenceCode);
                }

                return isTransferSuccess;
            }

            private async Task<bool> CreateTransferAsync(
                decimal Amount,
                string DAccountNo,
                string DAccountBranch,
                string CAccountNo, string CAccountBranch,
                string RefrenceNo, RefundRequest refundRequest)
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
                            <Id>00003</Id>
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


            var transferRecord = new Refund
                {
                    RequestId = RefrenceNo,
                    MsgId = msgId,
                    PmtInfId = pmtInfId,
                    InstrId = instrId,
                    EndToEndId = endToEndId,
                    Amount = Amount,
                    Currency = "ETB", // Currency from request (ETB used here)
                    ShortCode = MerchantCode,
                    FirstName = refundRequest.FirstName,
                    LastName = refundRequest.LastName,
                    OrderId = refundRequest.OrderId,
                    RefundAccountNumber = refundRequest.RefundAccountNumber,
                    RefundFOP = refundRequest.RefundFOP,
                    RefundReferenceCode = refundRequest.RefundReferenceCode,
                    ReferenceNumber = refundRequest.ReferenceNumber,
                    DAccountNo = DAccountNo,
                       DAccountBranch = DAccountBranch,
                        TransferDate = DateTime.UtcNow,  // Current timestamp for when the transfer was initiated
                    CBSResponseStatus = isSuccess ? "Success" : "Failed", // Set response status
                    CBSErrorReason = isSuccess ? null : reason,  // Store error message if the transfer fails
                    CBSRequestTimestamp = DateTime.UtcNow,  // Timestamp when request was made
                    CBSResponseTimestamp = isSuccess ? DateTime.UtcNow : (DateTime?)null,  // Response timestamp if successful
                    CBSIsSuccessful = isSuccess
                };

            if (isSuccess)
            {
                var refundConfirmationRequest = new RefundConfirmationRequest
                {
                    Shortcode= MerchantCode,
                    RefundReferenceCode = refundRequest.RefundReferenceCode,
                    BankRefundReference = pmtInfId, // Ensure a valid bank reference
                    OrderId = refundRequest.OrderId,
                    Amount = refundRequest.Amount.ToString(),
                    Currency = refundRequest.Currency,
                    RefundAccountNumber = refundRequest.RefundAccountNumber,
                    RefundDate = DateTime.UtcNow, // Use current date if needed
                    RefundFOP = refundRequest.RefundFOP,
                    Status = "1", // Success
                    Remark = "Refund successfully processed",
                    AccountHolderName = refundRequest.FirstName+refundRequest.LastName,
                };

                // **Call the refund confirmation API**
                bool refundConfirmed = await ConfirmRefundAsync(refundConfirmationRequest);
                if (!refundConfirmed)
                {
                    await LogErrorToAirlinesErrorAsync("Refund Confirmation", refundRequest.RefundAccountNumber, "Failed", "Refund confirmation API failed", "ConfirmRefund", RefrenceNo);
                }
            }
            else
            {
                await LogErrorToAirlinesErrorAsync("Transfer", DAccountNo, "Failed", reason, "CreateTransfer", RefrenceNo);
                throw new Exception(reason);

            }


            // Save the transfer record to the database
            await _context.refunds.AddAsync(transferRecord);
                await _context.SaveChangesAsync();
                return isSuccess;
            }

            // Log error method
   

        private async Task LogErrorToAirlinesErrorAsync(string action, string accountNo, string status, string reason, string method, string referenceNo)
        {
            var feedback = new
            {
                Code = "SB_DS_003",  // Custom error code
                Label = reason,
                Severity = "ERROR",
                Type = "BUS",
                Source = method,  // Log the method name where the error occurred
                Origin = "AirlinesRefundRepository",  // This is where the error happened
                SpanId = referenceNo,
                Parameters = new List<object>
        {
            new { Code = "0", Value = $"Error in {method} for accountNo: {accountNo}, referenceNo: {referenceNo}" }
        }
            };

            // Serialize the feedback object to JSON or a custom format
            string feedbackJson = JsonSerializer.Serialize(feedback);

            var errorRecord = new AirlinesError
            {
                ReturnCode = "ERROR",
                TicketId = Guid.NewGuid().ToString(),
                TraceId = referenceNo,
                Feedbacks = feedbackJson,  // Store serialized feedback
                RequestDate = DateTime.UtcNow,
                ErrorType = reason
            };

            _context.airlineserror.Add(errorRecord);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> ConfirmRefundAsync(RefundConfirmationRequest refundRequest)
        {
            string apiUrl = "https://ethiopiangatewaytest.azurewebsites.net/Lion/api/V1.0/Lion/ConfirmRefund";

            // Encode username and password for Basic Authentication
            string username = "lionbanktest@ethiopianairlines.com";
            string password = "LI*&%@54778Ba";
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

            var confirmationPayload = new
            {
                shortcode = MerchantCode,
                Amount = refundRequest.Amount,
                currency = refundRequest.Currency,
                OrderId = refundRequest.OrderId,
                RefundReferenceCode = refundRequest.RefundReferenceCode,
                RefundAccountNumber = refundRequest.RefundAccountNumber,
                refundDate = refundRequest.RefundDate.ToString("yyyy-MM-dd"),
                bankRefundReference = refundRequest.BankRefundReference,
                refundFOP = refundRequest.RefundFOP,
                Status = refundRequest.Status,  // 1 for Success, 0 for Failure
                Remark = refundRequest.Remark,
                AccountHolderName = refundRequest.AccountHolderName,
            };

            string jsonPayload = JsonSerializer.Serialize(confirmationPayload);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Add Basic Auth header
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        await LogErrorToAirlinesErrorAsync(
                            "API Error",
                            refundRequest.RefundAccountNumber,
                            "Failed",
                            responseString,
                            "ConfirmRefund",
                            refundRequest.RefundReferenceCode
                        );
        
                    }

                    var jsonResponse = JsonSerializer.Deserialize<RefundConfirmationResponse>(responseString);
                    if (jsonResponse == null)
                    {
                        await LogErrorToAirlinesErrorAsync(
                            "Deserialization Error",
                            refundRequest.RefundAccountNumber,
                            "Failed",
                            "Invalid JSON response",
                            "ConfirmRefund",
                            refundRequest.RefundReferenceCode
                        );
                      
                    }

                    var confirmRefund = new ConfirmRefund
                    {
                        ShortCode = refundRequest.Shortcode ?? "Unknown",
                        Amount = refundRequest.Amount,
                        Currency = refundRequest.Currency ?? "N/A",
                        OrderId = refundRequest.OrderId ?? "N/A",
                        RefundReferenceCode = refundRequest.RefundReferenceCode ?? "N/A",
                        RefundAccountNumber = refundRequest.RefundAccountNumber ?? "N/A",
                        RefundDate = refundRequest.RefundDate.ToString("yyyy-MM-dd") ,
                        BankRefundReference = refundRequest.BankRefundReference ?? "N/A",
                        RefundFOP = refundRequest.RefundFOP ?? "N/A",
                        Status = jsonResponse?.Status == 1 ? "1" : "0",
                        Remark = refundRequest.Remark ?? "N/A",
                        AccountHolderName = refundRequest.AccountHolderName ?? "N/A",
                        ResponseRefundReferenceCode = jsonResponse?.refundReferenceCode ?? "N/A",
                        ResponseBankRefundReference = jsonResponse?.bankRefundReference ?? "N/A",
                        ResponseAmount = jsonResponse?.Amount?.ToString() ?? "0",
                        ResponseStatus = jsonResponse?.Status.ToString() ?? "N/A",
                        ResponseMessage = jsonResponse?.message ?? "N/A",
                        CreatedDate = DateTime.UtcNow,
                    };

                    _context.confirmRefunds.Add(confirmRefund);
                    await _context.SaveChangesAsync();

                    return jsonResponse.Status == 1;
                }
            }
            catch (Exception ex)
            {
                await LogErrorToAirlinesErrorAsync(
                    "Exception Occurred",
                    refundRequest.RefundAccountNumber,
                    "Failed",
                    ex.Message,
                    "ConfirmRefund",
                    refundRequest.RefundReferenceCode
                );

                return false;
            }
        }
        private string GenerateBankRefundReference()
        {
            return $"REF{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
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



        public async Task<bool> IsReferenceNoUniqueAsync(string referenceNo)
        {
            // Check if the ReferenceNo already exists in the database
            var existingRequest = await _context.refunds
                .FirstOrDefaultAsync(b => b.ReferenceNumber == referenceNo);

            return existingRequest == null; // Return true if not found, false otherwise
        }

    }

}

