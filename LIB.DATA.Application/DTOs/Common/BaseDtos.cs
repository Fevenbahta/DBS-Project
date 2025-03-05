using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs.Common
{
    public abstract class BaseDtos
    {
        public string UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string Status { get; set; }


    }
}
