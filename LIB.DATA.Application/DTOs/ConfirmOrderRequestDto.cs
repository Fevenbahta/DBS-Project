using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs
{

public class ConfirmOrderRequestDTO
    {
       
        public string OrderId { get; set; }

       
        public string ShortCode { get; set; }

       
        public double Amount { get; set; }

       
        public string Currency { get; set; }

       
        public int Status { get; set; } // 1 = Success, 0 = Error

        public string Remark { get; set; }

       
        public string TraceNumber { get; set; }

       
        public string ReferenceNumber { get; set; }

        public string ReferenceId { get; set; }  // Add ReferenceId field


        public string PaidAccountNumber { get; set; }

       
        public string PayerCustomerName { get; set; }





    }

}
