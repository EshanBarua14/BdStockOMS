namespace BdStockOMS.API.Models;

public enum RMSLevel
{
    Investor = 1,
    Trader   = 2,
    Stock    = 3,
    Sector   = 4,
    Market   = 5,
    Exchange = 6
}

public enum RMSAction
{
    Warn  = 1,
    Block = 2,
    Freeze = 3
}

public class RMSLimit
{
    public int Id                  { get; set; }
    public RMSLevel Level          { get; set; }
    public int? EntityId           { get; set; } // UserId, StockId, SectorId etc
    public string EntityType       { get; set; } = string.Empty;
    public int BrokerageHouseId    { get; set; }
    public decimal MaxOrderValue   { get; set; }
    public decimal MaxDailyValue   { get; set; }
    public decimal MaxExposure     { get; set; }
    public decimal ConcentrationPct { get; set; } // max % of portfolio in one stock
    public RMSAction ActionOnBreach { get; set; } = RMSAction.Block;
    public bool IsActive           { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public BrokerageHouse BrokerageHouse { get; set; } = null!;
}
