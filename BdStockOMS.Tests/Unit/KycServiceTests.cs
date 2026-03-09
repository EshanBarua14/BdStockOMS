using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;

namespace BdStockOMS.Tests.Unit
{
    public class KycServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly KycService _svc;

        public KycServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _svc = new KycService(_db);
            SeedData();
        }

        private void SeedData()
        {
            _db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            _db.BrokerageHouses.Add(new BrokerageHouse
            {
                Id = 1, Name = "Test BH",
                LicenseNumber = "LIC001", Email = "bh@test.com",
                Phone = "01700000000", Address = "Dhaka",
                IsActive = true, CreatedAt = DateTime.UtcNow
            });
            _db.Users.AddRange(
                new User { Id = 1, FullName = "Investor One", Email = "inv1@test.com", PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 2, FullName = "Investor Two", Email = "inv2@test.com", PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 3, FullName = "CCD Agent",    Email = "ccd@test.com",  PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private KycSubmitRequest MakeSubmit(int userId, KycDocumentType type, string docNum)
            => new KycSubmitRequest { UserId = userId, DocumentType = type, DocumentNumber = docNum, FilePath = "/f.jpg" };

        [Fact]
        public async Task SubmitDocument_ValidRequest_ReturnsDocument()
        {
            var result = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-001"));
            Assert.NotNull(result);
            Assert.Equal(KycStatus.Pending, result.Status);
        }

        [Fact]
        public async Task SubmitDocument_SetsUploadedAt()
        {
            var result = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.Passport, "PP-001"));
            Assert.True(result.UploadedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task SubmitDocument_DuplicatePending_ThrowsInvalidOperation()
        {
            await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-001"));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-002")));
        }

        [Fact]
        public async Task SubmitDocument_AfterRejected_AllowsResubmit()
        {
            _db.KycDocuments.Add(new KycDocument { UserId = 1, DocumentType = KycDocumentType.NationalId, DocumentNumber = "OLD", FilePath = "/old.jpg", Status = KycStatus.Rejected, UploadedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
            var result = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-NEW"));
            Assert.Equal(KycStatus.Pending, result.Status);
        }

        [Fact]
        public async Task SubmitDocument_WithBackFilePath_Stored()
        {
            var req = MakeSubmit(1, KycDocumentType.DrivingLicense, "DL-001");
            req.BackFilePath = "/back.jpg";
            var result = await _svc.SubmitDocumentAsync(req);
            Assert.Equal("/back.jpg", result.BackFilePath);
        }

        [Fact]
        public async Task SubmitDocument_WithExpiryDate_Stored()
        {
            var expiry = new DateTime(2030, 1, 1);
            var req = MakeSubmit(1, KycDocumentType.Passport, "PP-EXP");
            req.ExpiryDate = expiry;
            var result = await _svc.SubmitDocumentAsync(req);
            Assert.Equal(expiry, result.ExpiryDate);
        }

        [Fact]
        public async Task SubmitDocument_DifferentTypes_BothAllowed()
        {
            var r1 = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId,   "NID-1"));
            var r2 = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.BankStatement, "BS-1"));
            Assert.NotEqual(r1.Id, r2.Id);
        }

        [Fact]
        public async Task SubmitDocument_SavedToDatabase()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.TaxIdentificationNumber, "TIN-001"));
            var fromDb = await _db.KycDocuments.FindAsync(doc.Id);
            Assert.NotNull(fromDb);
            Assert.Equal("TIN-001", fromDb!.DocumentNumber);
        }

        [Fact]
        public async Task ReviewDocument_SubmitForReview_SetsUnderReview()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-R1"));
            var result = await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.SubmittedForReview });
            Assert.Equal(KycStatus.UnderReview, result.Status);
        }

        [Fact]
        public async Task ReviewDocument_ApproveByAgent_SetsApproved()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-A1"));
            var result = await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent });
            Assert.Equal(KycStatus.Approved, result.Status);
        }

        [Fact]
        public async Task ReviewDocument_RejectByAgent_SetsRejectedWithReason()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-REJ"));
            var result = await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.RejectedByAgent, Remarks = "Blurry" });
            Assert.Equal(KycStatus.Rejected, result.Status);
            Assert.Equal("Blurry", result.RejectionReason);
        }

        [Fact]
        public async Task ReviewDocument_NotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = 9999, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent }));
        }

        [Fact]
        public async Task ReviewDocument_CreatesApprovalRecord()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "NID-AR"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent, Remarks = "OK" });
            var history = await _svc.GetApprovalHistoryAsync(doc.Id);
            Assert.Single(history);
        }

        [Fact]
        public async Task ReviewDocument_ApproveBySupervisor_SetsApproved()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(2, KycDocumentType.Passport, "PP-SUP"));
            var result = await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedBySupervisor });
            Assert.Equal(KycStatus.Approved, result.Status);
        }

        [Fact]
        public async Task GetDocumentsByUser_ReturnsOnlyUserDocs()
        {
            await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            await _svc.SubmitDocumentAsync(MakeSubmit(2, KycDocumentType.NationalId, "N2"));
            var docs = await _svc.GetDocumentsByUserAsync(1);
            Assert.All(docs, d => Assert.Equal(1, d.UserId));
        }

        [Fact]
        public async Task GetDocumentsByUser_EmptyForNewUser_ReturnsEmpty()
        {
            var docs = await _svc.GetDocumentsByUserAsync(99);
            Assert.Empty(docs);
        }

        [Fact]
        public async Task GetPendingDocuments_ReturnsPendingAndUnderReview()
        {
            var doc1 = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            var doc2 = await _svc.SubmitDocumentAsync(MakeSubmit(2, KycDocumentType.NationalId, "N2"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc2.Id, ActorUserId = 3, Action = ApprovalAction.SubmittedForReview });
            var pending = await _svc.GetPendingDocumentsAsync(1);
            Assert.Equal(2, pending.Count());
        }

        [Fact]
        public async Task GetPendingDocuments_ExcludesApproved()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent });
            var pending = await _svc.GetPendingDocumentsAsync(1);
            Assert.Empty(pending);
        }

        [Fact]
        public async Task IsKycApproved_NoDocuments_ReturnsFalse()
        {
            Assert.False(await _svc.IsKycApprovedAsync(1));
        }

        [Fact]
        public async Task IsKycApproved_PendingDocument_ReturnsFalse()
        {
            await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            Assert.False(await _svc.IsKycApprovedAsync(1));
        }

        [Fact]
        public async Task IsKycApproved_ApprovedDocument_ReturnsTrue()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent });
            Assert.True(await _svc.IsKycApprovedAsync(1));
        }

        [Fact]
        public async Task IsKycApproved_RejectedDocument_ReturnsFalse()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.RejectedByAgent, Remarks = "Bad" });
            Assert.False(await _svc.IsKycApprovedAsync(1));
        }

        [Fact]
        public async Task GetDocumentById_ExistingId_ReturnsDocument()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            var result = await _svc.GetDocumentByIdAsync(doc.Id);
            Assert.NotNull(result);
            Assert.Equal(doc.Id, result!.Id);
        }

        [Fact]
        public async Task GetDocumentById_NonExistentId_ReturnsNull()
        {
            var result = await _svc.GetDocumentByIdAsync(9999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetApprovalHistory_MultipleActions_ReturnsAllInOrder()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.SubmittedForReview });
            await _svc.ReviewDocumentAsync(new KycReviewRequest { KycDocumentId = doc.Id, ActorUserId = 3, Action = ApprovalAction.ApprovedByAgent });
            var history = (await _svc.GetApprovalHistoryAsync(doc.Id)).ToList();
            Assert.Equal(2, history.Count);
            Assert.Equal(ApprovalAction.SubmittedForReview, history[0].Action);
            Assert.Equal(ApprovalAction.ApprovedByAgent,    history[1].Action);
        }

        [Fact]
        public async Task GetApprovalHistory_NoHistory_ReturnsEmpty()
        {
            var doc = await _svc.SubmitDocumentAsync(MakeSubmit(1, KycDocumentType.NationalId, "N1"));
            var history = await _svc.GetApprovalHistoryAsync(doc.Id);
            Assert.Empty(history);
        }
    }
}
