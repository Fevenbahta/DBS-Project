using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{

    public class Payee
    {
        public Contact Contact { get; set; }
        public Bank Bank { get; set; }
    }
}
