using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class PaymentAccount
    {
       
            [Required]
            public string? Id { get; set; }

            [Required]
            [RegularExpression("^(IBAN|BAN|INTERNAL|BBAN|OTHER|PHONE|ALIAS|EMAIL)$")]
            public string? IdType { get; set; }
        }


    }
