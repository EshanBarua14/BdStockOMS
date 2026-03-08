using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.Portfolio;

namespace BdStockOMS.API.Services;

public interface IPortfolioPnlService
{
    // Full portfolio summary with all holdings and total P&L
    Task<Result<PortfolioSummaryDto>> GetPortfolioSummaryAsync(int investorId);

    // P&L for one specific stock holding
    Task<Result<PortfolioHoldingDto>> GetHoldingAsync(int investorId, int stockId);

    // Day-by-day portfolio value for the last N days using MarketData
    Task<Result<List<PortfolioHistoryItemDto>>> GetPortfolioHistoryAsync(int investorId, int days = 30);
}
