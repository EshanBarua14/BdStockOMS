using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IStockRepository : IRepository<Stock>
{
    Task<Stock?> GetByTradingCodeAsync(string tradingCode, string exchange);
    Task<(IEnumerable<Stock> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? exchange = null,
        StockCategory? category = null, string? search = null);
}
