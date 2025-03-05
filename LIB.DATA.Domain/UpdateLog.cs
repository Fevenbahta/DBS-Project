using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class UpdateLog
    {
        [Key]
        public int LogId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Operation { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string UpdatedFields { get; set; }
    }
}
