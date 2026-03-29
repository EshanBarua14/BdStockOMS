using System;
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models
{
    public enum IPOStatus { Upcoming, Open, Closed, Allocated, Refunded, Listed }

    public enum IPOApplicationStatus { Pending, Accepted, Rejected, Allocated, Refunded }

    public class IPO
    {
        public int Id                     { get; set; }
        public int StockId                { get; set; }
        [MaxLength(100)] public string CompanyName { get; set; } = string.Empty;
        [MaxLength(20)]  public string TradingCode { get; set; } = string.Empty;
        public decimal   OfferPrice       { get; set; }
        public int       TotalShares      { get; set; }
        public int       SharesRemaining  { get; set; }
        public decimal   MinInvestment    { get; set; }
        public decimal   MaxInvestment    { get; set; }
        public DateTime  OpenDate         { get; set; }
        public DateTime  CloseDate        { get; set; }
        public DateTime? AllocationDate   { get; set; }
        public DateTime? ListingDate      { get; set; }
        public IPOStatus Status           { get; set; } = IPOStatus.Upcoming;
        public string?   Description      { get; set; }
        public DateTime  CreatedAt        { get; set; } = DateTime.UtcNow;

        public virtual Stock Stock { get; set; } = null!;
    }

    public class IPOApplication
    {
        public int Id                     { get; set; }
        public int IPOId                  { get; set; }
        public int InvestorId             { get; set; }
        public int BrokerageHouseId       { get; set; }
        public int    AppliedShares       { get; set; }
        public decimal AppliedAmount      { get; set; }
        public int    AllocatedShares     { get; set; }
        public decimal AllocatedAmount    { get; set; }
        public decimal RefundAmount       { get; set; }
        public IPOApplicationStatus Status { get; set; } = IPOApplicationStatus.Pending;
        public string? RejectionReason    { get; set; }
        public DateTime  AppliedAt        { get; set; } = DateTime.UtcNow;
        public DateTime? AllocatedAt      { get; set; }
        public DateTime? RefundedAt       { get; set; }

        public virtual IPO IPO { get; set; } = null!;
    }
}
