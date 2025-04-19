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
        public string AccountNo { get; set; }
        /// //////////response <summary>
        /// //////////response
        /// </summary>
        public string Status { get; set; }
        public string ResponseError { get; set; }
        public List<string> ResProviderId { get; set; }

        // Changing from int to List<int> for InvoiceId
        public List<string> InvoiceId { get; set; }

        // Changing from string to List<string> for InvoiceIdentificationValue
        public List<string> InvoiceIdentificationValue { get; set; }

        // Changing from decimal to List<decimal> for InvoiceAmount
        public List<decimal> InvoiceAmount { get; set; }

        // Changing from string to List<string> for CurrencyAlphaCode
        public List<string> CurrencyAlphaCode { get; set; }

        // Changing from string to List<string> for CurrencyDesignation
        public List<string> CurrencyDesignation { get; set; }

        // Changing from string? to List<string?> for CustomerName
        public List<string?> CustomerName { get; set; }

        // Changing from string? to List<string?> for ProviderName
        public List<string?> ProviderName { get; set; }
    }

}
