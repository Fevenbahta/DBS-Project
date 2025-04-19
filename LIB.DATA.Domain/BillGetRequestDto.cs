using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class BillGetRequestDto
    {

        public string BillerType { get; set; }
        public string ProviderId { get; set; }
        public string UniqueCode { get; set; }
        public string PhoneNumber { get; set; }
        public string ReferenceNo { get; set; }
      
        public string AccountNo { get; set; }
    }

}
