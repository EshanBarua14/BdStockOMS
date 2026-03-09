using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public enum SettlementBatchStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class SettlementBatch
{
    public int Id { get; set; }

    public int BrokerageHouseId { get; set; }

    [MaxLength(10)]
    public string Exchange { get; set; } = string.Empty;

    public DateTime SettlementDate  { get; set; }  // T+2 date
    public DateTime TradeDate       { get; set; }  // Original trade date

    public SettlementBatchStatus Status { get; set; } = SettlementBatchStatus.Pending;

    public int     TotalTrades      { get; set; }
    public decimal TotalBuyValue    { get; set; }
    public decimal TotalSellValue   { get; set; }
    public decimal NetObligations   { get; set; }  // TotalBuyValue - TotalSellValue

    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;

    public virtual ICollection<SettlementItem> Items { get; set; } = new List<SettlementItem>();
}
