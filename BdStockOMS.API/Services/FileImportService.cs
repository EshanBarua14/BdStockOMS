using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class FileImportService : IFileImportService
    {
        private readonly AppDbContext _db;

        public FileImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<FileImportBatch> StageAsync(FileImportRequest request)
        {
            var lines = request.CsvContent
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            // Skip header row
            var dataLines = lines.Count > 1 ? lines.Skip(1).ToList() : lines;

            var batch = new FileImportBatch
            {
                UploadedByUserId = request.UploadedByUserId,
                BrokerageHouseId = request.BrokerageHouseId,
                FileType         = request.FileType,
                FileName         = request.FileName,
                Status           = ImportBatchStatus.Staged,
                TotalRows        = dataLines.Count,
                UploadedAt       = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow
            };

            _db.FileImportBatches.Add(batch);
            await _db.SaveChangesAsync();

            var rows = dataLines.Select((line, index) => new FileImportRow
            {
                FileImportBatchId = batch.Id,
                RowNumber         = index + 1,
                RawData           = line,
                Status            = RowStatus.Pending,
                CreatedAt         = DateTime.UtcNow
            }).ToList();

            _db.FileImportRows.AddRange(rows);
            await _db.SaveChangesAsync();

            return batch;
        }

        public async Task<ValidationSummary> ValidateAsync(int batchId)
        {
            var batch = await _db.FileImportBatches
                .Include(b => b.Rows)
                .FirstOrDefaultAsync(b => b.Id == batchId)
                ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

            batch.Status    = ImportBatchStatus.Validating;
            batch.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var errors  = new List<string>();
            int valid   = 0;
            int invalid = 0;

            foreach (var row in batch.Rows)
            {
                var (isValid, error, parsed) = ValidateRow(batch.FileType, row.RawData, row.RowNumber);
                if (isValid)
                {
                    row.Status     = RowStatus.Valid;
                    row.ParsedData = parsed;
                    valid++;
                }
                else
                {
                    row.Status          = RowStatus.Invalid;
                    row.ValidationError = error;
                    errors.Add($"Row {row.RowNumber}: {error}");
                    invalid++;
                }
            }

            batch.ValidRows   = valid;
            batch.InvalidRows = invalid;
            batch.Status      = invalid == 0
                                ? ImportBatchStatus.PendingApproval
                                : ImportBatchStatus.ValidationFailed;
            batch.UpdatedAt   = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ValidationSummary
            {
                BatchId     = batchId,
                TotalRows   = batch.TotalRows,
                ValidRows   = valid,
                InvalidRows = invalid,
                Errors      = errors
            };
        }

        private (bool isValid, string? error, string? parsed) ValidateRow(
            ImportFileType fileType, string rawData, int rowNumber)
        {
            var cols = rawData.Split(',');

            switch (fileType)
            {
                case ImportFileType.TradeUpload:
                    // Expected: StockCode, Side, Quantity, Price
                    if (cols.Length < 4)
                        return (false, "Expected 4 columns: StockCode,Side,Quantity,Price", null);
                    if (!int.TryParse(cols[2].Trim(), out var qty) || qty <= 0)
                        return (false, "Quantity must be a positive integer", null);
                    if (!decimal.TryParse(cols[3].Trim(), out var price) || price <= 0)
                        return (false, "Price must be a positive decimal", null);
                    var side = cols[1].Trim().ToUpper();
                    if (side != "BUY" && side != "SELL")
                        return (false, "Side must be BUY or SELL", null);
                    return (true, null, JsonSerializer.Serialize(new { StockCode = cols[0].Trim(), Side = side, Quantity = qty, Price = price }));

                case ImportFileType.FundUpload:
                    // Expected: InvestorEmail, Amount
                    if (cols.Length < 2)
                        return (false, "Expected 2 columns: InvestorEmail,Amount", null);
                    if (!decimal.TryParse(cols[1].Trim(), out var amount) || amount <= 0)
                        return (false, "Amount must be a positive decimal", null);
                    return (true, null, JsonSerializer.Serialize(new { Email = cols[0].Trim(), Amount = amount }));

                case ImportFileType.InvestorUpload:
                    // Expected: FullName, Email, Phone
                    if (cols.Length < 3)
                        return (false, "Expected 3 columns: FullName,Email,Phone", null);
                    if (!cols[1].Trim().Contains('@'))
                        return (false, "Invalid email address", null);
                    return (true, null, JsonSerializer.Serialize(new { FullName = cols[0].Trim(), Email = cols[1].Trim(), Phone = cols[2].Trim() }));

                default:
                    if (cols.Length < 1)
                        return (false, "Empty row", null);
                    return (true, null, rawData);
            }
        }

        public async Task<FileImportBatch> ApproveAsync(int batchId, int approverUserId)
        {
            var batch = await _db.FileImportBatches.FindAsync(batchId)
                        ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

            if (batch.Status != ImportBatchStatus.PendingApproval)
                throw new InvalidOperationException($"Batch must be in PendingApproval status to approve. Current: {batch.Status}");

            batch.Status           = ImportBatchStatus.Approved;
            batch.ApprovedByUserId = approverUserId;
            batch.ApprovedAt       = DateTime.UtcNow;
            batch.UpdatedAt        = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return batch;
        }

        public async Task<FileImportBatch> RejectAsync(int batchId, int approverUserId, string reason)
        {
            var batch = await _db.FileImportBatches.FindAsync(batchId)
                        ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

            batch.Status           = ImportBatchStatus.Rejected;
            batch.ApprovedByUserId = approverUserId;
            batch.Notes            = reason;
            batch.UpdatedAt        = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return batch;
        }

        public async Task<int> CommitAsync(int batchId)
        {
            var batch = await _db.FileImportBatches
                .Include(b => b.Rows)
                .FirstOrDefaultAsync(b => b.Id == batchId)
                ?? throw new KeyNotFoundException($"Batch {batchId} not found.");

            if (batch.Status != ImportBatchStatus.Approved)
                throw new InvalidOperationException($"Batch must be Approved before committing. Current: {batch.Status}");

            int committed = 0;
            foreach (var row in batch.Rows.Where(r => r.Status == RowStatus.Valid))
            {
                row.Status = RowStatus.Committed;
                committed++;
            }

            batch.Status    = ImportBatchStatus.Committed;
            batch.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return committed;
        }

        public async Task<FileImportBatch?> GetBatchAsync(int batchId)
        {
            return await _db.FileImportBatches
                .Include(b => b.Rows)
                .Include(b => b.UploadedByUser)
                .FirstOrDefaultAsync(b => b.Id == batchId);
        }

        public async Task<IEnumerable<FileImportBatch>> GetBatchesByBrokerageHouseAsync(int brokerageHouseId)
        {
            return await _db.FileImportBatches
                .Where(b => b.BrokerageHouseId == brokerageHouseId)
                .OrderByDescending(b => b.UploadedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileImportRow>> GetRowsAsync(int batchId)
        {
            return await _db.FileImportRows
                .Where(r => r.FileImportBatchId == batchId)
                .OrderBy(r => r.RowNumber)
                .ToListAsync();
        }
    }
}
