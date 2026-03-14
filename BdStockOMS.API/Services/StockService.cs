// Services/StockService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Stock;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IStockService
{
    Task<List<StockResponseDto>> GetAllStocksAsync();
    Task<StockResponseDto?> GetStockByIdAsync(int id);
    Task<List<StockResponseDto>> SearchStocksAsync(string query);
    Task<(StockResponseDto? Stock, string? Error)> CreateStockAsync(CreateStockDto dto);
    Task<(StockResponseDto? Stock, string? Error)> UpdateStockAsync(int id, UpdateStockDto dto);
    Task<bool> DeactivateStockAsync(int id);
}

public class StockService : IStockService
{
    private readonly AppDbContext _db;

    public StockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockResponseDto>> GetAllStocksAsync()
    {
        return await _db.Stocks
            .Where(s => s.IsActive)
            .OrderBy(s => s.TradingCode)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<StockResponseDto?> GetStockByIdAsync(int id)
    {
        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        return stock == null ? null : MapToDto(stock);
    }

    public async Task<List<StockResponseDto>> SearchStocksAsync(string query)
    {
        var lower = query.ToLower();
        return await _db.Stocks
            .Where(s => s.IsActive &&
                (s.TradingCode.ToLower().Contains(lower) ||
                 s.CompanyName.ToLower().Contains(lower)))
            .OrderBy(s => s.TradingCode)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<(StockResponseDto? Stock, string? Error)> CreateStockAsync(CreateStockDto dto)
    {
        // TradingCode must be unique per exchange
        var exists = await _db.Stocks.AnyAsync(s =>
            s.TradingCode == dto.TradingCode &&
            s.Exchange == dto.Exchange);

        if (exists)
            return (null, $"Stock '{dto.TradingCode}' already exists on {dto.Exchange}.");

        // Validate exchange value
        if (dto.Exchange != "DSE" && dto.Exchange != "CSE")
            return (null, "Exchange must be 'DSE' or 'CSE'.");

        var stock = new Stock
        {
            TradingCode = dto.TradingCode.ToUpper(),
            CompanyName = dto.CompanyName,
            Exchange = dto.Exchange.ToUpper(),
            LastTradePrice = dto.LastTradePrice,
            HighPrice = dto.HighPrice,
            LowPrice = dto.LowPrice,
            ClosePrice = dto.ClosePrice,
            Change = dto.Change,
            ChangePercent = dto.ChangePercent,
            Volume = dto.Volume,
            ValueInMillionTaka = dto.ValueInMillionTaka,
            LastUpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Stocks.Add(stock);
        await _db.SaveChangesAsync();
        return (MapToDto(stock), null);
    }

    public async Task<(StockResponseDto? Stock, string? Error)> UpdateStockAsync(
        int id, UpdateStockDto dto)
    {
        var stock = await _db.Stocks.FindAsync(id);
        if (stock == null || !stock.IsActive)
            return (null, "Stock not found.");

        stock.LastTradePrice = dto.LastTradePrice;
        stock.HighPrice = dto.HighPrice;
        stock.LowPrice = dto.LowPrice;
        stock.ClosePrice = dto.ClosePrice;
        stock.Change = dto.Change;
        stock.ChangePercent = dto.ChangePercent;
        stock.Volume = dto.Volume;
        stock.ValueInMillionTaka = dto.ValueInMillionTaka;
        stock.LastUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (MapToDto(stock), null);
    }

    public async Task<bool> DeactivateStockAsync(int id)
    {
        var stock = await _db.Stocks.FindAsync(id);
        if (stock == null) return false;

        stock.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static StockResponseDto MapToDto(Stock s) => new()
    {
        Id = s.Id,
        TradingCode = s.TradingCode,
        CompanyName = s.CompanyName,
        Exchange = s.Exchange,
        LastTradePrice = s.LastTradePrice,
        HighPrice = s.HighPrice,
        LowPrice = s.LowPrice,
        ClosePrice = s.ClosePrice,
        Change = s.Change,
        ChangePercent = s.ChangePercent,
        Volume = s.Volume,
        ValueInMillionTaka = s.ValueInMillionTaka,
        LastUpdatedAt = s.LastUpdatedAt,
        IsActive = s.IsActive,
        Category = s.Category.ToString(),
        CircuitBreakerHigh = s.CircuitBreakerHigh,
        CircuitBreakerLow = s.CircuitBreakerLow,
        BoardLotSize = s.BoardLotSize
    };
}
