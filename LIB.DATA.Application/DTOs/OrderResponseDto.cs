using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Application.DTOs
{
    public class OrderResponseDto
    {
        public double Amount { get; set; }
        public string TraceNumber { get; set; }
        public int StatusCodeResponse { get; set; }
        public string StatusCodeResponseDescription { get; set; }
        public string OrderId { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string CustomerName { get; set; }
        public long MerchantId { get; set; }
        public string MerchantCode { get; set; }
        public string MerchantName { get; set; }
        public string Message { get; set; }
        public int Status { get; set; }
    }

}
