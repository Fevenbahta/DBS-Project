using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ECPaymentRequestDTO
    {
        public string BillerType { get; set; }
        public string InvoiceId { get; set; }
        public string ReferenceNo { get; set; }
        public string CustomerId { get; set; }
         public string Reason { get; set; }
        public decimal PaymentAmount { get; set; }

        public string Branch { get; set; }
        public string AccountNo{ get; set; }
        public string ProviderId { get; set; }
        public string CustomerCode { get; set; }


    }

}
