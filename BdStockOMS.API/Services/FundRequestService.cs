using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IFundRequestService
{
    Task<Result<FundRequest>> CreateRequestAsync(
        int investorId, decimal amount, PaymentMethod method,
        string? referenceNumber, string? notes, int brokerageHouseId);
    Task<Result> ApproveByTraderAsync(int requestId, int traderId, string? notes);
    Task<Result> ApproveByCCDAsync(int requestId, int ccdUserId);
    Task<Result> RejectAsync(int requestId, int rejectedByUserId, string reason);
    Task<Result> CompleteAsync(int requestId, int completedByUserId);
    Task<PagedResult<FundRequest>> GetRequestsAsync(
        int brokerageHouseId, int page, int pageSize,
        FundRequestStatus? status, int? investorId);
    Task<PagedResult<FundRequest>> GetMyRequestsAsync(int investorId, int page, int pageSize);
}

public class FundRequestService : IFundRequestService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public FundRequestService(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    public async Task<Result<FundRequest>> CreateRequestAsync(
        int investorId, decimal amount, PaymentMethod method,
        string? referenceNumber, string? notes, int brokerageHouseId)
    {
        if (amount <= 0)
            return Result<FundRequest>.Failure(
                "Amount must be greater than zero.", "INVALID_AMOUNT");

        if (amount > 10_000_000m)
            return Result<FundRequest>.Failure(
                "Single request cannot exceed 1 crore BDT.", "AMOUNT_EXCEEDS_LIMIT");

        // Check for pending requests
        var hasPending = await _db.FundRequests
            .AnyAsync(f => f.InvestorId == investorId &&
                           f.Status == FundRequestStatus.Pending);
        if (hasPending)
            return Result<FundRequest>.Failure(
                "You already have a pending fund request.", "PENDING_REQUEST_EXISTS");

        var request = new FundRequest
        {
            InvestorId       = investorId,
            BrokerageHouseId = brokerageHouseId,
            Amount           = amount,
            PaymentMethod    = method,
            ReferenceNumber  = referenceNumber,
            Notes            = notes,
            Status           = FundRequestStatus.Pending,
            CreatedAt        = DateTime.UtcNow
        };

        _db.FundRequests.Add(request);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(investorId, "FUND_REQUEST_CREATED", "FundRequest",
            request.Id, null, $"Amount: {amount} BDT via {method}", null);

        return Result<FundRequest>.Success(request);
    }

    public async Task<Result> ApproveByTraderAsync(int requestId, int traderId, string? notes)
    {
        var request = await _db.FundRequests.FindAsync(requestId);
        if (request == null)
            return Result.Failure("Fund request not found.", "NOT_FOUND");

        if (request.Status != FundRequestStatus.Pending)
            return Result.Failure(
                $"Request is already {request.Status}.", "INVALID_STATUS");

        request.Status    = FundRequestStatus.ApprovedByTrader;
        request.TraderId  = traderId;
        request.Notes     = notes ?? request.Notes;
        request.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(traderId, "FUND_REQUEST_TRADER_APPROVED", "FundRequest",
            requestId, null, $"Trader {traderId} approved request {requestId}", null);

        return Result.Success();
    }

    public async Task<Result> ApproveByCCDAsync(int requestId, int ccdUserId)
    {
        var request = await _db.FundRequests.FindAsync(requestId);
        if (request == null)
            return Result.Failure("Fund request not found.", "NOT_FOUND");

        if (request.Status != FundRequestStatus.ApprovedByTrader)
            return Result.Failure(
                "Request must be approved by Trader first.", "INVALID_STATUS");

        request.Status    = FundRequestStatus.ApprovedByCCD;
        request.CCDUserId = ccdUserId;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(ccdUserId, "FUND_REQUEST_CCD_APPROVED", "FundRequest",
            requestId, null, $"CCD {ccdUserId} approved request {requestId}", null);

        return Result.Success();
    }

    public async Task<Result> RejectAsync(int requestId, int rejectedByUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Rejection reason is required.", "REASON_REQUIRED");

        var request = await _db.FundRequests.FindAsync(requestId);
        if (request == null)
            return Result.Failure("Fund request not found.", "NOT_FOUND");

        if (request.Status == FundRequestStatus.Completed ||
            request.Status == FundRequestStatus.Rejected)
            return Result.Failure(
                $"Cannot reject a {request.Status} request.", "INVALID_STATUS");

        request.Status          = FundRequestStatus.Rejected;
        request.RejectionReason = reason;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(rejectedByUserId, "FUND_REQUEST_REJECTED", "FundRequest",
            requestId, null, $"Reason: {reason}", null);

        return Result.Success();
    }

    public async Task<Result> CompleteAsync(int requestId, int completedByUserId)
    {
        var request = await _db.FundRequests
            .Include(f => f.Investor)
            .FirstOrDefaultAsync(f => f.Id == requestId);

        if (request == null)
            return Result.Failure("Fund request not found.", "NOT_FOUND");

        if (request.Status != FundRequestStatus.ApprovedByCCD)
            return Result.Failure(
                "Request must be approved by CCD before completion.", "INVALID_STATUS");

        // Credit investor's cash balance
        request.Investor.CashBalance += request.Amount;
        request.Status               = FundRequestStatus.Completed;
        request.CompletedAt          = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(completedByUserId, "FUND_REQUEST_COMPLETED", "FundRequest",
            requestId, null,
            $"Amount {request.Amount} BDT credited to investor {request.InvestorId}", null);

        return Result.Success();
    }

    public async Task<PagedResult<FundRequest>> GetRequestsAsync(
        int brokerageHouseId, int page, int pageSize,
        FundRequestStatus? status, int? investorId)
    {
        var query = _db.FundRequests
            .Where(f => f.BrokerageHouseId == brokerageHouseId);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        if (investorId.HasValue)
            query = query.Where(f => f.InvestorId == investorId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<FundRequest>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<FundRequest>> GetMyRequestsAsync(
        int investorId, int page, int pageSize)
    {
        var query = _db.FundRequests.Where(f => f.InvestorId == investorId);
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<FundRequest>.Create(items, total, page, pageSize);
    }
}
