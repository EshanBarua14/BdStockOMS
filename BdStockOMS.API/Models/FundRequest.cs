namespace BdStockOMS.API.Models;

public enum FundRequestStatus
{
    Pending,
    ApprovedByTrader,
    ApprovedByCCD,
    Rejected,
    Completed
}

public enum PaymentMethod
{
    Cash,
    Cheque,
    BEFTN,
    RTGS,
    bKash,
    Nagad,
    Rocket,
    SellShares
}

public class FundRequest
{
    public int Id                      { get; set; }
    public int InvestorId              { get; set; }
    public int? TraderId               { get; set; }
    public int? CCDUserId              { get; set; }
    public int BrokerageHouseId        { get; set; }
    public decimal Amount              { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNumber     { get; set; }
    public string? Notes               { get; set; }
    public FundRequestStatus Status    { get; set; } = FundRequestStatus.Pending;
    public string? RejectionReason     { get; set; }
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt        { get; set; }
    public DateTime? CompletedAt       { get; set; }

    // Navigation
    public User Investor               { get; set; } = null!;
    public User? Trader                { get; set; }
    public User? CCDUser               { get; set; }
    public BrokerageHouse BrokerageHouse { get; set; } = null!;
}
