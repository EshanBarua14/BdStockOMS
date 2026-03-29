namespace BdStockOMS.API.Models;

public enum RMSLevelV2
{
    Client    = 1,
    User      = 2,
    BOGroup   = 3,
    Basket    = 4,
    Branch    = 5,
    Broker    = 6,
}

public enum RMSLimitType
{
    DayBuyValue       = 1,
    DaySellValue      = 2,
    DayNetValue       = 3,
    MaxOrderValue     = 4,
    MaxExposure       = 5,
    ConcentrationPct  = 6,
    MarginUtilization = 7,
    EDRThreshold      = 8,
}

public class RMSLimitV2
{
    public int Id                  { get; set; }
    public RMSLevelV2 Level        { get; set; }
    public RMSLimitType LimitType  { get; set; }
    public int? EntityId           { get; set; }
    public string EntityType       { get; set; } = string.Empty;
    public int BrokerageHouseId    { get; set; }
    public decimal LimitValue      { get; set; }
    public decimal WarnAt          { get; set; } = 80m;
    public RMSAction ActionOnBreach { get; set; } = RMSAction.Block;
    public bool IsActive           { get; set; } = true;
    public int Priority            { get; set; } = 100;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;
    public string? Notes           { get; set; }
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}

public class EDRSnapshot
{
    public int Id                  { get; set; }
    public int InvestorId          { get; set; }
    public int BrokerageHouseId    { get; set; }
    public decimal TotalEquity     { get; set; }
    public decimal TotalDebt       { get; set; }
    public decimal EDRRatio        { get; set; }
    public decimal MarginUsed      { get; set; }
    public decimal MarginLimit     { get; set; }
    public decimal MarginUtilPct   { get; set; }
    public DateTime CalculatedAt   { get; set; } = DateTime.UtcNow;
    public virtual User Investor   { get; set; } = null!;
}
