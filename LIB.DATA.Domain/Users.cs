using LIB.API.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class Users : BaseDomainEntity
    {
        [Key]
        public int Id { get; set; }
        public string Branch { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
 
        public string UserName { get; set; }
      
        public string Password { get; set; }
        public string BranchCode { get; set; }
    }
}
