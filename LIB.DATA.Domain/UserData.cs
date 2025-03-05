using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class UserData
    {

        public string BRANCH { get; set; }
        public string BRANCH_NAME { get; set; }

            public string USER_NAME { get; set; }
      
        [Key]
        public string FULL_NAME { get; set; }
        public string ROLE { get; set; }
        public string? CREATED_DATE { get; set; }
        public string? UPDATED_DATE { get; set; }
        public string STATUS { get; set; }
    }
}
