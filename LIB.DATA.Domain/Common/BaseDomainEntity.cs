using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain.Common
{
    public abstract class BaseDomainEntity
    {

        public string? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public string? Status { get; set; }
    }
}
