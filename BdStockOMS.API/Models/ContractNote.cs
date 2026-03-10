using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class ContractNote
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string ContractNoteNumber { get; set; } = string.Empty;

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Order? Order { get; set; }

        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual User? Client { get; set; }

        [MaxLength(200)]
        public string TraderName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string BranchName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string InstrumentCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string InstrumentName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Side { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal ExecutedPrice { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CdscFee { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal LevyCharge { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal VatOnCommission { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal NetAmount { get; set; }

        public DateTime TradeDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        public string Status { get; set; } = "Generated";

        public bool IsVoid { get; set; } = false;
        public DateTime? VoidedAt { get; set; }

        [MaxLength(500)]
        public string? VoidReason { get; set; }
    }
}
