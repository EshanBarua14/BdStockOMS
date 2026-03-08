namespace BdStockOMS.API.Models;

public class Watchlist
{
    public int Id              { get; set; }
    public int UserId          { get; set; }
    public string Name         { get; set; } = string.Empty;
    public bool IsDefault      { get; set; } = false;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User                        { get; set; } = null!;
    public ICollection<WatchlistItem> Items { get; set; } = new List<WatchlistItem>();
}

public class WatchlistItem
{
    public int Id          { get; set; }
    public int WatchlistId { get; set; }
    public int StockId     { get; set; }
    public int SortOrder   { get; set; } = 0;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Watchlist Watchlist { get; set; } = null!;
    public Stock Stock         { get; set; } = null!;
}
