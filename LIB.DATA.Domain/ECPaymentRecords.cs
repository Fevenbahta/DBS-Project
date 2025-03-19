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

        public int id { get; set; }
        public string InvoiceId { get; set; }
        public string ReferenceNo { get; set; }
        public string UniqueCode { get; set; }
        public string Reason { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Branch { get; set; }
        public string Currency { get; set; }
        public string Account { get; set; }
        public string BillerId { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
    }
}
