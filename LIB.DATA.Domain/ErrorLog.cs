using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class ErrorLog
    {
        [Key]
        public string ticketId { get; set; }
        public string traceId { get; set; }
        public string returnCode { get; set; }
        public DateTime EventDate { get; set; }
        public string feedbacks { get; set; }
        public string? TransactionId { get; set; }
        public string? TransactionType { get; set; }
    }
}
