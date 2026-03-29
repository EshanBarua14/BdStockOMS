namespace BdStockOMS.API.Services;

public record CseStockTick(
    string  TradingCode,
    decimal LastTradePrice,
    decimal Change,
    decimal ChangePercent,
    string  Direction
);

public record CseIndexData(
    decimal CASPI,
    decimal CSE30,
    decimal CASPIChange,
    DateTime FetchedAt
);

public interface ICseScraperService
{
    Task<List<CseStockTick>> GetAllPricesAsync(CancellationToken ct = default);
    Task<CseIndexData?>      GetIndexDataAsync(CancellationToken ct = default);
    bool IsMarketOpen();
}
