using BdStockOMS.API.Data;
using BdStockOMS.API.Exchange;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
namespace BdStockOMS.API.Services;

public class MarketDepthService : IMarketDepthService
{
    private readonly AppDbContext _db;
    private readonly IExchangeConnectorFactory _factory;

    public MarketDepthService(AppDbContext db, IExchangeConnectorFactory factory)
    {
        _db      = db;
        _factory = factory;
    }

    public async Task<MarketDepthSnapshot?> GetDepthAsync(int stockId)
    {
        var depth = await _db.MarketDepths
            .Include(d => d.Stock)
            .FirstOrDefaultAsync(d => d.StockId == stockId);

        if (depth == null) return null;

        return ToSnapshot(depth);
    }

    public async Task<MarketDepthSnapshot> RefreshDepthAsync(int stockId)
    {
        var stock = await _db.Stocks.FindAsync(stockId)
            ?? throw new InvalidOperationException($"Stock {stockId} not found");

        var connector = _factory.GetConnector(stock.Exchange);
        var dto       = await connector.GetMarketDepthAsync(stock.TradingCode);
        var depth     = await UpsertDepthAsync(stockId, dto);

        return ToSnapshot(depth, stock.TradingCode);
    }

    public async Task<MarketDepth> UpsertDepthAsync(int stockId, MarketDepthDto dto)
    {
        var depth = await _db.MarketDepths
            .FirstOrDefaultAsync(d => d.StockId == stockId);

        if (depth == null)
        {
            depth = new MarketDepth { StockId = stockId };
            _db.MarketDepths.Add(depth);
        }

        var stock = await _db.Stocks.FindAsync(stockId);
        depth.Exchange  = stock?.Exchange ?? string.Empty;
        depth.UpdatedAt = DateTime.UtcNow;

        // Map bid levels
        var bids = dto.Bids.Take(5).ToList();
        depth.Bid1Price = bids.ElementAtOrDefault(0)?.Price ?? 0;
        depth.Bid1Qty   = bids.ElementAtOrDefault(0)?.Quantity ?? 0;
        depth.Bid2Price = bids.ElementAtOrDefault(1)?.Price ?? 0;
        depth.Bid2Qty   = bids.ElementAtOrDefault(1)?.Quantity ?? 0;
        depth.Bid3Price = bids.ElementAtOrDefault(2)?.Price ?? 0;
        depth.Bid3Qty   = bids.ElementAtOrDefault(2)?.Quantity ?? 0;
        depth.Bid4Price = bids.ElementAtOrDefault(3)?.Price ?? 0;
        depth.Bid4Qty   = bids.ElementAtOrDefault(3)?.Quantity ?? 0;
        depth.Bid5Price = bids.ElementAtOrDefault(4)?.Price ?? 0;
        depth.Bid5Qty   = bids.ElementAtOrDefault(4)?.Quantity ?? 0;

        // Map ask levels
        var asks = dto.Asks.Take(5).ToList();
        depth.Ask1Price = asks.ElementAtOrDefault(0)?.Price ?? 0;
        depth.Ask1Qty   = asks.ElementAtOrDefault(0)?.Quantity ?? 0;
        depth.Ask2Price = asks.ElementAtOrDefault(1)?.Price ?? 0;
        depth.Ask2Qty   = asks.ElementAtOrDefault(1)?.Quantity ?? 0;
        depth.Ask3Price = asks.ElementAtOrDefault(2)?.Price ?? 0;
        depth.Ask3Qty   = asks.ElementAtOrDefault(2)?.Quantity ?? 0;
        depth.Ask4Price = asks.ElementAtOrDefault(3)?.Price ?? 0;
        depth.Ask4Qty   = asks.ElementAtOrDefault(3)?.Quantity ?? 0;
        depth.Ask5Price = asks.ElementAtOrDefault(4)?.Price ?? 0;
        depth.Ask5Qty   = asks.ElementAtOrDefault(4)?.Quantity ?? 0;

        await _db.SaveChangesAsync();
        return depth;
    }

    private static MarketDepthSnapshot ToSnapshot(MarketDepth d, string? tradingCode = null)
    {
        var bids = new List<DepthLevelDto>
        {
            new(d.Bid1Price, d.Bid1Qty),
            new(d.Bid2Price, d.Bid2Qty),
            new(d.Bid3Price, d.Bid3Qty),
            new(d.Bid4Price, d.Bid4Qty),
            new(d.Bid5Price, d.Bid5Qty),
        };
        var asks = new List<DepthLevelDto>
        {
            new(d.Ask1Price, d.Ask1Qty),
            new(d.Ask2Price, d.Ask2Qty),
            new(d.Ask3Price, d.Ask3Qty),
            new(d.Ask4Price, d.Ask4Qty),
            new(d.Ask5Price, d.Ask5Qty),
        };
        return new MarketDepthSnapshot(
            d.StockId,
            tradingCode ?? d.Stock?.TradingCode ?? string.Empty,
            d.Exchange, bids, asks, d.UpdatedAt);
    }
}
