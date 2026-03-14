// DTOs/Stock/StockResponseDto.cs
namespace BdStockOMS.API.DTOs.Stock;

// What we send BACK to the client
public class StockResponseDto
{
    public int Id { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal LastTradePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = "A";
    public decimal CircuitBreakerHigh { get; set; }
    public decimal CircuitBreakerLow { get; set; }
    public int BoardLotSize { get; set; } = 1;
}
