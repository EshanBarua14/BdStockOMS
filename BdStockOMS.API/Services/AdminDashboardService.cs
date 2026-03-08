using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Dashboard;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _context;

    public AdminDashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AdminDashboardDto>> GetDashboardAsync()
    {
        var users       = await GetUserStatsAsync();
        var orders      = await GetOrderStatsAsync();
        var fundReqs    = await GetFundRequestStatsAsync();
        var system      = await GetSystemStatsAsync();
        var activity    = await GetRecentActivityAsync(10);

        return Result<AdminDashboardDto>.Success(new AdminDashboardDto
        {
            Users          = users.Value!,
            Orders         = orders.Value!,
            FundRequests   = fundReqs.Value!,
            System         = system.Value!,
            RecentActivity = activity.Value!,
            GeneratedAt    = DateTime.UtcNow
        });
    }

    public async Task<Result<UserStatsDto>> GetUserStatsAsync()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .ToListAsync();

        return Result<UserStatsDto>.Success(new UserStatsDto
        {
            TotalUsers     = users.Count,
            TotalInvestors = users.Count(u => u.Role.Name == "Investor"),
            TotalTraders   = users.Count(u => u.Role.Name == "Trader"),
            TotalCCDs      = users.Count(u => u.Role.Name == "CCD"),
            TotalAdmins    = users.Count(u => u.Role.Name is "Admin" or "SuperAdmin"),
            ActiveUsers    = users.Count(u => u.IsActive),
            LockedUsers    = users.Count(u => u.IsLocked)
        });
    }

    public async Task<Result<OrderStatsDto>> GetOrderStatsAsync()
    {
        var today     = DateTime.UtcNow.Date;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var allOrders = await _context.Orders.ToListAsync();

        var todayOrders    = allOrders.Where(o => o.CreatedAt.Date == today).ToList();
        var monthOrders    = allOrders.Where(o => o.CreatedAt >= monthStart).ToList();
        var executedToday  = todayOrders.Where(o => o.Status == OrderStatus.Executed).ToList();

        return Result<OrderStatsDto>.Success(new OrderStatsDto
        {
            TotalOrdersToday      = todayOrders.Count,
            TotalOrdersThisMonth  = monthOrders.Count,
            PendingOrders         = allOrders.Count(o => o.Status == OrderStatus.Pending),
            ExecutedOrdersToday   = executedToday.Count,
            TotalTradedValueToday = executedToday.Sum(o => o.Quantity * o.PriceAtOrder)
        });
    }

    public async Task<Result<FundRequestStatsDto>> GetFundRequestStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var all   = await _context.FundRequests.ToListAsync();

        return Result<FundRequestStatsDto>.Success(new FundRequestStatsDto
        {
            PendingApproval    = all.Count(f => f.Status == FundRequestStatus.Pending),
            CompletedToday     = all.Count(f => f.Status == FundRequestStatus.Completed && f.CreatedAt.Date == today),
            TotalDepositedToday = all
                .Where(f => f.Status == FundRequestStatus.Completed && f.CreatedAt.Date == today)
                .Sum(f => f.Amount)
        });
    }

    public async Task<Result<SystemStatsDto>> GetSystemStatsAsync()
    {
        var brokerages     = await _context.BrokerageHouses.ToListAsync();
        var stocks         = await _context.Stocks.ToListAsync();
        var watchlists     = await _context.Watchlists.CountAsync();
        var notifications  = await _context.Notifications.CountAsync(n => !n.IsRead);

        return Result<SystemStatsDto>.Success(new SystemStatsDto
        {
            TotalBrokerageHouses  = brokerages.Count,
            ActiveBrokerageHouses = brokerages.Count(b => b.IsActive),
            TotalStocks           = stocks.Count,
            ActiveStocks          = stocks.Count(s => s.IsActive),
            TotalWatchlists       = watchlists,
            UnreadNotifications   = notifications
        });
    }

    public async Task<Result<List<RecentActivityDto>>> GetRecentActivityAsync(int count = 10)
    {
        var activity = new List<RecentActivityDto>();

        // Recent orders
        var recentOrders = await _context.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .OrderByDescending(o => o.CreatedAt)
            .Take(count)
            .ToListAsync();

        foreach (var o in recentOrders)
        {
            activity.Add(new RecentActivityDto
            {
                Type        = "Order",
                Description = $"{o.OrderType} {o.Quantity} {o.Stock?.TradingCode} by {o.Investor?.FullName} — {o.Status}",
                Timestamp   = o.CreatedAt
            });
        }

        // Recent fund requests
        var recentFunds = await _context.FundRequests
            .Include(f => f.Investor)
            .OrderByDescending(f => f.CreatedAt)
            .Take(count)
            .ToListAsync();

        foreach (var f in recentFunds)
        {
            activity.Add(new RecentActivityDto
            {
                Type        = "FundRequest",
                Description = $"Fund request of {f.Amount:N0} BDT by {f.Investor?.FullName} — {f.Status}",
                Timestamp   = f.CreatedAt
            });
        }

        // Sort all activity by most recent and take top N
        var sorted = activity
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();

        return Result<List<RecentActivityDto>>.Success(sorted);
    }
}
