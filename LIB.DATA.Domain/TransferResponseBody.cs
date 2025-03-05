using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class TransferResponseBody
    {
        public Guid? Id { get; set; }
        public string Status { get; set; }
        public Guid AccountId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? ReferenceId { get; set; }
        public Amount Amount { get; set; }
        public DateTime ExecutionDate { get; set; }
        public string Subject { get; set; }
        public Payee Payee { get; set; }
        public PaymentInformation PaymentInformation { get; set; }
        public List<CustomField> CustomFields { get; set; }
    }

}
