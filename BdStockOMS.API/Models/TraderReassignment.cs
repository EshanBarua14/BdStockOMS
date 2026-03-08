namespace BdStockOMS.API.Models;

public class TraderReassignment
{
    public int Id                  { get; set; }
    public int InvestorId          { get; set; }
    public int? OldTraderId        { get; set; }
    public int NewTraderId         { get; set; }
    public int ReassignedByUserId  { get; set; }
    public string? Reason          { get; set; }
    public int BrokerageHouseId    { get; set; }
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Investor           { get; set; } = null!;
    public User? OldTrader         { get; set; }
    public User NewTrader          { get; set; } = null!;
    public User ReassignedBy       { get; set; } = null!;
    public BrokerageHouse BrokerageHouse { get; set; } = null!;
}
