using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class MarketDataService : IMarketDataService
{
    private readonly AppDbContext _context;

    public MarketDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<MarketDataResponseDto>>> GetAllAsync(MarketDataQueryDto query)
    {
        var q = _context.MarketData
            .Include(m => m.Stock)
            .AsQueryable();

        if (query.StockId.HasValue)
            q = q.Where(m => m.StockId == query.StockId.Value);

        if (!string.IsNullOrWhiteSpace(query.Exchange))
            q = q.Where(m => m.Exchange == query.Exchange);

        if (query.FromDate.HasValue)
            q = q.Where(m => m.Date >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(m => m.Date <= query.ToDate.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(m => m.Date)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => ToDto(m))
            .ToListAsync();

        return Result<PagedResult<MarketDataResponseDto>>.Success(new PagedResult<MarketDataResponseDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<MarketDataResponseDto>> GetByIdAsync(int id)
    {
        var m = await _context.MarketData
            .Include(x => x.Stock)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m == null)
            return Result<MarketDataResponseDto>.Failure("Market data record not found.");

        return Result<MarketDataResponseDto>.Success(ToDto(m));
    }

    public async Task<Result<List<MarketDataResponseDto>>> GetByStockAsync(int stockId, string exchange, int days = 30)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);
        var items = await _context.MarketData
            .Include(m => m.Stock)
            .Where(m => m.StockId == stockId && m.Exchange == exchange && m.Date >= from)
            .OrderByDescending(m => m.Date)
            .Select(m => ToDto(m))
            .ToListAsync();

        return Result<List<MarketDataResponseDto>>.Success(items);
    }

    public async Task<Result<MarketDataResponseDto>> CreateAsync(CreateMarketDataDto dto)
    {
        var stock = await _context.Stocks.FindAsync(dto.StockId);
        if (stock == null)
            return Result<MarketDataResponseDto>.Failure("Stock not found.");

        var exists = await _context.MarketData.AnyAsync(m =>
            m.StockId == dto.StockId &&
            m.Exchange == dto.Exchange &&
            m.Date == dto.Date.Date);

        if (exists)
            return Result<MarketDataResponseDto>.Failure("Market data for this stock/exchange/date already exists.");

        var entity = new Models.MarketData
        {
            StockId = dto.StockId,
            Exchange = dto.Exchange,
            Open = dto.Open,
            High = dto.High,
            Low = dto.Low,
            Close = dto.Close,
            Volume = dto.Volume,
            ValueInMillionTaka = dto.ValueInMillionTaka,
            Trades = dto.Trades,
            Date = dto.Date.Date,
            CreatedAt = DateTime.UtcNow
        };

        _context.MarketData.Add(entity);
        await _context.SaveChangesAsync();

        entity.Stock = stock;
        return Result<MarketDataResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<BulkMarketDataResultDto>> BulkCreateAsync(BulkMarketDataDto dto)
    {
        var result = new BulkMarketDataResultDto();

        foreach (var item in dto.Items)
        {
            var stock = await _context.Stocks.FindAsync(item.StockId);
            if (stock == null)
            {
                result.Errors.Add($"StockId {item.StockId} not found.");
                result.Skipped++;
                continue;
            }

            var exists = await _context.MarketData.AnyAsync(m =>
                m.StockId == item.StockId &&
                m.Exchange == item.Exchange &&
                m.Date == item.Date.Date);

            if (exists)
            {
                result.Skipped++;
                continue;
            }

            _context.MarketData.Add(new Models.MarketData
            {
                StockId = item.StockId,
                Exchange = item.Exchange,
                Open = item.Open,
                High = item.High,
                Low = item.Low,
                Close = item.Close,
                Volume = item.Volume,
                ValueInMillionTaka = item.ValueInMillionTaka,
                Trades = item.Trades,
                Date = item.Date.Date,
                CreatedAt = DateTime.UtcNow
            });

            result.Created++;
        }

        await _context.SaveChangesAsync();
        return Result<BulkMarketDataResultDto>.Success(result);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var entity = await _context.MarketData.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("Market data record not found.");

        _context.MarketData.Remove(entity);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static MarketDataResponseDto ToDto(Models.MarketData m) => new()
    {
        Id = m.Id,
        StockId = m.StockId,
        TradingCode = m.Stock?.TradingCode ?? string.Empty,
        CompanyName = m.Stock?.CompanyName ?? string.Empty,
        Exchange = m.Exchange,
        Open = m.Open,
        High = m.High,
        Low = m.Low,
        Close = m.Close,
        Volume = m.Volume,
        ValueInMillionTaka = m.ValueInMillionTaka,
        Trades = m.Trades,
        Date = m.Date,
        CreatedAt = m.CreatedAt
    };
}
