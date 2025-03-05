using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class Amount
    {
        [Required]
        public string? Currency { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than zero.")]
        public decimal Value { get; set; }
    }


}
