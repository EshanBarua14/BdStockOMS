using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Reports;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class BrokerageReportService : IBrokerageReportService
{
    private readonly AppDbContext _context;

    // Standard commission rate used for estimation (0.5%)
    private const decimal StandardCommissionRate = 0.005m;

    public BrokerageReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderSummaryReportDto>> GetOrderSummaryAsync(int brokerageHouseId, ReportQueryDto query)
    {
        var bh = await _context.BrokerageHouses.FindAsync(brokerageHouseId);
        if (bh == null)
            return Result<OrderSummaryReportDto>.Failure("Brokerage house not found.");

        var (from, to) = ResolveDateRange(query);

        var orders = await _context.Orders
            .Where(o => o.BrokerageHouseId == brokerageHouseId &&
                        o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        return Result<OrderSummaryReportDto>.Success(new OrderSummaryReportDto
        {
            BrokerageHouseId   = brokerageHouseId,
            BrokerageHouseName = bh.Name,
            FromDate           = from,
            ToDate             = to,
            TotalOrders        = orders.Count,
            BuyOrders          = orders.Count(o => o.OrderType == OrderType.Buy),
            SellOrders         = orders.Count(o => o.OrderType == OrderType.Sell),
            PendingOrders      = orders.Count(o => o.Status == OrderStatus.Pending),
            ExecutedOrders     = orders.Count(o => o.Status == OrderStatus.Executed),
            CancelledOrders    = orders.Count(o => o.Status == OrderStatus.Cancelled),
            RejectedOrders     = orders.Count(o => o.Status == OrderStatus.Rejected),
            TotalOrderValue    = orders
                .Where(o => o.Status == OrderStatus.Executed)
                .Sum(o => o.Quantity * o.PriceAtOrder)
        });
    }

    public async Task<Result<List<TopInvestorDto>>> GetTopInvestorsAsync(int brokerageHouseId, ReportQueryDto query, int top = 10)
    {
        var bh = await _context.BrokerageHouses.FindAsync(brokerageHouseId);
        if (bh == null)
            return Result<List<TopInvestorDto>>.Failure("Brokerage house not found.");

        var (from, to) = ResolveDateRange(query);

        // Group executed orders by investor and sum traded value
        var topInvestors = await _context.Orders
            .Include(o => o.Investor)
            .Where(o => o.BrokerageHouseId == brokerageHouseId &&
                        o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => new { o.InvestorId, o.Investor.FullName, o.Investor.Email })
            .Select(g => new TopInvestorDto
            {
                InvestorId       = g.Key.InvestorId,
                InvestorName     = g.Key.FullName,
                Email            = g.Key.Email,
                TotalOrders      = g.Count(),
                ExecutedOrders   = g.Count(o => o.Status == OrderStatus.Executed),
                TotalTradedValue = g
                    .Where(o => o.Status == OrderStatus.Executed)
                    .Sum(o => o.Quantity * o.PriceAtOrder)
            })
            .OrderByDescending(i => i.TotalTradedValue)
            .Take(top)
            .ToListAsync();

        return Result<List<TopInvestorDto>>.Success(topInvestors);
    }

    public async Task<Result<CommissionReportDto>> GetCommissionReportAsync(int brokerageHouseId, ReportQueryDto query)
    {
        var bh = await _context.BrokerageHouses.FindAsync(brokerageHouseId);
        if (bh == null)
            return Result<CommissionReportDto>.Failure("Brokerage house not found.");

        var (from, to) = ResolveDateRange(query);

        var executedOrders = await _context.Orders
            .Where(o => o.BrokerageHouseId == brokerageHouseId &&
                        o.Status == OrderStatus.Executed &&
                        o.CreatedAt >= from && o.CreatedAt <= to)
            .ToListAsync();

        var totalTradedValue = executedOrders.Sum(o => o.Quantity * o.PriceAtOrder);

        return Result<CommissionReportDto>.Success(new CommissionReportDto
        {
            BrokerageHouseId    = brokerageHouseId,
            BrokerageHouseName  = bh.Name,
            FromDate            = from,
            ToDate              = to,
            TotalExecutedOrders = executedOrders.Count,
            TotalTradedValue    = totalTradedValue,
            EstimatedCommission = Math.Round(totalTradedValue * StandardCommissionRate, 2)
        });
    }

    public async Task<Result<FundRequestReportDto>> GetFundRequestReportAsync(int brokerageHouseId, ReportQueryDto query)
    {
        var bh = await _context.BrokerageHouses.FindAsync(brokerageHouseId);
        if (bh == null)
            return Result<FundRequestReportDto>.Failure("Brokerage house not found.");

        var (from, to) = ResolveDateRange(query);

        var requests = await _context.FundRequests
            .Where(f => f.BrokerageHouseId == brokerageHouseId &&
                        f.CreatedAt >= from && f.CreatedAt <= to)
            .ToListAsync();

        return Result<FundRequestReportDto>.Success(new FundRequestReportDto
        {
            BrokerageHouseId     = brokerageHouseId,
            BrokerageHouseName   = bh.Name,
            FromDate             = from,
            ToDate               = to,
            TotalRequests        = requests.Count,
            PendingRequests      = requests.Count(f => f.Status == FundRequestStatus.Pending),
            CompletedRequests    = requests.Count(f => f.Status == FundRequestStatus.Completed),
            RejectedRequests     = requests.Count(f => f.Status == FundRequestStatus.Rejected),
            TotalCompletedAmount = requests
                .Where(f => f.Status == FundRequestStatus.Completed)
                .Sum(f => f.Amount),
        });
    }

    // ── HELPER: Default to current month if no dates given ─────
    private static (DateTime from, DateTime to) ResolveDateRange(ReportQueryDto query)
    {
        var from = query.FromDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var to   = query.ToDate   ?? DateTime.UtcNow;
        return (from, to);
    }
}
