using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class BosImportSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BrokerageHouseId { get; set; }

        [Required, MaxLength(50)]
        public string FileType { get; set; } = string.Empty;
        // "Clients-UBR" or "Positions-UBR"

        [Required, MaxLength(500)]
        public string XmlFileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string CtrlFileName { get; set; } = string.Empty;

        [Required, MaxLength(32)]
        public string ExpectedMd5 { get; set; } = string.Empty;

        [Required, MaxLength(32)]
        public string ActualMd5 { get; set; } = string.Empty;

        public bool Md5Verified { get; set; }

        public int TotalRecords { get; set; }
        public int ReconciledRecords { get; set; }
        public int UnmatchedRecords { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending";
        // Pending | Verified | Reconciled | Failed

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public int ImportedByUserId { get; set; }

        [ForeignKey("BrokerageHouseId")]
        public BrokerageHouse BrokerageHouse { get; set; } = null!;

        [ForeignKey("ImportedByUserId")]
        public User ImportedBy { get; set; } = null!;
    }
}
