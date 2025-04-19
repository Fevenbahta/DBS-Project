using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace LIB.API.Application.DTOs
{

    public class OrderRequestDto
    {
  
        public string OrderId { get; set; }
        public string ReferenceId { get; set; }  // Add ReferenceId field
        public string BillerType { get; set; }
        public string PhoneNumber { get; set; }
        public string AccountNo { get; set; }
    }

}
