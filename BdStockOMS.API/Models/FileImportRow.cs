using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public enum RowStatus
    {
        Pending,
        Valid,
        Invalid,
        Committed,
        Skipped
    }

    public class FileImportRow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FileImportBatchId { get; set; }

        public int RowNumber { get; set; }

        [Required]
        [MaxLength(2000)]
        public string RawData { get; set; } = string.Empty;

        public RowStatus Status { get; set; } = RowStatus.Pending;

        [MaxLength(1000)]
        public string? ValidationError { get; set; }

        [MaxLength(2000)]
        public string? ParsedData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(FileImportBatchId))]
        public FileImportBatch FileImportBatch { get; set; } = null!;
    }
}
