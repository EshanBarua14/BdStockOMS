using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public enum ApprovalAction
    {
        SubmittedForReview,
        ApprovedByAgent,
        RejectedByAgent,
        EscalatedToSupervisor,
        ApprovedBySupervisor,
        RejectedBySupervisor,
        Expired
    }

    public class KycApproval
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KycDocumentId { get; set; }

        [Required]
        public int ActorUserId { get; set; }

        [Required]
        public ApprovalAction Action { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime ActionedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(KycDocumentId))]
        public KycDocument KycDocument { get; set; } = null!;

        [ForeignKey(nameof(ActorUserId))]
        public User ActorUser { get; set; } = null!;
    }
}
