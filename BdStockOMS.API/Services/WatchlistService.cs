using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class WatchlistWithItems
{
    public int Id              { get; set; }
    public string Name         { get; set; } = string.Empty;
    public bool IsDefault      { get; set; }
    public DateTime CreatedAt  { get; set; }
    public List<WatchlistStockDto> Stocks { get; set; } = new();
}

public class WatchlistStockDto
{
    public int WatchlistItemId  { get; set; }
    public int StockId          { get; set; }
    public string TradingCode   { get; set; } = string.Empty;
    public string CompanyName   { get; set; } = string.Empty;
    public string Exchange      { get; set; } = string.Empty;
    public decimal LastTradePrice { get; set; }
    public decimal Change       { get; set; }
    public decimal ChangePercent { get; set; }
    public int SortOrder        { get; set; }
}

public interface IWatchlistService
{
    Task<Result<Watchlist>> CreateWatchlistAsync(int userId, string name);
    Task<Result> DeleteWatchlistAsync(int watchlistId, int userId);
    Task<Result> RenameWatchlistAsync(int watchlistId, int userId, string newName);
    Task<Result<WatchlistItem>> AddStockAsync(int watchlistId, int userId, int stockId);
    Task<Result> RemoveStockAsync(int watchlistId, int userId, int stockId);
    Task<Result> ReorderStocksAsync(int watchlistId, int userId,
                                    List<(int stockId, int sortOrder)> order);
    Task<List<WatchlistWithItems>> GetMyWatchlistsAsync(int userId);
    Task<Result<WatchlistWithItems>> GetWatchlistAsync(int watchlistId, int userId);
    Task EnsureDefaultWatchlistAsync(int userId);
}

public class WatchlistService : IWatchlistService
{
    private readonly AppDbContext _db;
    private const int MaxWatchlists    = 10;
    private const int MaxStocksPerList = 50;

    public WatchlistService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Watchlist>> CreateWatchlistAsync(int userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Watchlist>.Failure("Watchlist name is required.", "NAME_REQUIRED");

        if (name.Length > 50)
            return Result<Watchlist>.Failure(
                "Watchlist name cannot exceed 50 characters.", "NAME_TOO_LONG");

        var count = await _db.Watchlists.CountAsync(w => w.UserId == userId);
        if (count >= MaxWatchlists)
            return Result<Watchlist>.Failure(
                $"Cannot have more than {MaxWatchlists} watchlists.", "MAX_WATCHLISTS");

        var watchlist = new Watchlist
        {
            UserId    = userId,
            Name      = name.Trim(),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Watchlists.Add(watchlist);
        await _db.SaveChangesAsync();
        return Result<Watchlist>.Success(watchlist);
    }

    public async Task<Result> DeleteWatchlistAsync(int watchlistId, int userId)
    {
        var watchlist = await _db.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result.Failure("Watchlist not found.", "NOT_FOUND");

        if (watchlist.IsDefault)
            return Result.Failure("Cannot delete the default watchlist.", "CANNOT_DELETE_DEFAULT");

        _db.Watchlists.Remove(watchlist);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RenameWatchlistAsync(int watchlistId, int userId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure("Watchlist name is required.", "NAME_REQUIRED");

        var watchlist = await _db.Watchlists
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result.Failure("Watchlist not found.", "NOT_FOUND");

        watchlist.Name = newName.Trim();
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<WatchlistItem>> AddStockAsync(
        int watchlistId, int userId, int stockId)
    {
        var watchlist = await _db.Watchlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result<WatchlistItem>.Failure("Watchlist not found.", "NOT_FOUND");

        if (watchlist.Items.Count >= MaxStocksPerList)
            return Result<WatchlistItem>.Failure(
                $"Cannot add more than {MaxStocksPerList} stocks.", "MAX_STOCKS");

        var stockExists = await _db.Stocks.AnyAsync(s => s.Id == stockId && s.IsActive);
        if (!stockExists)
            return Result<WatchlistItem>.Failure("Stock not found.", "STOCK_NOT_FOUND");

        var alreadyAdded = watchlist.Items.Any(i => i.StockId == stockId);
        if (alreadyAdded)
            return Result<WatchlistItem>.Failure(
                "Stock already in watchlist.", "ALREADY_EXISTS");

        var item = new WatchlistItem
        {
            WatchlistId = watchlistId,
            StockId     = stockId,
            SortOrder   = watchlist.Items.Count,
            AddedAt     = DateTime.UtcNow
        };

        _db.WatchlistItems.Add(item);
        await _db.SaveChangesAsync();
        return Result<WatchlistItem>.Success(item);
    }

    public async Task<Result> RemoveStockAsync(int watchlistId, int userId, int stockId)
    {
        var watchlist = await _db.Watchlists
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result.Failure("Watchlist not found.", "NOT_FOUND");

        var item = await _db.WatchlistItems
            .FirstOrDefaultAsync(i => i.WatchlistId == watchlistId && i.StockId == stockId);

        if (item == null)
            return Result.Failure("Stock not in watchlist.", "NOT_FOUND");

        _db.WatchlistItems.Remove(item);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReorderStocksAsync(int watchlistId, int userId,
        List<(int stockId, int sortOrder)> order)
    {
        var watchlist = await _db.Watchlists
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result.Failure("Watchlist not found.", "NOT_FOUND");

        var items = await _db.WatchlistItems
            .Where(i => i.WatchlistId == watchlistId)
            .ToListAsync();

        foreach (var (stockId, sortOrder) in order)
        {
            var item = items.FirstOrDefault(i => i.StockId == stockId);
            if (item != null) item.SortOrder = sortOrder;
        }

        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<WatchlistWithItems>> GetMyWatchlistsAsync(int userId)
    {
        var watchlists = await _db.Watchlists
            .Include(w => w.Items)
            .ThenInclude(i => i.Stock)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync();

        return watchlists.Select(MapToDto).ToList();
    }

    public async Task<Result<WatchlistWithItems>> GetWatchlistAsync(
        int watchlistId, int userId)
    {
        var watchlist = await _db.Watchlists
            .Include(w => w.Items)
            .ThenInclude(i => i.Stock)
            .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

        if (watchlist == null)
            return Result<WatchlistWithItems>.Failure("Watchlist not found.", "NOT_FOUND");

        return Result<WatchlistWithItems>.Success(MapToDto(watchlist));
    }

    public async Task EnsureDefaultWatchlistAsync(int userId)
    {
        var hasDefault = await _db.Watchlists
            .AnyAsync(w => w.UserId == userId && w.IsDefault);

        if (!hasDefault)
        {
            _db.Watchlists.Add(new Watchlist
            {
                UserId    = userId,
                Name      = "My Watchlist",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }

    private static WatchlistWithItems MapToDto(Watchlist w) => new()
    {
        Id        = w.Id,
        Name      = w.Name,
        IsDefault = w.IsDefault,
        CreatedAt = w.CreatedAt,
        Stocks    = w.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => new WatchlistStockDto
            {
                WatchlistItemId = i.Id,
                StockId         = i.StockId,
                TradingCode     = i.Stock?.TradingCode ?? "",
                CompanyName     = i.Stock?.CompanyName ?? "",
                Exchange        = i.Stock?.Exchange ?? "",
                LastTradePrice  = i.Stock?.LastTradePrice ?? 0,
                Change          = i.Stock?.Change ?? 0,
                ChangePercent   = i.Stock?.ChangePercent ?? 0,
                SortOrder       = i.SortOrder
            }).ToList()
    };
}
