using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.IPO;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services
{
    public class IPOService : IIPOService
    {
        private readonly AppDbContext _db;
        public IPOService(AppDbContext db) => _db = db;

        public async Task<Result<IPOResponseDto>> CreateIPOAsync(CreateIPODto dto, CancellationToken ct = default)
        {
            if (dto.CloseDate <= dto.OpenDate)
                return Result<IPOResponseDto>.Failure("CloseDate must be after OpenDate.");
            if (dto.MaxInvestment < dto.MinInvestment)
                return Result<IPOResponseDto>.Failure("MaxInvestment must be >= MinInvestment.");

            var ipo = new IPO
            {
                StockId        = dto.StockId,
                CompanyName    = dto.CompanyName,
                TradingCode    = dto.TradingCode,
                OfferPrice     = dto.OfferPrice,
                TotalShares    = dto.TotalShares,
                SharesRemaining = dto.TotalShares,
                MinInvestment  = dto.MinInvestment,
                MaxInvestment  = dto.MaxInvestment,
                OpenDate       = dto.OpenDate,
                CloseDate      = dto.CloseDate,
                Description    = dto.Description,
                Status         = IPOStatus.Upcoming
            };
            _db.IPOs.Add(ipo);
            await _db.SaveChangesAsync(ct);
            return Result<IPOResponseDto>.Success(ToDto(ipo));
        }

        public async Task<Result<IPOResponseDto>> GetIPOAsync(int id, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == id, ct);
            return ipo == null
                ? Result<IPOResponseDto>.Failure("IPO not found.")
                : Result<IPOResponseDto>.Success(ToDto(ipo));
        }

        public async Task<Result<List<IPOResponseDto>>> GetAllIPOsAsync(string? status, CancellationToken ct = default)
        {
            var q = _db.IPOs.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<IPOStatus>(status, true, out var s))
                q = q.Where(i => i.Status == s);
            var list = await q.OrderByDescending(i => i.OpenDate).ToListAsync(ct);
            return Result<List<IPOResponseDto>>.Success(list.Select(ToDto).ToList());
        }

        public async Task<Result<IPOApplicationResponseDto>> ApplyAsync(ApplyIPODto dto, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == dto.IPOId, ct);
            if (ipo == null)          return Result<IPOApplicationResponseDto>.Failure("IPO not found.");
            if (ipo.Status != IPOStatus.Open) return Result<IPOApplicationResponseDto>.Failure("IPO is not open for applications.");

            var amount = dto.AppliedShares * ipo.OfferPrice;
            if (amount < ipo.MinInvestment) return Result<IPOApplicationResponseDto>.Failure("Applied amount below minimum investment.");
            if (amount > ipo.MaxInvestment) return Result<IPOApplicationResponseDto>.Failure("Applied amount exceeds maximum investment.");

            var existing = await _db.IPOApplications
                .AnyAsync(a => a.IPOId == dto.IPOId && a.InvestorId == dto.InvestorId, ct);
            if (existing) return Result<IPOApplicationResponseDto>.Failure("Investor has already applied for this IPO.");

            var application = new IPOApplication
            {
                IPOId            = dto.IPOId,
                InvestorId       = dto.InvestorId,
                BrokerageHouseId = dto.BrokerageHouseId,
                AppliedShares    = dto.AppliedShares,
                AppliedAmount    = amount,
                Status           = IPOApplicationStatus.Pending
            };
            _db.IPOApplications.Add(application);
            await _db.SaveChangesAsync(ct);
            return Result<IPOApplicationResponseDto>.Success(ToAppDto(application, ipo.CompanyName));
        }

        public async Task<Result<IPOAllocationResultDto>> AllocateAsync(int ipoId, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == ipoId, ct);
            if (ipo == null)                   return Result<IPOAllocationResultDto>.Failure("IPO not found.");
            if (ipo.Status != IPOStatus.Closed) return Result<IPOAllocationResultDto>.Failure("IPO must be closed before allocation.");

            var applications = await _db.IPOApplications
                .Where(a => a.IPOId == ipoId && a.Status == IPOApplicationStatus.Pending)
                .ToListAsync(ct);

            if (!applications.Any()) return Result<IPOAllocationResultDto>.Failure("No pending applications found.");

            var totalApplied = applications.Sum(a => a.AppliedShares);
            var isOversubscribed = totalApplied > ipo.TotalShares;
            var ratio = isOversubscribed ? (double)ipo.TotalShares / totalApplied : 1.0;

            int allocatedCount = 0;
            decimal totalRefund = 0;

            foreach (var app in applications)
            {
                var allocated = (int)Math.Floor(app.AppliedShares * ratio);
                var allocatedAmount = allocated * ipo.OfferPrice;
                var refund = app.AppliedAmount - allocatedAmount;

                app.AllocatedShares = allocated;
                app.AllocatedAmount = allocatedAmount;
                app.RefundAmount    = refund;
                app.Status          = allocated > 0 ? IPOApplicationStatus.Allocated : IPOApplicationStatus.Rejected;
                app.AllocatedAt     = DateTime.UtcNow;
                if (allocated > 0) allocatedCount++;
                totalRefund += refund;
            }

            ipo.Status         = IPOStatus.Allocated;
            ipo.AllocationDate = DateTime.UtcNow;
            ipo.SharesRemaining = 0;

            await _db.SaveChangesAsync(ct);

            return Result<IPOAllocationResultDto>.Success(new IPOAllocationResultDto
            {
                IPOId              = ipoId,
                CompanyName        = ipo.CompanyName,
                TotalApplications  = applications.Count,
                TotalAppliedShares = totalApplied,
                TotalShares        = ipo.TotalShares,
                IsOversubscribed   = isOversubscribed,
                SubscriptionRatio  = (decimal)Math.Round(ratio, 4),
                AllocatedCount     = allocatedCount,
                TotalRefundAmount  = totalRefund
            });
        }

        public async Task<Result<int>> ProcessRefundsAsync(int ipoId, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == ipoId, ct);
            if (ipo == null) return Result<int>.Failure("IPO not found.");
            if (ipo.Status != IPOStatus.Allocated) return Result<int>.Failure("IPO must be in Allocated status before processing refunds.");

            var toRefund = await _db.IPOApplications
                .Where(a => a.IPOId == ipoId && a.RefundAmount > 0 && a.RefundedAt == null)
                .ToListAsync(ct);

            foreach (var app in toRefund)
            {
                app.RefundedAt = DateTime.UtcNow;
                if (app.Status == IPOApplicationStatus.Allocated)
                    app.Status = IPOApplicationStatus.Refunded;
            }

            ipo.Status = IPOStatus.Refunded;
            await _db.SaveChangesAsync(ct);
            return Result<int>.Success(toRefund.Count);
        }

        public async Task<Result<List<IPOApplicationResponseDto>>> GetApplicationsAsync(int ipoId, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == ipoId, ct);
            if (ipo == null) return Result<List<IPOApplicationResponseDto>>.Failure("IPO not found.");

            var apps = await _db.IPOApplications
                .Where(a => a.IPOId == ipoId)
                .OrderBy(a => a.AppliedAt)
                .ToListAsync(ct);

            return Result<List<IPOApplicationResponseDto>>.Success(
                apps.Select(a => ToAppDto(a, ipo.CompanyName)).ToList());
        }

        public async Task<Result<IPOApplicationResponseDto>> GetApplicationAsync(int applicationId, CancellationToken ct = default)
        {
            var app = await _db.IPOApplications
                .Include(a => a.IPO)
                .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            if (app == null) return Result<IPOApplicationResponseDto>.Failure("Application not found.");
            return Result<IPOApplicationResponseDto>.Success(ToAppDto(app, app.IPO?.CompanyName ?? string.Empty));
        }

        public async Task<Result<bool>> CloseIPOAsync(int ipoId, CancellationToken ct = default)
        {
            var ipo = await _db.IPOs.FirstOrDefaultAsync(i => i.Id == ipoId, ct);
            if (ipo == null) return Result<bool>.Failure("IPO not found.");
            if (ipo.Status != IPOStatus.Open) return Result<bool>.Failure("Only open IPOs can be closed.");
            ipo.Status = IPOStatus.Closed;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        private static IPOResponseDto ToDto(IPO i) => new()
        {
            Id = i.Id, StockId = i.StockId, CompanyName = i.CompanyName,
            TradingCode = i.TradingCode, OfferPrice = i.OfferPrice,
            TotalShares = i.TotalShares, SharesRemaining = i.SharesRemaining,
            MinInvestment = i.MinInvestment, MaxInvestment = i.MaxInvestment,
            OpenDate = i.OpenDate, CloseDate = i.CloseDate,
            AllocationDate = i.AllocationDate, ListingDate = i.ListingDate,
            Status = i.Status.ToString(), Description = i.Description,
            CreatedAt = i.CreatedAt
        };

        private static IPOApplicationResponseDto ToAppDto(IPOApplication a, string companyName) => new()
        {
            Id = a.Id, IPOId = a.IPOId, CompanyName = companyName,
            InvestorId = a.InvestorId, AppliedShares = a.AppliedShares,
            AppliedAmount = a.AppliedAmount, AllocatedShares = a.AllocatedShares,
            AllocatedAmount = a.AllocatedAmount, RefundAmount = a.RefundAmount,
            Status = a.Status.ToString(), RejectionReason = a.RejectionReason,
            AppliedAt = a.AppliedAt, AllocatedAt = a.AllocatedAt, RefundedAt = a.RefundedAt
        };
    }
}
