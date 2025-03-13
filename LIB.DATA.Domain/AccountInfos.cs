using LIB.API.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class AccountInfos 
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
     

        public string CUSTOMER_ID { get; set; }
        public string FULL_NAME { get; set; }
        public string ACCOUNTNUMBER { get; set; }
    
        public string BRANCH { get; set; }
        public string TELEPHONENUMBER { get; set; }


    }
}
