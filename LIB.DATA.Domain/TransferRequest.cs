using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LIB.API.Domain
{
    public class TransferRequest
    {
        [Required]
        public Guid? AccountId { get; set; }

        public Guid? ReservationId { get; set; }
       
        [Required]
        public Guid? ReferenceId { get; set; }

        [Required]
        public Amount? Amount { get; set; }

        public DateTime? RequestedExecutionDate { get; set; }

   
        public string Subject { get; set; }

        public Payee Payee { get; set; }

        [Required]
        public PaymentInformation? PaymentInformation { get; set; }

        public List<CustomField> CustomFields { get; set; }
    }
}
