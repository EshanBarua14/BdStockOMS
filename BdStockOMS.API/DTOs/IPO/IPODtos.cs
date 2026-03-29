using System;
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.IPO
{
    public class IPOResponseDto
    {
        public int      Id              { get; set; }
        public int      StockId         { get; set; }
        public string   CompanyName     { get; set; } = string.Empty;
        public string   TradingCode     { get; set; } = string.Empty;
        public decimal  OfferPrice      { get; set; }
        public int      TotalShares     { get; set; }
        public int      SharesRemaining { get; set; }
        public decimal  MinInvestment   { get; set; }
        public decimal  MaxInvestment   { get; set; }
        public DateTime OpenDate        { get; set; }
        public DateTime CloseDate       { get; set; }
        public DateTime? AllocationDate { get; set; }
        public DateTime? ListingDate    { get; set; }
        public string   Status          { get; set; } = string.Empty;
        public string?  Description     { get; set; }
        public DateTime CreatedAt       { get; set; }
    }

    public class CreateIPODto
    {
        [Range(1, int.MaxValue)] public int StockId { get; set; }
        [Required, MaxLength(100)] public string CompanyName { get; set; } = string.Empty;
        [Required, MaxLength(20)]  public string TradingCode { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue)] public decimal OfferPrice    { get; set; }
        [Range(1, int.MaxValue)]       public int     TotalShares   { get; set; }
        [Range(0.01, double.MaxValue)] public decimal MinInvestment { get; set; }
        [Range(0.01, double.MaxValue)] public decimal MaxInvestment { get; set; }
        public DateTime OpenDate  { get; set; }
        public DateTime CloseDate { get; set; }
        public string?  Description { get; set; }
    }

    public class ApplyIPODto
    {
        [Range(1, int.MaxValue)] public int IPOId             { get; set; }
        [Range(1, int.MaxValue)] public int InvestorId        { get; set; }
        [Range(1, int.MaxValue)] public int BrokerageHouseId  { get; set; }
        [Range(1, int.MaxValue)] public int AppliedShares     { get; set; }
    }

    public class IPOApplicationResponseDto
    {
        public int      Id               { get; set; }
        public int      IPOId            { get; set; }
        public string   CompanyName      { get; set; } = string.Empty;
        public int      InvestorId       { get; set; }
        public int      AppliedShares    { get; set; }
        public decimal  AppliedAmount    { get; set; }
        public int      AllocatedShares  { get; set; }
        public decimal  AllocatedAmount  { get; set; }
        public decimal  RefundAmount     { get; set; }
        public string   Status           { get; set; } = string.Empty;
        public string?  RejectionReason  { get; set; }
        public DateTime AppliedAt        { get; set; }
        public DateTime? AllocatedAt     { get; set; }
        public DateTime? RefundedAt      { get; set; }
    }

    public class IPOAllocationResultDto
    {
        public int     IPOId              { get; set; }
        public string  CompanyName        { get; set; } = string.Empty;
        public int     TotalApplications  { get; set; }
        public int     TotalAppliedShares { get; set; }
        public int     TotalShares        { get; set; }
        public bool    IsOversubscribed   { get; set; }
        public decimal SubscriptionRatio  { get; set; }
        public int     AllocatedCount     { get; set; }
        public decimal TotalRefundAmount  { get; set; }
    }
}
