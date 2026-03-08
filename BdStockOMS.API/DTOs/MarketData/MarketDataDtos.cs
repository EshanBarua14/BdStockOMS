using System.ComponentModel.DataAnnotations;

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
    [Range(1, int.MaxValue, ErrorMessage = "StockId must be a positive integer.")]
    public int StockId { get; set; }

    [Required(ErrorMessage = "Exchange is required.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Exchange must be between 2 and 10 characters.")]
    public string Exchange { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Open price must be greater than zero.")]
    public decimal Open { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "High price must be greater than zero.")]
    public decimal High { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Low price must be greater than zero.")]
    public decimal Low { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Close price must be greater than zero.")]
    public decimal Close { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "Volume cannot be negative.")]
    public long Volume { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Value cannot be negative.")]
    public decimal ValueInMillionTaka { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Trades cannot be negative.")]
    public int Trades { get; set; }

    public DateTime Date { get; set; }
}

public class BulkMarketDataDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one item is required.")]
    public List<CreateMarketDataDto> Items { get; set; } = new();
}

public class MarketDataQueryDto
{
    public int? StockId { get; set; }
    public string? Exchange { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
