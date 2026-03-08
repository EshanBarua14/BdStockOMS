namespace BdStockOMS.API.Models;

public class MarketData
{
    public int Id              { get; set; }
    public int StockId         { get; set; }
    public string Exchange     { get; set; } = string.Empty;
    public decimal Open        { get; set; }
    public decimal High        { get; set; }
    public decimal Low         { get; set; }
    public decimal Close       { get; set; }
    public long Volume         { get; set; }
    public decimal ValueInMillionTaka { get; set; }
    public int Trades          { get; set; }
    public DateTime Date       { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Stock Stock { get; set; } = null!;
}
