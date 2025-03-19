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
        public int InvoiceId { get; set; }
        public string InvoiceIdentificationValue { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string CurrencyAlphaCode { get; set; }
        public string CurrencyDesignation { get; set; }
    }

}
