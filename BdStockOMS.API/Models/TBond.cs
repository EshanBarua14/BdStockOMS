using System;
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models
{
    public enum TBondStatus { Active, Matured, Cancelled }

    public enum CouponFrequency { Monthly, Quarterly, SemiAnnual, Annual }

    public enum TBondOrderStatus { Pending, Executed, Cancelled, Settled }

    public class TBond
    {
        public int    Id               { get; set; }
        [MaxLength(50)]  public string ISIN          { get; set; } = string.Empty;
        [MaxLength(100)] public string Name          { get; set; } = string.Empty;
        public decimal FaceValue        { get; set; }
        public decimal CouponRate       { get; set; }  // annual % e.g. 0.08 = 8%
        public CouponFrequency CouponFrequency { get; set; } = CouponFrequency.SemiAnnual;
        public DateTime IssueDate       { get; set; }
        public DateTime MaturityDate    { get; set; }
        public decimal TotalIssueSize   { get; set; }
        public decimal OutstandingSize  { get; set; }
        public TBondStatus Status       { get; set; } = TBondStatus.Active;
        public string? Description      { get; set; }
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    }

    public class TBondOrder
    {
        public int    Id                { get; set; }
        public int    TBondId           { get; set; }
        public int    InvestorId        { get; set; }
        public int    BrokerageHouseId  { get; set; }
        public string Side              { get; set; } = "Buy"; // Buy or Sell
        public decimal Quantity         { get; set; }  // face value units
        public decimal Price            { get; set; }  // price per 100 face value
        public decimal TotalAmount      { get; set; }
        public TBondOrderStatus Status  { get; set; } = TBondOrderStatus.Pending;
        public DateTime OrderedAt       { get; set; } = DateTime.UtcNow;
        public DateTime? ExecutedAt     { get; set; }
        public DateTime? SettledAt      { get; set; }
        public string? Notes            { get; set; }

        public virtual TBond TBond      { get; set; } = null!;
    }

    public class CouponPayment
    {
        public int    Id                { get; set; }
        public int    TBondId           { get; set; }
        public int    InvestorId        { get; set; }
        public int    BrokerageHouseId  { get; set; }
        public decimal HoldingFaceValue { get; set; }
        public decimal CouponRate       { get; set; }
        public decimal CouponAmount     { get; set; }
        public DateTime PeriodStart     { get; set; }
        public DateTime PeriodEnd       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public bool   IsPaid            { get; set; } = false;
        public DateTime? PaidAt         { get; set; }
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;

        public virtual TBond TBond      { get; set; } = null!;
    }

    public class TBondHolding
    {
        public int    Id                { get; set; }
        public int    TBondId           { get; set; }
        public int    InvestorId        { get; set; }
        public int    BrokerageHouseId  { get; set; }
        public decimal FaceValueHeld    { get; set; }
        public decimal AverageCost      { get; set; }
        public DateTime LastUpdatedAt   { get; set; } = DateTime.UtcNow;

        public virtual TBond TBond      { get; set; } = null!;
    }
}
