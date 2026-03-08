using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.Dashboard;

namespace BdStockOMS.API.Services;

public interface IAdminDashboardService
{
    // Full dashboard — all stats in one call
    Task<Result<AdminDashboardDto>> GetDashboardAsync();

    // Individual stat sections (for partial refresh)
    Task<Result<UserStatsDto>> GetUserStatsAsync();
    Task<Result<OrderStatsDto>> GetOrderStatsAsync();
    Task<Result<FundRequestStatsDto>> GetFundRequestStatsAsync();
    Task<Result<SystemStatsDto>> GetSystemStatsAsync();
    Task<Result<List<RecentActivityDto>>> GetRecentActivityAsync(int count = 10);
}
