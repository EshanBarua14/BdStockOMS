using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public enum KycDocumentType
    {
        NationalId,
        Passport,
        DrivingLicense,
        BirthCertificate,
        TaxIdentificationNumber,
        BankStatement,
        UtilityBill
    }

    public enum KycStatus
    {
        Pending,
        UnderReview,
        Approved,
        Rejected,
        Expired
    }

    public class KycDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public KycDocumentType DocumentType { get; set; }

        [Required]
        [MaxLength(200)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? BackFilePath { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public KycStatus Status { get; set; } = KycStatus.Pending;

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<KycApproval> KycApprovals { get; set; } = new List<KycApproval>();
    }
}
