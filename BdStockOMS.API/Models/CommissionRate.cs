namespace BdStockOMS.API.Models;

public class CommissionRate
{
    public int Id                  { get; set; }
    public decimal BuyRate         { get; set; }  // % of trade value
    public decimal SellRate        { get; set; }
    public decimal CDBLRate        { get; set; } = 0.015m; // fixed
    public decimal DSEFeeRate      { get; set; } = 0.05m;  // fixed
    public bool IsActive           { get; set; } = true;
    public DateTime EffectiveFrom  { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}
