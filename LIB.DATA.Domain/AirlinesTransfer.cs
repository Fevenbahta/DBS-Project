using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class AirlinesTransfer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID





        public int Id { get; set; } // Primary Key

        public string OrderId { get; set; }
        public string ReferenceNo { get; set; }
        public string TraceNumber { get; set; }
        public string MerchantCode { get; set; }



        public string RequestId { get; set; }
        public string MsgId { get; set; }
        public string PmtInfId { get; set; }
        public string InstrId { get; set; }
        public string EndToEndId { get; set; }

        public decimal Amount { get; set; }

        public string DAccountNo { get; set; }  // Debtor Account Number
        public string CAccountNo { get; set; }  // Creditor Account Number
        public string DAccountBranch { get; set; } // Creditor Account Branch
        public string CAccountName { get; set; } // Optional field to store account name

        public DateTime TransferDate { get; set; }  // The date of transfer
        public string ResponseStatus { get; set; }  // Status of the response (Success/Failure)
        public string? ErrorReason { get; set; }  // If failed, why it failed
        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;  // Timestamp when request is made
        public DateTime? ResponseTimestamp { get; set; }  // Timestamp when the response is received
        public bool IsSuccessful { get; set; }
    }
}