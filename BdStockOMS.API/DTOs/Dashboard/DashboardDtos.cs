namespace BdStockOMS.API.DTOs.Dashboard;

// Counts of users broken down by role
public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalInvestors { get; set; }
    public int TotalTraders { get; set; }
    public int TotalCCDs { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedUsers { get; set; }
}

// Order activity summary
public class OrderStatsDto
{
    public int TotalOrdersToday { get; set; }
    public int TotalOrdersThisMonth { get; set; }
    public int PendingOrders { get; set; }
    public int ExecutedOrdersToday { get; set; }
    public decimal TotalTradedValueToday { get; set; }
}

// Fund request summary
public class FundRequestStatsDto
{
    public int PendingApproval { get; set; }
    public int CompletedToday { get; set; }
    public decimal TotalDepositedToday { get; set; }
}

// System-wide health stats
public class SystemStatsDto
{
    public int TotalBrokerageHouses { get; set; }
    public int ActiveBrokerageHouses { get; set; }
    public int TotalStocks { get; set; }
    public int ActiveStocks { get; set; }
    public int TotalWatchlists { get; set; }
    public int UnreadNotifications { get; set; }
}

// One item in the recent activity feed
public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty;   // "Order", "FundRequest", "User"
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

// The full dashboard response — everything in one call
public class AdminDashboardDto
{
    public UserStatsDto Users { get; set; } = new();
    public OrderStatsDto Orders { get; set; } = new();
    public FundRequestStatsDto FundRequests { get; set; } = new();
    public SystemStatsDto System { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
