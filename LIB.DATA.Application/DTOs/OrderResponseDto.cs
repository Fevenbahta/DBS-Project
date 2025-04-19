using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs
{
    public class OrderResponseDto
    {
        public decimal Amount { get; set; } = 0;  // Default to 0 if null
        public string OrderId { get; set; } = "";
        public string TraceNumber { get; set; } = "";  // Default to empty string if null
        public int StatusCodeResponse { get; set; } = 0;  // Default to 0 if null
        public string StatusCodeResponseDescription { get; set; } = "";  // Default to empty string if null
        public DateTime? ExpireDate { get; set; } 
        public string CustomerName { get; set; } = "";  // Default to empty string if null
        public int MerchantId { get; set; } = 0;  // Default to 0 if null
        public string MerchantCode { get; set; } = "";  // Default to empty string if null
        public string MerchantName { get; set; } = "";  // Default to empty string if null
        public string Message { get; set; } = "";  // Default to empty string if null
        public string UtilityName { get; set; } = "";  // Default to empty string if null
        public string LionTransactionNo { get; set; } = "";  // Default to empty string if null
        public string BusinessErrorCode { get; set; } = "";  // Default to empty string if null
        public int StatusCode { get; set; } = 0;  // Default to 0 if null
        public int Status { get; set; } = 0;  // Default to empty string if null
        public string MessageList { get; set; } = "";  // Default to empty string if null
        public string Errors { get; set; } = "";  // Default to empty string if null

    }

}
