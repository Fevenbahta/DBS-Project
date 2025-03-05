using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class BankInformation
    {
        [Required]
        public string? Id { get; set; } // Bank ID (e.g., ABAGATWWXXX)

        [Required]
        [RegularExpression("^(BIC|BANKCODE|SORTCODE|OTHER)$")]
        public string? IdType { get; set; } // Bank ID Type (e.g., BIC)
    }

}
