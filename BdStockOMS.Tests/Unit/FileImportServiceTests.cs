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
    public class FileImportServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly FileImportService _svc;

        public FileImportServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _svc = new FileImportService(_db);
            SeedData();
        }

        private void SeedData()
        {
            _db.Roles.Add(new Role { Id = 1, Name = "Admin" });
            _db.BrokerageHouses.Add(new BrokerageHouse
            {
                Id = 1, Name = "Test BH", LicenseNumber = "LIC001",
                Email = "bh@test.com", Phone = "01700000000",
                Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
            });
            _db.Users.Add(new User
            {
                Id = 1, FullName = "Admin User", Email = "admin@test.com",
                PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1,
                IsActive = true, CreatedAt = DateTime.UtcNow
            });
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private FileImportRequest MakeTradeRequest(string csv) => new FileImportRequest
        {
            UploadedByUserId = 1,
            BrokerageHouseId = 1,
            FileType = ImportFileType.TradeUpload,
            FileName = "trades.csv",
            CsvContent = csv
        };

        // ── Stage Tests ───────────────────────────────────

        [Fact]
        public async Task Stage_ValidCsv_CreatesBatch()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            Assert.NotNull(batch);
            Assert.Equal(ImportBatchStatus.Staged, batch.Status);
        }

        [Fact]
        public async Task Stage_ParsesRowCount()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            Assert.Equal(2, batch.TotalRows);
        }

        [Fact]
        public async Task Stage_SkipsHeaderRow()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            Assert.Equal(1, batch.TotalRows);
        }

        [Fact]
        public async Task Stage_CreatesRowRecords()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var rows = await _db.FileImportRows.Where(r => r.FileImportBatchId == batch.Id).ToListAsync();
            Assert.Equal(2, rows.Count);
        }

        [Fact]
        public async Task Stage_RowsHavePendingStatus()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var rows = await _db.FileImportRows.Where(r => r.FileImportBatchId == batch.Id).ToListAsync();
            Assert.All(rows, r => Assert.Equal(RowStatus.Pending, r.Status));
        }

        [Fact]
        public async Task Stage_StoresFileName()
        {
            var batch = await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            Assert.Equal("trades.csv", batch.FileName);
        }

        [Fact]
        public async Task Stage_StoresBrokerageHouseId()
        {
            var batch = await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            Assert.Equal(1, batch.BrokerageHouseId);
        }

        // ── Validate Tests ────────────────────────────────

        [Fact]
        public async Task Validate_AllValidRows_SetsPendingApproval()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var summary = await _svc.ValidateAsync(batch.Id);
            var updated = await _db.FileImportBatches.FindAsync(batch.Id);
            Assert.Equal(ImportBatchStatus.PendingApproval, updated!.Status);
        }

        [Fact]
        public async Task Validate_InvalidRows_SetsValidationFailed()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,INVALID,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var summary = await _svc.ValidateAsync(batch.Id);
            var updated = await _db.FileImportBatches.FindAsync(batch.Id);
            Assert.Equal(ImportBatchStatus.ValidationFailed, updated!.Status);
        }

        [Fact]
        public async Task Validate_ReturnsCorrectValidCount()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var summary = await _svc.ValidateAsync(batch.Id);
            Assert.Equal(2, summary.ValidRows);
        }

        [Fact]
        public async Task Validate_ReturnsCorrectInvalidCount()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,INVALID,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var summary = await _svc.ValidateAsync(batch.Id);
            Assert.Equal(1, summary.InvalidRows);
        }

        [Fact]
        public async Task Validate_InvalidQuantity_MarksRowInvalid()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,NOTANUMBER,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            var row = await _db.FileImportRows.FirstAsync(r => r.FileImportBatchId == batch.Id);
            Assert.Equal(RowStatus.Invalid, row.Status);
        }

        [Fact]
        public async Task Validate_ValidRow_StoresParsedData()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            var row = await _db.FileImportRows.FirstAsync(r => r.FileImportBatchId == batch.Id);
            Assert.NotNull(row.ParsedData);
        }

        [Fact]
        public async Task Validate_BatchNotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _svc.ValidateAsync(9999));
        }

        [Fact]
        public async Task Validate_FundUpload_ValidRow()
        {
            var req = new FileImportRequest
            {
                UploadedByUserId = 1, BrokerageHouseId = 1,
                FileType = ImportFileType.FundUpload,
                FileName = "funds.csv",
                CsvContent = "Email,Amount\ninv@test.com,50000"
            };
            var batch = await _svc.StageAsync(req);
            var summary = await _svc.ValidateAsync(batch.Id);
            Assert.Equal(1, summary.ValidRows);
        }

        [Fact]
        public async Task Validate_InvestorUpload_ValidRow()
        {
            var req = new FileImportRequest
            {
                UploadedByUserId = 1, BrokerageHouseId = 1,
                FileType = ImportFileType.InvestorUpload,
                FileName = "investors.csv",
                CsvContent = "FullName,Email,Phone\nJohn Doe,john@test.com,01700000001"
            };
            var batch = await _svc.StageAsync(req);
            var summary = await _svc.ValidateAsync(batch.Id);
            Assert.Equal(1, summary.ValidRows);
        }

        // ── Approve / Reject Tests ────────────────────────

        [Fact]
        public async Task Approve_PendingApprovalBatch_SetsApproved()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            var approved = await _svc.ApproveAsync(batch.Id, 1);
            Assert.Equal(ImportBatchStatus.Approved, approved.Status);
        }

        [Fact]
        public async Task Approve_SetsApprovedByUserId()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            var approved = await _svc.ApproveAsync(batch.Id, 1);
            Assert.Equal(1, approved.ApprovedByUserId);
        }

        [Fact]
        public async Task Approve_NotPendingApproval_ThrowsInvalidOperation()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _svc.ApproveAsync(batch.Id, 1));
        }

        [Fact]
        public async Task Reject_SetsBatchRejected()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var rejected = await _svc.RejectAsync(batch.Id, 1, "Bad data");
            Assert.Equal(ImportBatchStatus.Rejected, rejected.Status);
        }

        [Fact]
        public async Task Reject_StoresReason()
        {
            var batch = await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            var rejected = await _svc.RejectAsync(batch.Id, 1, "Bad data");
            Assert.Equal("Bad data", rejected.Notes);
        }

        // ── Commit Tests ──────────────────────────────────

        [Fact]
        public async Task Commit_ApprovedBatch_ReturnsCommittedCount()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            await _svc.ApproveAsync(batch.Id, 1);
            var committed = await _svc.CommitAsync(batch.Id);
            Assert.Equal(2, committed);
        }

        [Fact]
        public async Task Commit_SetsBatchStatusCommitted()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            await _svc.ApproveAsync(batch.Id, 1);
            await _svc.CommitAsync(batch.Id);
            var updated = await _db.FileImportBatches.FindAsync(batch.Id);
            Assert.Equal(ImportBatchStatus.Committed, updated!.Status);
        }

        [Fact]
        public async Task Commit_NotApproved_ThrowsInvalidOperation()
        {
            var batch = await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CommitAsync(batch.Id));
        }

        [Fact]
        public async Task Commit_SetsRowsToCommitted()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            await _svc.ValidateAsync(batch.Id);
            await _svc.ApproveAsync(batch.Id, 1);
            await _svc.CommitAsync(batch.Id);
            var rows = await _db.FileImportRows.Where(r => r.FileImportBatchId == batch.Id).ToListAsync();
            Assert.All(rows, r => Assert.Equal(RowStatus.Committed, r.Status));
        }

        // ── Query Tests ───────────────────────────────────

        [Fact]
        public async Task GetBatch_ExistingId_ReturnsBatch()
        {
            var batch = await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            var result = await _svc.GetBatchAsync(batch.Id);
            Assert.NotNull(result);
            Assert.Equal(batch.Id, result!.Id);
        }

        [Fact]
        public async Task GetBatch_NonExistent_ReturnsNull()
        {
            var result = await _svc.GetBatchAsync(9999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBatchesByBrokerageHouse_ReturnsCorrectBatches()
        {
            await _svc.StageAsync(MakeTradeRequest("H\nGP,BUY,10,400"));
            await _svc.StageAsync(MakeTradeRequest("H\nBRAC,SELL,5,50"));
            var batches = await _svc.GetBatchesByBrokerageHouseAsync(1);
            Assert.Equal(2, batches.Count());
        }

        [Fact]
        public async Task GetRows_ReturnsBatchRows()
        {
            var csv = "StockCode,Side,Quantity,Price\nGP,BUY,10,400\nBRAC,SELL,5,50";
            var batch = await _svc.StageAsync(MakeTradeRequest(csv));
            var rows = await _svc.GetRowsAsync(batch.Id);
            Assert.Equal(2, rows.Count());
        }
    }
}
