using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string OrderId { get; set; }
        public string RefundAccountNumber { get; set; }
        public string RefundFOP { get; set; }
        public string RefundReferenceCode { get; set; }
        public string ReferenceNumber { get; set; }
    }

}
