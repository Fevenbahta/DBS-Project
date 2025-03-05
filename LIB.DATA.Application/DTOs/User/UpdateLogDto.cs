using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs.User
{
    public class UpdateLogDto
    {
        public int LogId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Operation { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string UpdatedFields { get; set; }
    }
}
