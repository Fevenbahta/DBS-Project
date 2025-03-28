using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class BillError
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID
        public string Id { get; set; }
        public string ReturnCode { get; set; }
        public string TicketId { get; set; }
        public string TraceId { get; set; }
        public string Feedbacks { get; set; }
        public DateTime RequestDate { get; set; }
        public string ErrorType { get; set; }


    }


}
