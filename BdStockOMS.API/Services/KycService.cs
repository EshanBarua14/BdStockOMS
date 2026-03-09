using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class KycService : IKycService
    {
        private readonly AppDbContext _db;

        public KycService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<KycDocument> SubmitDocumentAsync(KycSubmitRequest request)
        {
            var existing = await _db.KycDocuments
                .FirstOrDefaultAsync(d => d.UserId == request.UserId &&
                                          d.DocumentType == request.DocumentType &&
                                          d.Status != KycStatus.Rejected &&
                                          d.Status != KycStatus.Expired);

            if (existing != null)
                throw new InvalidOperationException(
                    $"A {request.DocumentType} document is already {existing.Status} for this user.");

            var doc = new KycDocument
            {
                UserId        = request.UserId,
                DocumentType  = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                FilePath      = request.FilePath,
                BackFilePath  = request.BackFilePath,
                ExpiryDate    = request.ExpiryDate,
                Status        = KycStatus.Pending,
                UploadedAt    = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };

            _db.KycDocuments.Add(doc);
            await _db.SaveChangesAsync();
            return doc;
        }

        public async Task<KycDocument> ReviewDocumentAsync(KycReviewRequest request)
        {
            var doc = await _db.KycDocuments.FindAsync(request.KycDocumentId)
                      ?? throw new KeyNotFoundException($"KYC document {request.KycDocumentId} not found.");

            // Update document status based on action
            doc.Status = request.Action switch
            {
                ApprovalAction.SubmittedForReview    => KycStatus.UnderReview,
                ApprovalAction.ApprovedByAgent       => KycStatus.Approved,
                ApprovalAction.ApprovedBySupervisor  => KycStatus.Approved,
                ApprovalAction.RejectedByAgent       => KycStatus.Rejected,
                ApprovalAction.RejectedBySupervisor  => KycStatus.Rejected,
                ApprovalAction.Expired               => KycStatus.Expired,
                _                                    => doc.Status
            };

            if (doc.Status == KycStatus.Rejected)
                doc.RejectionReason = request.Remarks;

            doc.UpdatedAt = DateTime.UtcNow;

            var approval = new KycApproval
            {
                KycDocumentId = request.KycDocumentId,
                ActorUserId   = request.ActorUserId,
                Action        = request.Action,
                Remarks       = request.Remarks,
                ActionedAt    = DateTime.UtcNow
            };

            _db.KycApprovals.Add(approval);
            await _db.SaveChangesAsync();
            return doc;
        }

        public async Task<IEnumerable<KycDocument>> GetDocumentsByUserAsync(int userId)
        {
            return await _db.KycDocuments
                .Where(d => d.UserId == userId)
                .Include(d => d.KycApprovals)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<KycDocument>> GetPendingDocumentsAsync(int brokerageHouseId)
        {
            return await _db.KycDocuments
                .Include(d => d.User)
                .Where(d => (d.Status == KycStatus.Pending || d.Status == KycStatus.UnderReview)
                            && d.User.BrokerageHouseId == brokerageHouseId)
                .OrderBy(d => d.UploadedAt)
                .ToListAsync();
        }

        public async Task<bool> IsKycApprovedAsync(int userId)
        {
            return await _db.KycDocuments
                .AnyAsync(d => d.UserId == userId && d.Status == KycStatus.Approved);
        }

        public async Task<KycDocument?> GetDocumentByIdAsync(int id)
        {
            return await _db.KycDocuments
                .Include(d => d.User)
                .Include(d => d.KycApprovals)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<KycApproval>> GetApprovalHistoryAsync(int kycDocumentId)
        {
            return await _db.KycApprovals
                .Where(a => a.KycDocumentId == kycDocumentId)
                .Include(a => a.ActorUser)
                .OrderBy(a => a.ActionedAt)
                .ToListAsync();
        }
    }
}
