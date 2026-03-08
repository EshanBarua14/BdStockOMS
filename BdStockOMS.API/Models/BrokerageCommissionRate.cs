namespace BdStockOMS.API.Models;

public class BrokerageCommissionRate
{
    public int Id                  { get; set; }
    public int BrokerageHouseId    { get; set; }
    public decimal BuyRate         { get; set; }
    public decimal SellRate        { get; set; }
    public bool IsActive           { get; set; } = true;
    public DateTime EffectiveFrom  { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public BrokerageHouse BrokerageHouse { get; set; } = null!;
}
