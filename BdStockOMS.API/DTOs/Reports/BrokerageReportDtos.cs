namespace BdStockOMS.API.DTOs.Reports;

// Summary of orders placed in a period
public class OrderSummaryReportDto
{
    public int BrokerageHouseId { get; set; }
    public string BrokerageHouseName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalOrders { get; set; }
    public int BuyOrders { get; set; }
    public int SellOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ExecutedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal TotalOrderValue { get; set; }
}

// One investor's trading activity
public class TopInvestorDto
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int ExecutedOrders { get; set; }
    public decimal TotalTradedValue { get; set; }
}

// Commission earned in a period
public class CommissionReportDto
{
    public int BrokerageHouseId { get; set; }
    public string BrokerageHouseName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalExecutedOrders { get; set; }
    public decimal TotalTradedValue { get; set; }

    // Estimated commission = TotalTradedValue × standard rate
    public decimal EstimatedCommission { get; set; }
}

// Fund request deposit/withdrawal summary
public class FundRequestReportDto
{
    public int BrokerageHouseId { get; set; }
    public string BrokerageHouseName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public decimal TotalCompletedAmount { get; set; }
    
}

// Query parameters shared by all reports
public class ReportQueryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
