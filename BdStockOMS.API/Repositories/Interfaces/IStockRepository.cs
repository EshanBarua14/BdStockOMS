using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IStockRepository : IRepository<Stock>
{
    // Get stock by trading code (GP, BRACBANK etc)
    Task<Stock?> GetByTradingCodeAsync(string tradingCode);

    // Get all stocks from one exchange
    Task<IEnumerable<Stock>> GetByExchangeAsync(string exchange);
    // exchange = "DSE" or "CSE"

    // Scraper calls this to update all prices at once
    Task UpdatePricesAsync(IEnumerable<Stock> stocks);

    // Search stocks by company name or trading code
    Task<IEnumerable<Stock>> SearchAsync(string searchTerm);
}