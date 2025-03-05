using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class SBCPErrorFeedback
    {
        public string Code { get; set; }
        public string Label { get; set; }
        public string Severity { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string Origin { get; set; }
        public string SpanId { get; set; }
        public List<SBCPErrorParameter> Parameters { get; set; }
    }

}
