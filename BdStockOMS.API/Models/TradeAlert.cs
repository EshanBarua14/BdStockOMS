using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public enum TradeAlertType
{
    OrderValueLimit,
    DailyExposureLimit,
    StockConcentration,
    SectorConcentration,
    CashBalanceInsufficient,
    MarginBreached
}

public enum TradeAlertSeverity
{
    Warning,
    Breach
}

public class TradeAlert
{
    public int Id { get; set; }

    public int InvestorId       { get; set; }
    public int BrokerageHouseId { get; set; }
    public int? OrderId         { get; set; }

    public TradeAlertType     AlertType { get; set; }
    public TradeAlertSeverity Severity  { get; set; }

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public decimal? ThresholdValue { get; set; }
    public decimal? ActualValue    { get; set; }

    public bool IsAcknowledged         { get; set; } = false;
    public DateTime? AcknowledgedAt    { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
}
