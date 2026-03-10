using System;
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models
{
    public class BosImportLog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? Md5Hash { get; set; }

        public bool Md5Verified { get; set; } = false;

        public int RecordsProcessed { get; set; }
        public int RecordsFailed { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(2000)]
        public string? ErrorDetails { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string ImportedBy { get; set; } = string.Empty;
    }
}
