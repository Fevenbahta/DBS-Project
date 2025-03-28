using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class HellocashTransactionRequest
    {
        public string AppCode { get; set; }
        public string AppPassphrase { get; set; }
        public decimal Amount { get; set; }
        public string DebitAccount { get; set; }
        public string AccName { get; set; }
        public string DestAccount { get; set; }
        public string DestAccName { get; set; }
        public string DestBankSwiftCode { get; set; }
        public string OriginatorRef { get; set; }
        public string Remarks { get; set; }
    }

}
