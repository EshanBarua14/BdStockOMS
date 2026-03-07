// DTOs/Stock/CreateStockDto.cs
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.Stock;

public class CreateStockDto
{
    [Required]
    [MaxLength(20)]
    public string TradingCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Exchange { get; set; } = string.Empty; // "DSE" or "CSE"

    public decimal LastTradePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }
}
