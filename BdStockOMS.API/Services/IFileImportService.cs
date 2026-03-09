using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class FileImportRequest
    {
        public int UploadedByUserId { get; set; }
        public int BrokerageHouseId { get; set; }
        public ImportFileType FileType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string CsvContent { get; set; } = string.Empty;
    }

    public class ValidationSummary
    {
        public int BatchId { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public interface IFileImportService
    {
        Task<FileImportBatch> StageAsync(FileImportRequest request);
        Task<ValidationSummary> ValidateAsync(int batchId);
        Task<FileImportBatch> ApproveAsync(int batchId, int approverUserId);
        Task<FileImportBatch> RejectAsync(int batchId, int approverUserId, string reason);
        Task<int> CommitAsync(int batchId);
        Task<FileImportBatch?> GetBatchAsync(int batchId);
        Task<IEnumerable<FileImportBatch>> GetBatchesByBrokerageHouseAsync(int brokerageHouseId);
        Task<IEnumerable<FileImportRow>> GetRowsAsync(int batchId);
    }
}
