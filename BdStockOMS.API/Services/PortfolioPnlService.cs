using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Portfolio;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class PortfolioPnlService : IPortfolioPnlService
{
    private readonly AppDbContext _context;

    public PortfolioPnlService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PortfolioSummaryDto>> GetPortfolioSummaryAsync(int investorId)
    {
        // Load the investor with their brokerage house
        var investor = await _context.Users
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Id == investorId);

        if (investor == null)
            return Result<PortfolioSummaryDto>.Failure("Investor not found.");

        // Load all portfolio holdings for this investor with stock info
        var holdings = await _context.Portfolios
            .Include(p => p.Stock)
            .Where(p => p.InvestorId == investorId && p.Quantity > 0)
            .ToListAsync();

        // Calculate P&L for each holding
        var holdingDtos = holdings.Select(p => CalculateHolding(p)).ToList();

        // Sum up totals
        var totalCost    = holdingDtos.Sum(h => h.CostBasis);
        var totalValue   = holdingDtos.Sum(h => h.CurrentValue);
        var totalPnL     = totalValue - totalCost;
        var totalPnLPct  = totalCost > 0 ? (totalPnL / totalCost) * 100 : 0;

        return Result<PortfolioSummaryDto>.Success(new PortfolioSummaryDto
        {
            InvestorId          = investorId,
            InvestorName        = investor.FullName,
            BrokerageHouseName  = investor.BrokerageHouse?.Name ?? string.Empty,
            CashBalance         = investor.CashBalance,
            TotalCostBasis      = totalCost,
            TotalCurrentValue   = totalValue,
            TotalUnrealizedPnL  = totalPnL,
            TotalPnLPercent     = Math.Round(totalPnLPct, 2),
            TotalPortfolioValue = investor.CashBalance + totalValue,
            Holdings            = holdingDtos
        });
    }

    public async Task<Result<PortfolioHoldingDto>> GetHoldingAsync(int investorId, int stockId)
    {
        var portfolio = await _context.Portfolios
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.InvestorId == investorId && p.StockId == stockId);

        if (portfolio == null)
            return Result<PortfolioHoldingDto>.Failure("Portfolio holding not found.");

        return Result<PortfolioHoldingDto>.Success(CalculateHolding(portfolio));
    }

    public async Task<Result<List<PortfolioHistoryItemDto>>> GetPortfolioHistoryAsync(int investorId, int days = 30)
    {
        var investor = await _context.Users.FindAsync(investorId);
        if (investor == null)
            return Result<List<PortfolioHistoryItemDto>>.Failure("Investor not found.");

        // Get current holdings (quantity and average buy price)
        var holdings = await _context.Portfolios
            .Include(p => p.Stock)
            .Where(p => p.InvestorId == investorId && p.Quantity > 0)
            .ToListAsync();

        if (!holdings.Any())
            return Result<List<PortfolioHistoryItemDto>>.Success(new List<PortfolioHistoryItemDto>());

        var stockIds = holdings.Select(h => h.StockId).ToList();
        var fromDate = DateTime.UtcNow.Date.AddDays(-days);

        // Get historical close prices for all held stocks
        var marketData = await _context.MarketData
            .Where(m => stockIds.Contains(m.StockId) && m.Date >= fromDate)
            .ToListAsync();

        // Group market data by date
        var dates = marketData.Select(m => m.Date).Distinct().OrderBy(d => d).ToList();

        var history = new List<PortfolioHistoryItemDto>();

        foreach (var date in dates)
        {
            decimal dayValue = 0;
            decimal dayCost  = 0;

            foreach (var holding in holdings)
            {
                // Find the close price for this stock on this date
                var md = marketData.FirstOrDefault(m =>
                    m.StockId == holding.StockId && m.Date == date);

                // Use close price if available, else fall back to current price
                var price = md?.Close ?? holding.Stock.LastTradePrice;

                dayValue += holding.Quantity * price;
                dayCost  += holding.Quantity * holding.AverageBuyPrice;
            }

            history.Add(new PortfolioHistoryItemDto
            {
                Date           = date,
                PortfolioValue = Math.Round(dayValue, 2),
                PnL            = Math.Round(dayValue - dayCost, 2)
            });
        }

        return Result<List<PortfolioHistoryItemDto>>.Success(history);
    }

    // ── HELPER: Calculate P&L for one holding ─────
    private static PortfolioHoldingDto CalculateHolding(Models.Portfolio p)
    {
        var currentPrice = p.Stock.LastTradePrice;
        var costBasis    = p.Quantity * p.AverageBuyPrice;
        var currentValue = p.Quantity * currentPrice;
        var pnl          = currentValue - costBasis;
        var pnlPct       = costBasis > 0 ? (pnl / costBasis) * 100 : 0;

        return new PortfolioHoldingDto
        {
            PortfolioId     = p.Id,
            StockId         = p.StockId,
            TradingCode     = p.Stock.TradingCode,
            CompanyName     = p.Stock.CompanyName,
            Exchange        = p.Stock.Exchange,
            Category        = p.Stock.Category.ToString(),
            Quantity        = p.Quantity,
            AverageBuyPrice = p.AverageBuyPrice,
            CurrentPrice    = currentPrice,
            CostBasis       = Math.Round(costBasis, 2),
            CurrentValue    = Math.Round(currentValue, 2),
            UnrealizedPnL   = Math.Round(pnl, 2),
            PnLPercent      = Math.Round(pnlPct, 2),
            LastUpdatedAt   = p.LastUpdatedAt
        };
    }
}
