using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ConfirmRefund
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID
        public int Id { get; set; } // Primary Key

        // Request Data
        public string ShortCode { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string OrderId { get; set; }
        public string RefundReferenceCode { get; set; }
        public string RefundAccountNumber { get; set; }
        public string RefundDate { get; set; }
        public string BankRefundReference { get; set; }
        public string RefundFOP { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string AccountHolderName { get; set; }

        // Response Data
        public string ResponseRefundReferenceCode { get; set; }
        public string ResponseBankRefundReference { get; set; }
        public string ResponseAmount { get; set; }
        public string ResponseStatus { get; set; }
        public string ResponseMessage { get; set; }

        // Timestamp for the record creation
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

}
