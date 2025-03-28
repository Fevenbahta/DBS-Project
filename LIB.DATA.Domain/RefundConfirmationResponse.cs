using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LIB.API.Domain
{


    public class RefundConfirmationResponse
    {
        public string refundReferenceCode { get; set; }
        public string bankRefundReference { get; set; }
        public string OrderId { get; set; }
        public string Amount { get; set; }
        public int Status { get; set; }
        public string message { get; set; }
    }

    public class RefundConfirmationRequest
    {
        public string Shortcode { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string RefundReferenceCode { get; set; }
        public string RefundAccountNumber { get; set; }
        public DateTime RefundDate { get; set; }
        public string BankRefundReference { get; set; }
        public string RefundFOP { get; set; }
        public string Status { get; set; }  // 1 for Success, 0 for Failure
        public string Remark { get; set; }
        public string AccountHolderName { get; set; }
    }


}
