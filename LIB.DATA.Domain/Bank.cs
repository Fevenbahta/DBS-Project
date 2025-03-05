using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
  using System.ComponentModel.DataAnnotations;
namespace LIB.API.Domain
{
   public class Bank
        {
            [Required]
            public string? Name { get; set; }

       
            public string AddressLine1 { get; set; }

            public string AddressLine2 { get; set; } // Optional, if there's no second address line

             public string ZipCode { get; set; }

             public string City { get; set; }

              public string CountryCode { get; set; }

            public string BranchCode { get; set; } // Optional, if available
        }
    }



