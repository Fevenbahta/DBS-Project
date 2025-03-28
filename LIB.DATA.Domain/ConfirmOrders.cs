using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ConfirmOrders
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID
        public int Id { get; set; }

        // Request Fields
        public string OrderId { get; set; }
        public string ShortCode { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
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
        public string Message { get; set; }
        // Timestamps
        public DateTime RequestDate { get; set; } 
        public DateTime? ResponseDate{ get; set; }
        public string ReferenceId { get; set; }  // Add ReferenceId field
    }

}
