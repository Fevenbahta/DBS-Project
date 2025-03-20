using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ECPaymentRequestDTO
    {
        public string InvoiceId { get; set; }
        public string ReferenceNo { get; set; }
        public string CustomerCode { get; set; }
         public string Reason { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Branch { get; set; }
        public string Currency { get; set; }
        public string AccountNo{ get; set; }
        public string ProviderId { get; set; }


    }

}
