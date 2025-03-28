using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class Refund
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID

        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ShortCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string OrderId { get; set; }
        public string RefundAccountNumber { get; set; }
        public string RefundFOP { get; set; }
        public string RefundReferenceCode { get; set; }
        public string ReferenceNumber { get; set; }





        public string RequestId { get; set; }
        public string MsgId { get; set; }
        public string PmtInfId { get; set; }
        public string InstrId { get; set; }
        public string EndToEndId { get; set; }

        public string DAccountNo { get; set; }  // Debtor Account Number
          public string DAccountBranch { get; set; } // Creditor Account Branch
      
        public DateTime TransferDate { get; set; }  // The date of transfer
        public string? CBSResponseStatus { get; set; }  // Status of the response (Success/Failure)
        public string? CBSErrorReason { get; set; }  // If failed, why it failed
        public DateTime CBSRequestTimestamp { get; set; } = DateTime.Now;  // Timestamp when request is made
        public DateTime? CBSResponseTimestamp { get; set; }  // Timestamp when the response is received
        public bool CBSIsSuccessful { get; set; }

     

    }

}
