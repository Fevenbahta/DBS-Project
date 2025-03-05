using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID
        public int Id { get; set; }
        public Guid? accountId { get; set; }
        public Guid? reservationId { get; set; }
        public Guid? referenceId { get; set; }
        public decimal amount { get; set; }
        public DateTime? requestedExecutionDate { get; set; }
        public string paymentType { get; set; }
        public string paymentScheme { get; set; }
        public string ReciverAccountId { get; set; }
        public string ReciverAccountIdType { get; set; }
        public string bankId { get; set; }
        public string bankIdType { get; set; }
        public string bankName { get; set; }
        public string status { get; set; }
        public string cbsStatusMessage { get; set; }
        public string bankStatusMessage { get; set; }
        public string conversationId { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolderName { get; set; }
    }

}
