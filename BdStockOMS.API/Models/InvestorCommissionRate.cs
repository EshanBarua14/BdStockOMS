namespace BdStockOMS.API.Models;

public class InvestorCommissionRate
{
    public int Id                  { get; set; }
    public int InvestorId          { get; set; }
    public int BrokerageHouseId    { get; set; }
    public decimal BuyRate         { get; set; }
    public decimal SellRate        { get; set; }
    public bool IsApproved         { get; set; } = false;
    public int? ApprovedByUserId   { get; set; }
    public DateTime EffectiveFrom  { get; set; }
    public DateTime? EffectiveTo   { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Investor           { get; set; } = null!;
    public BrokerageHouse BrokerageHouse { get; set; } = null!;
    public User? ApprovedBy        { get; set; }
}
