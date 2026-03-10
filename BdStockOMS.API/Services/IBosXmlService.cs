using System.Threading.Tasks;
using System.Collections.Generic;
using BdStockOMS.API.Models;
using System.Collections.Generic;

namespace BdStockOMS.API.Services
{
    public class BosClientRecord
    {
        public string BoAccountNumber { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string NidOrPassport { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }

    public class BosPositionRecord
    {
        public string BoAccountNumber { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal MarketValue { get; set; }
    }

    public class BosReconciliationResult
    {
        public int SessionId { get; set; }
        public bool Md5Verified { get; set; }
        public int TotalRecords { get; set; }
        public int ReconciledRecords { get; set; }
        public int UnmatchedRecords { get; set; }
        public List<string> UnmatchedItems { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class BosUploadRequest
    {
        public int BrokerageHouseId { get; set; }
        public int UploadedByUserId { get; set; }
        public string XmlFileName { get; set; } = string.Empty;
        public string XmlContent { get; set; } = string.Empty;
        public string CtrlFileName { get; set; } = string.Empty;
        public string CtrlContent { get; set; } = string.Empty;
    }

    public class BosExportResult
    {
        public string FileName { get; set; } = string.Empty;
        public string XmlContent { get; set; } = string.Empty;
        public string Md5Hash { get; set; } = string.Empty;
    }

    public interface IBosXmlService
    {
        // MD5 verification
        bool VerifyMd5(string fileContent, string expectedMd5);
        string ComputeMd5(string fileContent);

        // Parsers
        List<BosClientRecord> ParseClientsXml(string xmlContent);
        List<BosPositionRecord> ParsePositionsXml(string xmlContent);

        // Extract expected MD5 from ctrl file
        string ExtractMd5FromCtrl(string ctrlContent);

        // Reconciliation
        Task<BosReconciliationResult> ReconcileClientsAsync(BosUploadRequest request);
        Task<BosReconciliationResult> ReconcilePositionsAsync(BosUploadRequest request);

        // EOD export
        Task<BosExportResult> ExportPositionsToXmlAsync(int brokerageHouseId);

        // Session history
        Task<List<BosImportSession>> GetSessionsAsync(int brokerageHouseId);
    }
}
