using System;
using System.Collections.Generic;

namespace BdStockOMS.API.Models
{
    public enum ComplianceAlertType
    {
        LargeTradeAlert,
        AMLStructuring,
        AMLSmurfing,
        WashTrade,
        Layering,
        FrontRunning,
        UnusualFrequency,
        SuspiciousPattern
    }

    public enum ComplianceSeverity { Low, Medium, High, Critical }

    public enum ComplianceStatus { Open, UnderReview, Resolved, Escalated, FalsePositive }

    public class ComplianceReport
    {
        public Guid   Id                { get; set; } = Guid.NewGuid();
        public int    BrokerageHouseId  { get; set; }
        public ComplianceAlertType AlertType { get; set; }
        public ComplianceSeverity  Severity  { get; set; }
        public ComplianceStatus    Status    { get; set; } = ComplianceStatus.Open;
        public int    FlaggedInvestorId { get; set; }
        public string FlaggedEntityType { get; set; } = string.Empty;
        public int?   OrderId           { get; set; }
        public string Description       { get; set; } = string.Empty;
        public decimal? TradeValue      { get; set; }
        public string?  PatternData     { get; set; }
        public DateTime DetectedAt      { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt     { get; set; }
        public string?   ResolvedBy     { get; set; }
        public string?   ResolutionNotes { get; set; }
        public bool      IsEscalated    { get; set; }
        public string    CreatedBy      { get; set; } = string.Empty;
    }

    public class ComplianceSettings
    {
        public decimal LargeTradeThresholdBDT    { get; set; } = 5_000_000m;
        public decimal DailyVolumeThresholdBDT   { get; set; } = 20_000_000m;
        public int     AMLStructuringWindowHours  { get; set; } = 24;
        public int     AMLStructuringMaxTransactions { get; set; } = 10;
        public int     WashTradeWindowMinutes     { get; set; } = 30;
        public int     SuspiciousFrequencyPerHour { get; set; } = 20;
    }

    public class ComplianceFilterDto
    {
        public int?  BrokerageHouseId { get; set; }
        public int?  InvestorId       { get; set; }
        public ComplianceAlertType? AlertType { get; set; }
        public ComplianceSeverity?  Severity  { get; set; }
        public ComplianceStatus?    Status    { get; set; }
        public DateTime? From     { get; set; }
        public DateTime? To       { get; set; }
        public int Page     { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ResolveComplianceDto
    {
        public ComplianceStatus NewStatus      { get; set; }
        public string ResolutionNotes          { get; set; } = string.Empty;
        public string ResolvedBy               { get; set; } = string.Empty;
    }

    public class ComplianceSummaryDto
    {
        public int TotalOpen      { get; set; }
        public int TotalResolved  { get; set; }
        public int TotalEscalated { get; set; }
        public int CriticalCount  { get; set; }
        public int HighCount      { get; set; }
        public Dictionary<string, int> ByAlertType { get; set; } = new();
    }

    public class ComplianceExportDto
    {
        public int      BrokerageHouseId { get; set; }
        public DateTime From             { get; set; }
        public DateTime To               { get; set; }
        public string   Format           { get; set; } = "csv";
    }
}
