using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LIB.API.Domain
{
    public class TransferFilterParameters
    {
        [Required]
        public Guid? AccountId { get; set; }

        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        public string? Currency { get; set; }
        public DateTime? ExecutionDateFrom { get; set; } // ✅ Nullable
        public DateTime? ExecutionDateTo { get; set; } // ✅ Nullable
        public string? Range { get; set; }
        public List<string>? Statuses { get; set; }
    }
}
