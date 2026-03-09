using BdStockOMS.API.Exchange;
using BdStockOMS.API.Models;
namespace BdStockOMS.API.Services;

public record MarketDepthSnapshot(
    int      StockId,
    string   TradingCode,
    string   Exchange,
    List<DepthLevelDto> Bids,
    List<DepthLevelDto> Asks,
    DateTime UpdatedAt);

public interface IMarketDepthService
{
    Task<MarketDepthSnapshot?> GetDepthAsync(int stockId);
    Task<MarketDepthSnapshot> RefreshDepthAsync(int stockId);
    Task<MarketDepth> UpsertDepthAsync(int stockId, MarketDepthDto dto);
}
