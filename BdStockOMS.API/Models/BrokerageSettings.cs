using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class BrokerageSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BrokerageHouseId { get; set; }

        // RMS Settings
        [Column(TypeName = "decimal(18,4)")]
        public decimal MaxSingleOrderValue { get; set; } = 1_000_000m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MaxDailyTurnover { get; set; } = 10_000_000m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MarginRatio { get; set; } = 1.5m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinCashBalance { get; set; } = 0m;

        // Feature Toggles
        public bool IsMarginTradingEnabled { get; set; } = false;
        public bool IsShortSellingEnabled { get; set; } = false;
        public bool IsSmsAlertEnabled { get; set; } = true;
        public bool IsEmailAlertEnabled { get; set; } = true;
        public bool IsAutoSettlementEnabled { get; set; } = true;
        public bool IsTwoFactorRequired { get; set; } = false;

        // Trading Hours (stored as minutes from midnight)
        public int TradingStartMinutes { get; set; } = 570;  // 09:30
        public int TradingEndMinutes { get; set; } = 870;    // 14:30

        [MaxLength(10)]
        public string TimeZone { get; set; } = "BST";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(BrokerageHouseId))]
        public BrokerageHouse BrokerageHouse { get; set; } = null!;
    }
}
