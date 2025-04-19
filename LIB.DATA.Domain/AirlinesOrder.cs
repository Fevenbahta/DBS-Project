using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{

    public class AirlinesOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID
        public int Id { get; set; }
        public string? BillerType { get; set; }
        public string? PhoneNumber { get; set; }
   
        public string? AccountNo { get; set; }


        public string OrderId { get; set; }

        public string ShortCode { get; set; }

        public decimal Amount { get; set; }

        public string TraceNumber { get; set; }

        public int StatusCodeResponse { get; set; }

        public string StatusCodeResponseDescription { get; set; }

        public DateTime? ExpireDate { get; set; }
  
        
        public string CustomerName { get; set; }

        
        public long MerchantId { get; set; }

        public string MerchantCode { get; set; }

        public string MerchantName { get; set; }

        public string Message { get; set; }
              public int Status { get; set; }  
        public string BusinessErrorCode { get; set; }
        public int StatusCode { get; set; }
        public string MessageList { get; set; }
        public string LionTransactionNo { get; set; }

        public string Errors { get; set; }
        public string UtilityName { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string ReferenceId { get; set; }  // Add ReferenceId field
    }

}
