using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class BillGetResponseDto
    {
        public string Status { get; set; }
        public string ProviderId { get; set; }
        public string CustomerCode { get; set; }
        public string InvoiceId { get; set; }
        public string InvoiceIdentificationValue { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string CurrencyAlphaCode { get; set; }
        public string CurrencyDesignation { get; set; }
        public string CustomerName { get; set; }
        public string ProviderName { get; set; }
        public string Deadline { get; set; }
        public decimal Amount { get; set; }


    }

}
