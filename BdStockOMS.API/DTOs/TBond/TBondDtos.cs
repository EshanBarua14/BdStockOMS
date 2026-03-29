using System;
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.TBond
{
    public class TBondResponseDto
    {
        public int      Id              { get; set; }
        public string   ISIN            { get; set; } = string.Empty;
        public string   Name            { get; set; } = string.Empty;
        public decimal  FaceValue       { get; set; }
        public decimal  CouponRate      { get; set; }
        public string   CouponFrequency { get; set; } = string.Empty;
        public DateTime IssueDate       { get; set; }
        public DateTime MaturityDate    { get; set; }
        public decimal  TotalIssueSize  { get; set; }
        public decimal  OutstandingSize { get; set; }
        public string   Status          { get; set; } = string.Empty;
        public string?  Description     { get; set; }
        public int      DaysToMaturity  { get; set; }
        public DateTime CreatedAt       { get; set; }
    }

    public class CreateTBondDto
    {
        [Required, MaxLength(50)]  public string ISIN       { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Name       { get; set; } = string.Empty;
        [Range(0.01, double.MaxValue)] public decimal FaceValue      { get; set; }
        [Range(0.0001, 1.0)]           public decimal CouponRate     { get; set; }
        public string CouponFrequency  { get; set; } = "SemiAnnual";
        public DateTime IssueDate      { get; set; }
        public DateTime MaturityDate   { get; set; }
        [Range(0.01, double.MaxValue)] public decimal TotalIssueSize { get; set; }
        public string?  Description    { get; set; }
    }

    public class PlaceTBondOrderDto
    {
        [Range(1, int.MaxValue)] public int TBondId          { get; set; }
        [Range(1, int.MaxValue)] public int InvestorId       { get; set; }
        [Range(1, int.MaxValue)] public int BrokerageHouseId { get; set; }
        [Required] public string Side     { get; set; } = "Buy";
        [Range(0.01, double.MaxValue)] public decimal Quantity { get; set; }
        [Range(0.01, double.MaxValue)] public decimal Price    { get; set; }
        public string? Notes { get; set; }
    }

    public class TBondOrderResponseDto
    {
        public int      Id              { get; set; }
        public int      TBondId         { get; set; }
        public string   BondName        { get; set; } = string.Empty;
        public string   ISIN            { get; set; } = string.Empty;
        public int      InvestorId      { get; set; }
        public string   Side            { get; set; } = string.Empty;
        public decimal  Quantity        { get; set; }
        public decimal  Price           { get; set; }
        public decimal  TotalAmount     { get; set; }
        public string   Status          { get; set; } = string.Empty;
        public DateTime OrderedAt       { get; set; }
        public DateTime? ExecutedAt     { get; set; }
        public DateTime? SettledAt      { get; set; }
        public string?  Notes           { get; set; }
    }

    public class CouponPaymentResponseDto
    {
        public int      Id              { get; set; }
        public int      TBondId         { get; set; }
        public string   BondName        { get; set; } = string.Empty;
        public int      InvestorId      { get; set; }
        public decimal  HoldingFaceValue { get; set; }
        public decimal  CouponRate      { get; set; }
        public decimal  CouponAmount    { get; set; }
        public DateTime PeriodStart     { get; set; }
        public DateTime PeriodEnd       { get; set; }
        public DateTime PaymentDate     { get; set; }
        public bool     IsPaid          { get; set; }
        public DateTime? PaidAt         { get; set; }
    }

    public class TBondHoldingResponseDto
    {
        public int      Id              { get; set; }
        public int      TBondId         { get; set; }
        public string   BondName        { get; set; } = string.Empty;
        public string   ISIN            { get; set; } = string.Empty;
        public int      InvestorId      { get; set; }
        public decimal  FaceValueHeld   { get; set; }
        public decimal  AverageCost     { get; set; }
        public decimal  CurrentValue    { get; set; }
        public DateTime LastUpdatedAt   { get; set; }
    }

    public class MaturityProcessResultDto
    {
        public int      TBondId         { get; set; }
        public string   BondName        { get; set; } = string.Empty;
        public int      HoldingsSettled { get; set; }
        public decimal  TotalPaidOut    { get; set; }
    }
}
