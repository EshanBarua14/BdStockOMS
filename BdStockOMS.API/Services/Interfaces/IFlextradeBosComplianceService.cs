using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface IFlextradeBosComplianceService
    {
        Task<BosComplianceReport> RunComplianceCheckAsync(int brokerageHouseId);
        Task<BosComplianceReport> GetCachedComplianceReportAsync(int brokerageHouseId);
        Task<List<BosComplianceReport>> GetAllBrokerageComplianceAsync();
        Task RefreshAllAsync();
    }

    public class BosComplianceReport
    {
        public int BrokerageHouseId { get; set; }
        public string BrokerageName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompliant => Checks.Count > 0 && Checks.TrueForAll(c => c.Passed);
        public int PassedCount => Checks.Count(c => c.Passed);
        public int FailedCount => Checks.Count(c => !c.Passed);
        public List<BosComplianceCheck> Checks { get; set; } = new();
        public bool FromCache { get; set; }
    }

    public class BosComplianceCheck
    {
        public string CheckName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? FailureReason { get; set; }
        public string Severity { get; set; } = "Critical"; // Critical | Warning | Info
    }

    public static class BosComplianceCheckNames
    {
        public const string BrokerageSettingsExist     = "BrokerageSettingsExist";
        public const string TrecNumberConfigured       = "TrecNumberConfigured";
        public const string ActiveBranchExists         = "ActiveBranchExists";
        public const string CommissionRatesConfigured  = "CommissionRatesConfigured";
        public const string RmsLimitsConfigured        = "RmsLimitsConfigured";
        public const string ActiveTraderExists         = "ActiveTraderExists";
        public const string BosImportWithin24Hours     = "BosImportWithin24Hours";
        public const string BoAccountFormatValid       = "BoAccountFormatValid";
        public const string KycQueueClear              = "KycQueueClear";
        public const string SettlementUpToDate         = "SettlementUpToDate";
    }
}
