// DTOs/Stock/UpdateStockDto.cs
namespace BdStockOMS.API.DTOs.Stock;

// CCD/Admin updates price data — TradingCode and Exchange never change
public class UpdateStockDto
{
    public decimal LastTradePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }
}
