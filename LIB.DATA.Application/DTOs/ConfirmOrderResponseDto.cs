using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs
{
    public class ConfirmOrderResponseDto
    {
        public string OrderId { get; set; }
        public string ShortCode { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; }
        public int Status { get; set; } // 1 = Success, 0 = Error
        public string Remark { get; set; }
        public string TraceNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string PaidAccountNumber { get; set; }
        public string PayerCustomerName { get; set; }

        // Response Fields
        public DateTime? ExpireDate { get; set; }
        public int StatusCodeResponse { get; set; }
        public string StatusCodeResponseDescription { get; set; }
        public string CustomerName { get; set; }
        public long MerchantId { get; set; }
        public string MerchantCode { get; set; }
        public string MerchantName { get; set; }

        // Optional Message Field for Additional Response Info
        public string Message { get; set; }
    }

}
