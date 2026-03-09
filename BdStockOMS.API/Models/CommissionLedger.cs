using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public enum CommissionLedgerType
{
    BrokerCommission,
    CDBLCharge,
    ExchangeFee,
    TotalCharge
}

public class CommissionLedger
{
    public int Id { get; set; }

    public int TradeId          { get; set; }
    public int OrderId          { get; set; }
    public int InvestorId       { get; set; }
    public int BrokerageHouseId { get; set; }

    [MaxLength(10)]
    public string Exchange  { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Side      { get; set; } = string.Empty; // BUY or SELL

    public decimal TradeValue       { get; set; }
    public decimal BrokerCommission { get; set; }
    public decimal CDBLCharge       { get; set; }
    public decimal ExchangeFee      { get; set; }
    public decimal TotalCharges     { get; set; }
    public decimal NetAmount        { get; set; }
    public decimal CommissionRate   { get; set; }

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("TradeId")]
    public virtual Trade Trade { get; set; } = null!;

    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
