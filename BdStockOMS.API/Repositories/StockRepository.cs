using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Repositories;

public class StockRepository : BaseRepository<Stock>, IStockRepository
{
    public StockRepository(AppDbContext db) : base(db) { }

    public async Task<Stock?> GetByTradingCodeAsync(string tradingCode, string exchange) =>
        await _db.Stocks.FirstOrDefaultAsync(s =>
            s.TradingCode == tradingCode && s.Exchange == exchange);

    public async Task<(IEnumerable<Stock> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? exchange = null,
        StockCategory? category = null, string? search = null)
    {
        var query = _db.Stocks.AsQueryable();

        if (!string.IsNullOrEmpty(exchange))
            query = query.Where(s => s.Exchange == exchange);

        if (category.HasValue)
            query = query.Where(s => s.Category == category.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s =>
                s.TradingCode.Contains(search) ||
                s.CompanyName.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.TradingCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
