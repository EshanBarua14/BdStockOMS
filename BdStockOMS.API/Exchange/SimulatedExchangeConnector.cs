namespace BdStockOMS.API.Exchange;

public class SimulatedExchangeConnector : IExchangeConnector
{
    private readonly string _exchangeCode;
    private readonly ILogger<SimulatedExchangeConnector> _logger;
    private bool _connected;

    // Simulated price store (in-memory, resets on restart)
    private static readonly Dictionary<string, decimal> _prices = new()
    {
        ["GRAMEEN"]  = 32.50m,
        ["BEXIMCO"]  = 18.20m,
        ["SQPHARMA"] = 245.00m,
        ["BRAC"]     = 55.80m,
        ["RENATA"]   = 1180.00m,
    };

    public string ExchangeCode => _exchangeCode;
    public bool   IsConnected  => _connected;

    public SimulatedExchangeConnector(string exchangeCode,
        ILogger<SimulatedExchangeConnector> logger)
    {
        _exchangeCode = exchangeCode;
        _logger       = logger;
    }

    public Task<bool> ConnectAsync()
    {
        _connected = true;
        _logger.LogInformation("SimulatedExchangeConnector [{Exchange}] connected", _exchangeCode);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync()
    {
        _connected = false;
        _logger.LogInformation("SimulatedExchangeConnector [{Exchange}] disconnected", _exchangeCode);
        return Task.CompletedTask;
    }

    public Task<MarketTickDto> GetLatestPriceAsync(string tradingCode)
    {
        var basePrice = _prices.GetValueOrDefault(tradingCode.ToUpper(), 100m);
        // Simulate small random drift ±0.5%
        var rng    = new Random();
        var drift  = basePrice * (decimal)(rng.NextDouble() * 0.01 - 0.005);
        var price  = Math.Round(basePrice + drift, 2);
        var change = Math.Round(drift, 2);
        var pct    = basePrice == 0 ? 0 : Math.Round(change / basePrice * 100, 2);

        return Task.FromResult(new MarketTickDto(
            tradingCode, price, change, pct,
            rng.NextInt64(10_000, 500_000),
            DateTime.UtcNow));
    }

    public Task<MarketDepthDto> GetMarketDepthAsync(string tradingCode)
    {
        var basePrice = _prices.GetValueOrDefault(tradingCode.ToUpper(), 100m);
        var rng = new Random();

        var bids = Enumerable.Range(1, 5).Select(i =>
            new DepthLevelDto(Math.Round(basePrice - i * 0.10m, 2),
                              rng.NextInt64(1_000, 50_000))).ToList();

        var asks = Enumerable.Range(1, 5).Select(i =>
            new DepthLevelDto(Math.Round(basePrice + i * 0.10m, 2),
                              rng.NextInt64(1_000, 50_000))).ToList();

        return Task.FromResult(new MarketDepthDto(tradingCode, bids, asks));
    }

    public Task<List<OhlcDto>> GetHistoricalDataAsync(string tradingCode, DateTime from, DateTime to)
    {
        var basePrice = _prices.GetValueOrDefault(tradingCode.ToUpper(), 100m);
        var rng  = new Random(42);
        var data = new List<OhlcDto>();
        var cur  = from.Date;

        while (cur <= to.Date)
        {
            if (cur.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            { cur = cur.AddDays(1); continue; }

            var open  = basePrice + (decimal)(rng.NextDouble() * 4 - 2);
            var close = open      + (decimal)(rng.NextDouble() * 4 - 2);
            var high  = Math.Max(open, close) + (decimal)(rng.NextDouble() * 2);
            var low   = Math.Min(open, close) - (decimal)(rng.NextDouble() * 2);

            data.Add(new OhlcDto(cur,
                Math.Round(open, 2), Math.Round(high, 2),
                Math.Round(low,  2), Math.Round(close, 2),
                rng.NextInt64(50_000, 2_000_000)));

            basePrice = close;
            cur = cur.AddDays(1);
        }
        return Task.FromResult(data);
    }

    public Task<ExchangeOrderResult> SendOrderAsync(ExchangeOrderRequest order)
    {
        _logger.LogInformation(
            "Simulated [{Exchange}] SendOrder: {Code} {Side} {Qty}@{Price}",
            _exchangeCode, order.TradingCode, order.Side, order.Quantity, order.Price);

        // Simulate 95% acceptance rate
        var rng = new Random();
        if (rng.Next(100) < 5)
            return Task.FromResult(new ExchangeOrderResult(false, order.ExchangeOrderId,
                "Rejected", "Simulated exchange rejection"));

        return Task.FromResult(new ExchangeOrderResult(true, order.ExchangeOrderId,
            "Acknowledged"));
    }

    public Task<ExchangeOrderResult> CancelOrderAsync(string exchangeOrderId)
    {
        _logger.LogInformation("Simulated [{Exchange}] CancelOrder: {Id}",
            _exchangeCode, exchangeOrderId);
        return Task.FromResult(new ExchangeOrderResult(true, exchangeOrderId, "Cancelled"));
    }

    public Task<ExchangeOrderStatus> GetOrderStatusAsync(string exchangeOrderId)
    {
        return Task.FromResult(new ExchangeOrderStatus(
            exchangeOrderId, "Filled", 100, 100.00m));
    }
}
