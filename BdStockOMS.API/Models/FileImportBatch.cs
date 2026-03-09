using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public enum ImportFileType
    {
        TradeUpload,
        PortfolioUpload,
        FundUpload,
        InvestorUpload
    }

    public enum ImportBatchStatus
    {
        Staged,
        Validating,
        ValidationFailed,
        PendingApproval,
        Approved,
        Committed,
        Rejected
    }

    public class FileImportBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UploadedByUserId { get; set; }

        [Required]
        public int BrokerageHouseId { get; set; }

        [Required]
        public ImportFileType FileType { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Staged;

        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UploadedByUserId))]
        public User UploadedByUser { get; set; } = null!;

        [ForeignKey(nameof(BrokerageHouseId))]
        public BrokerageHouse BrokerageHouse { get; set; } = null!;

        public ICollection<FileImportRow> Rows { get; set; } = new List<FileImportRow>();
    }
}
