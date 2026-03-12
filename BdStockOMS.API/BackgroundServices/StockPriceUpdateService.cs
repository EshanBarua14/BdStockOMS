using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using BdStockOMS.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

/// <summary>
/// Realistic DSE/CSE market simulation engine.
/// Generates OHLCV data, order book depth, buy/sell pressure,
/// index values and news — all pushed via SignalR every 2s.
/// </summary>
public class StockPriceUpdateService : BackgroundService
{
    private readonly IHubContext<StockPriceHub> _hubContext;
    private readonly IServiceScopeFactory       _scopeFactory;
    private readonly ILogger<StockPriceUpdateService> _logger;

    // Per-stock simulation state
    private readonly Dictionary<int, StockState> _states = new();
    private readonly Random _rng = new(42);
    private int _tick = 0;

    // Market regime
    private double _marketSentiment = 0.0;   // -1 bear .. +1 bull
    private int    _sentimentTicks   = 0;

    public StockPriceUpdateService(
        IHubContext<StockPriceHub>        hubContext,
        IServiceScopeFactory              scopeFactory,
        ILogger<StockPriceUpdateService>  logger)
    {
        _hubContext   = hubContext;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Stock Price Update Service started.");
        await InitialiseStates(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _tick++;
                UpdateMarketSentiment();
                await BroadcastTick(ct);

                // Slower tasks
                if (_tick % 5  == 0) await BroadcastDepth(ct);
                if (_tick % 10 == 0) await BroadcastPressure(ct);
                if (_tick % 30 == 0) await BroadcastIndices(ct);
                if (_tick % 60 == 0) await BroadcastNews(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulation tick error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    // ── Initialise realistic base prices ─────────────────────────────────
    private async Task InitialiseStates(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stocks = await db.Stocks.Where(s => s.IsActive).ToListAsync(ct);

        foreach (var s in stocks)
        {
            // If ClosePrice is 0 seed it from LastTradePrice
            if (s.ClosePrice == 0 && s.LastTradePrice > 0)
            {
                s.ClosePrice = s.LastTradePrice;
                s.HighPrice  = s.LastTradePrice * 1.02m;
                s.LowPrice   = s.LastTradePrice * 0.98m;
                s.Volume     = _rng.Next(10_000, 500_000);
            }

            double vol = GetVolatility(s.Category);
            _states[s.Id] = new StockState
            {
                BasePrice    = (double)s.LastTradePrice,
                CurrentPrice = (double)s.LastTradePrice,
                ClosePrice   = (double)s.ClosePrice,
                Volatility   = vol,
                Trend        = (_rng.NextDouble() - 0.5) * 0.002,
                BuyPressure  = 0.5 + (_rng.NextDouble() - 0.5) * 0.2,
                Volume       = (long)(s.Volume == 0 ? _rng.Next(10_000, 300_000) : s.Volume),
                High         = (double)(s.HighPrice > 0 ? s.HighPrice : s.LastTradePrice * 1.02m),
                Low          = (double)(s.LowPrice  > 0 ? s.LowPrice  : s.LastTradePrice * 0.98m),
            };
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Initialised {Count} stock states.", _states.Count);
    }

    // ── Volatility by sector/category ────────────────────────────────────
    private static double GetVolatility(BdStockOMS.API.Models.StockCategory cat) => cat switch
    {
        BdStockOMS.API.Models.StockCategory.Z    => 0.008,
        BdStockOMS.API.Models.StockCategory.B    => 0.006,
        BdStockOMS.API.Models.StockCategory.N    => 0.007,
        BdStockOMS.API.Models.StockCategory.G    => 0.003,
        BdStockOMS.API.Models.StockCategory.Spot => 0.004,
        _                                         => 0.005,
    };

    // ── Market sentiment drift ────────────────────────────────────────────
    private void UpdateMarketSentiment()
    {
        _sentimentTicks--;
        if (_sentimentTicks <= 0)
        {
            _marketSentiment  = (_rng.NextDouble() - 0.48) * 0.6;
            _sentimentTicks   = _rng.Next(20, 80);
        }
        // Random shocks
        if (_rng.NextDouble() < 0.01)
            _marketSentiment += (_rng.NextDouble() - 0.5) * 0.4;
        _marketSentiment = Math.Clamp(_marketSentiment, -1.0, 1.0);
    }

    // ── Main price tick ───────────────────────────────────────────────────
    private async Task BroadcastTick(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stocks = await db.Stocks.Where(s => s.IsActive).ToListAsync(ct);

        var updates = new List<object>();

        foreach (var stock in stocks)
        {
            if (!_states.TryGetValue(stock.Id, out var state)) continue;

            // ── GBM price simulation ──────────────────────────────────────
            double dt       = 2.0 / 86400.0;              // 2s in trading day
            double drift    = state.Trend + _marketSentiment * 0.001;
            double diffusion = state.Volatility * GaussianNoise();
            double move     = drift + diffusion * Math.Sqrt(dt) * 100;

            // Mean reversion towards base
            double reversion = (state.BasePrice - state.CurrentPrice) / state.BasePrice * 0.005;
            move += reversion;

            state.CurrentPrice = Math.Max(0.10, state.CurrentPrice * (1 + move / 100));

            // Circuit breaker ±10%
            double cbHigh = state.BasePrice * 1.10;
            double cbLow  = state.BasePrice * 0.90;
            state.CurrentPrice = Math.Clamp(state.CurrentPrice, cbLow, cbHigh);

            // OHLCV
            if (state.CurrentPrice > state.High) state.High = state.CurrentPrice;
            if (state.CurrentPrice < state.Low)  state.Low  = state.CurrentPrice;

            // Volume — higher when price moves more
            long volTick = (long)(_rng.Next(100, 5000) * (1 + Math.Abs(move) * 10));
            state.Volume += volTick;

            // Buy pressure — correlated with price direction
            double pressureNoise = (_rng.NextDouble() - 0.5) * 0.05;
            state.BuyPressure += move * 0.5 + pressureNoise;
            state.BuyPressure  = Math.Clamp(state.BuyPressure, 0.1, 0.9);

            decimal newPrice    = Math.Round((decimal)state.CurrentPrice, 2);
            decimal closePrice  = (decimal)state.ClosePrice;
            decimal change      = newPrice - closePrice;
            decimal changePct   = closePrice > 0 ? Math.Round(change / closePrice * 100, 2) : 0;

            // Persist to DB
            stock.LastTradePrice = newPrice;
            stock.Change         = change;
            stock.ChangePercent  = changePct;
            stock.HighPrice      = Math.Round((decimal)state.High,   2);
            stock.LowPrice       = Math.Round((decimal)state.Low,    2);
            stock.Volume         = state.Volume;
            stock.LastUpdatedAt  = DateTime.UtcNow;

            // Value in million taka
            stock.ValueInMillionTaka = Math.Round(newPrice * state.Volume / 1_000_000m, 2);

            var update = new
            {
                stockId        = stock.Id,
                tradingCode    = stock.TradingCode,
                companyName    = stock.CompanyName,
                exchange       = stock.Exchange,
                category       = stock.Category,
                lastTradePrice = newPrice,
                change,
                changePercent  = changePct,
                highPrice      = stock.HighPrice,
                lowPrice       = stock.LowPrice,
                closePrice     = stock.ClosePrice,
                volume         = stock.Volume,
                valueInMillionTaka = stock.ValueInMillionTaka,
                buyPressure    = Math.Round(state.BuyPressure * 100, 1),
                sellPressure   = Math.Round((1 - state.BuyPressure) * 100, 1),
                updatedAt      = stock.LastUpdatedAt,
            };

            updates.Add(update);

            // Per-stock group push
            await _hubContext.Clients
                .Group(stock.TradingCode)
                .SendAsync("PriceUpdate", update, ct);
        }

        // Bulk push to all clients
        await _hubContext.Clients.All.SendAsync("BulkPriceUpdate", updates, ct);

        await db.SaveChangesAsync(ct);
        _logger.LogDebug("Tick {Tick}: updated {Count} stocks.", _tick, updates.Count);
    }

    // ── Market depth (order book) per subscribed stock ────────────────────
    private async Task BroadcastDepth(CancellationToken ct)
    {
        foreach (var (stockId, state) in _states)
        {
            double p     = state.CurrentPrice;
            double tick  = Math.Max(0.01, Math.Round(p * 0.001, 2));
            double bp    = state.BuyPressure;

            var bids = Enumerable.Range(1, 10).Select(i => new
            {
                price  = Math.Round(p - i * tick, 2),
                qty    = (int)((_rng.Next(500, 8000)) * (bp + 0.1) / (i * 0.4 + 1)),
                orders = _rng.Next(1, 25),
            }).ToList();

            var asks = Enumerable.Range(1, 10).Select(i => new
            {
                price  = Math.Round(p + i * tick, 2),
                qty    = (int)((_rng.Next(500, 8000)) * (1 - bp + 0.1) / (i * 0.4 + 1)),
                orders = _rng.Next(1, 25),
            }).ToList();

            // Find the trading code to use as group name
            var code = _states.Keys.Contains(stockId)
                ? await GetTradingCode(stockId, ct)
                : null;
            if (code == null) continue;

            await _hubContext.Clients.Group(code).SendAsync("DepthUpdate", new
            {
                stockId,
                tradingCode = code,
                lastPrice   = Math.Round(p, 2),
                bids,
                asks,
                spread      = Math.Round(tick * 2, 2),
                updatedAt   = DateTime.UtcNow,
            }, ct);
        }
    }

    // ── Buy/sell pressure broadcast ───────────────────────────────────────
    private async Task BroadcastPressure(CancellationToken ct)
    {
        var pressureList = _states.Select(kvp => new
        {
            stockId     = kvp.Key,
            buyPressure  = Math.Round(kvp.Value.BuyPressure * 100, 1),
            sellPressure = Math.Round((1 - kvp.Value.BuyPressure) * 100, 1),
        }).ToList();

        await _hubContext.Clients.All.SendAsync("PressureUpdate", pressureList, ct);
    }

    // ── DSE/CSE index simulation ──────────────────────────────────────────
    private double _dsex = 6248.30, _dses = 1312.45, _ds30 = 2187.60;
    private double _cseAll = 18420.50, _cse30 = 9841.20;

    private async Task BroadcastIndices(CancellationToken ct)
    {
        double avgChg = _states.Values
            .Select(s => (s.CurrentPrice - s.ClosePrice) / s.ClosePrice * 100)
            .DefaultIfEmpty(0)
            .Average();

        double noise() => (_rng.NextDouble() - 0.5) * 2;

        _dsex   = Math.Max(5000, _dsex   + avgChg * 8  + noise());
        _dses   = Math.Max(1000, _dses   + avgChg * 2  + noise() * 0.3);
        _ds30   = Math.Max(1500, _ds30   + avgChg * 3  + noise() * 0.5);
        _cseAll = Math.Max(10000, _cseAll + avgChg * 12 + noise() * 1.5);
        _cse30  = Math.Max(5000, _cse30  + avgChg * 5  + noise() * 0.8);

        await _hubContext.Clients.All.SendAsync("IndexUpdate", new
        {
            dsex    = Math.Round(_dsex,   2),
            dses    = Math.Round(_dses,   2),
            ds30    = Math.Round(_ds30,   2),
            cseAll  = Math.Round(_cseAll, 2),
            cse30   = Math.Round(_cse30,  2),
            updatedAt = DateTime.UtcNow,
        }, ct);
    }

    // ── Market news simulation ────────────────────────────────────────────
    private static readonly string[] _newsTemplates =
    [
        "{0} volume surges {1}% above 30-day average on strong buying",
        "{0} hits {2} high as institutional investors accumulate",
        "BSEC reviews circuit breaker rules for {0} category stocks",
        "{0} dividend announcement drives retail interest",
        "DSE turnover rises as {0} leads sector gainers",
        "{0} reports Q{3} earnings — revenue {4}% vs estimates",
        "Bangladesh Bank policy decision impacts {0} banking stocks",
        "{0} completes rights issue — shares resume trading",
        "Foreign investors net buyers of {0} worth ৳{5} crore",
        "CDBL system upgrade improves settlement for {0} trades",
    ];

    private static readonly string[] _sectors =
        ["telecom","banking","pharma","energy","textile","cement","insurance","NBFI","IT","food"];

    private int _newsIdx = 0;

    private async Task BroadcastNews(CancellationToken ct)
    {
        var codes = _states.Keys.ToList();
        if (codes.Count == 0) return;

        var code    = await GetTradingCode(codes[_rng.Next(codes.Count)], ct);
        var tmpl    = _newsTemplates[_newsIdx++ % _newsTemplates.Length];
        var quarter = ((DateTime.UtcNow.Month - 1) / 3) + 1;
        var title   = string.Format(tmpl,
            code ?? "DSE",
            _rng.Next(20, 150),
            DateTime.UtcNow.Year,
            quarter,
            _rng.Next(5, 40),
            _rng.Next(10, 500));

        var importance = _rng.NextDouble() switch
        {
            < 0.2  => "high",
            < 0.6  => "medium",
            _      => "low",
        };

        await _hubContext.Clients.All.SendAsync("NewsUpdate", new
        {
            id         = Guid.NewGuid(),
            title,
            tag        = code ?? _sectors[_rng.Next(_sectors.Length)],
            importance,
            time       = DateTime.UtcNow.ToString("hh:mm tt"),
            timestamp  = DateTime.UtcNow,
        }, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private readonly Dictionary<int, string> _codeCache = new();

    private async Task<string?> GetTradingCode(int stockId, CancellationToken ct)
    {
        if (_codeCache.TryGetValue(stockId, out var code)) return code;
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stock = await db.Stocks.FindAsync([stockId], ct);
        if (stock != null) _codeCache[stockId] = stock.TradingCode;
        return stock?.TradingCode;
    }

    private double GaussianNoise()
    {
        // Box-Muller transform
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    private class StockState
    {
        public double BasePrice    { get; set; }
        public double CurrentPrice { get; set; }
        public double ClosePrice   { get; set; }
        public double Volatility   { get; set; }
        public double Trend        { get; set; }
        public double BuyPressure  { get; set; }
        public long   Volume       { get; set; }
        public double High         { get; set; }
        public double Low          { get; set; }
    }
}
