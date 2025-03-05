using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class SBCPErrorResponseBody
    {
        public string ReturnCode { get; set; }
        public string TicketId { get; set; }
        public string TraceId { get; set; }
        public List<SBCPErrorFeedback> Feedbacks { get; set; }
    }

}
