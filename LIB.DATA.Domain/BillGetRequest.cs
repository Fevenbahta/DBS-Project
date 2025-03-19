using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Domain
{
    public class BillGetRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-generate ID

        public int Id { get; set; }

        //request
        public DateTime ResTransactionDate { get; set; }
        public string BillerType { get; set; }
        public string ReqProviderId { get; set; }
        public string UniqueCode { get; set; }
        public string PhoneNumber { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime ReqTransactionDate { get; set; }
        public string CustomerId { get; set; }
        /// //////////response <summary>
        /// //////////response
        /// </summary>
        public string Status { get; set; }
        public string ResponseError { get; set; }
        public string ResProviderId { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceIdentificationValue { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string CurrencyAlphaCode { get; set; }
        public string CurrencyDesignation { get; set; }
    }

}
