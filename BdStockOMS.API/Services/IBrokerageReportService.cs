using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.Reports;

namespace BdStockOMS.API.Services;

public interface IBrokerageReportService
{
    // Order counts and values for a brokerage house in a date range
    Task<Result<OrderSummaryReportDto>> GetOrderSummaryAsync(int brokerageHouseId, ReportQueryDto query);

    // Top investors by trading volume for a brokerage house
    Task<Result<List<TopInvestorDto>>> GetTopInvestorsAsync(int brokerageHouseId, ReportQueryDto query, int top = 10);

    // Commission earned by a brokerage house in a date range
    Task<Result<CommissionReportDto>> GetCommissionReportAsync(int brokerageHouseId, ReportQueryDto query);

    // Fund request deposit/withdrawal summary for a brokerage house
    Task<Result<FundRequestReportDto>> GetFundRequestReportAsync(int brokerageHouseId, ReportQueryDto query);
}
