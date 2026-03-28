namespace BdStockOMS.API.Services;

public record DseStockTick(
    string  TradingCode,
    decimal LastTradePrice,
    decimal Change,
    decimal ChangePercent,
    string  Direction  // "up" | "down" | "neutral"
);

public record DseIndexData(
    decimal DSEX,
    decimal DSES,
    decimal DS30,
    decimal DSEXChange,
    DateTime FetchedAt
);

public interface IDseScraperService
{
    Task<List<DseStockTick>> GetAllPricesAsync(CancellationToken ct = default);
    Task<DseIndexData?>      GetIndexDataAsync(CancellationToken ct = default);
    bool IsMarketOpen();
}
