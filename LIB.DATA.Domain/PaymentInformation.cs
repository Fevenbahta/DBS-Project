using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class PaymentInformation
    {
        [Required]
        [RegularExpression("^(LOCAL_TRANSFER|INTERNAL_TRANSFER|CUSTOM)$")]
        public string? PaymentType { get; set; }
        [RegularExpression("^(AWACH|MPESAWALLET|MPESATRUST|TELEBIRR|ETHSWICH|RTGS)$")]
        public string? PaymentScheme { get; set; }

        public PaymentAccount Account { get; set; }

  
        public BankInformation Bank { get; set; }
    }

}
