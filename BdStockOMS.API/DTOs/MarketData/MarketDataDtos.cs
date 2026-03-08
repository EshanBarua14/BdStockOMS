namespace BdStockOMS.API.DTOs.MarketData;

public class MarketDataResponseDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }
    public int Trades { get; set; }
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMarketDataDto
{
    public int StockId { get; set; }
    public string Exchange { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }
    public int Trades { get; set; }
    public DateTime Date { get; set; }
}

public class BulkMarketDataDto
{
    public List<CreateMarketDataDto> Items { get; set; } = new();
}

public class MarketDataQueryDto
{
    public int? StockId { get; set; }
    public string? Exchange { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
