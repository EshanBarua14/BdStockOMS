namespace BdStockOMS.API.DTOs.Portfolio;

// One stock holding with P&L calculated
public class PortfolioHoldingDto
{
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // How many shares held
    public int Quantity { get; set; }

    // What we paid on average per share
    public decimal AverageBuyPrice { get; set; }

    // Current market price (from Stock.LastTradePrice)
    public decimal CurrentPrice { get; set; }

    // Quantity × AverageBuyPrice
    public decimal CostBasis { get; set; }

    // Quantity × CurrentPrice
    public decimal CurrentValue { get; set; }

    // CurrentValue - CostBasis
    public decimal UnrealizedPnL { get; set; }

    // (UnrealizedPnL / CostBasis) × 100
    public decimal PnLPercent { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}

// Full portfolio summary for one investor
public class PortfolioSummaryDto
{
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public string BrokerageHouseName { get; set; } = string.Empty;

    // Cash available in account
    public decimal CashBalance { get; set; }

    // Total cost of all holdings
    public decimal TotalCostBasis { get; set; }

    // Total current market value of all holdings
    public decimal TotalCurrentValue { get; set; }

    // Total unrealized profit or loss
    public decimal TotalUnrealizedPnL { get; set; }

    // Overall P&L percentage
    public decimal TotalPnLPercent { get; set; }

    // Total portfolio worth = cash + stock value
    public decimal TotalPortfolioValue { get; set; }

    // Individual stock holdings
    public List<PortfolioHoldingDto> Holdings { get; set; } = new();
}

// One day's portfolio value snapshot (for historical chart)
public class PortfolioHistoryItemDto
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal PnL { get; set; }
}
