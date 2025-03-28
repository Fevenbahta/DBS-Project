using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class CreateBody
    {
        public decimal Amount { get; set; }
        public string DAccountNo { get; set; }
        public string OrderId { get; set; }
        public string ReferenceNo { get; set; }
        public string TraceNumber { get; set; }
  
    }

}
