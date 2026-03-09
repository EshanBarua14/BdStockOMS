using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public enum SettlementItemStatus
{
    Pending,
    Settled,
    Failed
}

public class SettlementItem
{
    public int Id { get; set; }

    public int SettlementBatchId { get; set; }
    public int TradeId           { get; set; }
    public int OrderId           { get; set; }
    public int InvestorId        { get; set; }
    public int BrokerageHouseId  { get; set; }

    [MaxLength(10)]
    public string Side { get; set; } = string.Empty; // BUY or SELL

    public int     Quantity     { get; set; }
    public decimal Price        { get; set; }
    public decimal TradeValue   { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal NetAmount    { get; set; }

    public SettlementType       SettlementType { get; set; }
    public SettlementItemStatus Status         { get; set; } = SettlementItemStatus.Pending;

    public DateTime  TradeDate      { get; set; }
    public DateTime  SettlementDate { get; set; }
    public DateTime? SettledAt      { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("SettlementBatchId")]
    public virtual SettlementBatch Batch { get; set; } = null!;

    [ForeignKey("TradeId")]
    public virtual Trade Trade { get; set; } = null!;

    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
