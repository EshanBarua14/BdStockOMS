// DTOs/Order/PortfolioResponseDto.cs
namespace BdStockOMS.API.DTOs.Order;

public class PortfolioResponseDto
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public int StockId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentMarketPrice { get; set; }
    public decimal TotalInvestment { get; set; }  // Quantity × AverageBuyPrice
    public decimal CurrentValue { get; set; }     // Quantity × CurrentMarketPrice
    public decimal ProfitLoss { get; set; }       // CurrentValue - TotalInvestment
    public decimal ProfitLossPercent { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
