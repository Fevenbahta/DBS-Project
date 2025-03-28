using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ECPaymentRecords
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID

        public int Id { get; set; }
        public string? InvoiceId { get; set; }
        public string? ReferenceNo { get; set; }
        public string? CustomerId { get; set; }
        public string? Reason { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Branch { get; set; }
        public string? Currency { get; set; }
        public string? AccountNo { get; set; }
        public string? ProviderId { get; set; }
        public string? Status { get; set; }
        public string? Response { get; set; }
        public string? ResponseId { get; set; }
        
        public string? ResponseError { get; set; }
    }
}
