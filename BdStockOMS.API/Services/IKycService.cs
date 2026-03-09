using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class KycSubmitRequest
    {
        public int UserId { get; set; }
        public KycDocumentType DocumentType { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? BackFilePath { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class KycReviewRequest
    {
        public int KycDocumentId { get; set; }
        public int ActorUserId { get; set; }
        public ApprovalAction Action { get; set; }
        public string? Remarks { get; set; }
    }

    public interface IKycService
    {
        Task<KycDocument> SubmitDocumentAsync(KycSubmitRequest request);
        Task<KycDocument> ReviewDocumentAsync(KycReviewRequest request);
        Task<IEnumerable<KycDocument>> GetDocumentsByUserAsync(int userId);
        Task<IEnumerable<KycDocument>> GetPendingDocumentsAsync(int brokerageHouseId);
        Task<bool> IsKycApprovedAsync(int userId);
        Task<KycDocument?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<KycApproval>> GetApprovalHistoryAsync(int kycDocumentId);
    }
}
