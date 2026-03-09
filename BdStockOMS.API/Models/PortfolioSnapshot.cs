using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class PortfolioSnapshot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int BrokerageHouseId { get; set; }

        [Required]
        public DateTime SnapshotDate { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalInvested { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CurrentValue { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnrealizedPnL { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal RealizedPnL { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalPnL { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal RoiPercent { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CashBalance { get; set; }

        public int TotalHoldings { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(BrokerageHouseId))]
        public BrokerageHouse BrokerageHouse { get; set; } = null!;
    }
}
