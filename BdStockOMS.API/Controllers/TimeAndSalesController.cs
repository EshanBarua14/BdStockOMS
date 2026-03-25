using BdStockOMS.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

public enum AggressorSide { Unknown = 0, Buy = 1, Sell = -1 }

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimeAndSalesController : ControllerBase
{
    private readonly AppDbContext _db;
    public TimeAndSalesController(AppDbContext db) => _db = db;

    /// <summary>
    /// GET /api/timeandsales/{tradingCode}?count=80&aggressorFilter=1
    /// </summary>
    [HttpGet("{tradingCode}")]
    public async Task<IActionResult> GetTimeAndSales(
        string tradingCode,
        [FromQuery] int count = 80,
        [FromQuery] AggressorSide? aggressorFilter = null)
    {
        if (count < 1 || count > 500) count = 80;

        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.TradingCode == tradingCode.ToUpper() && s.IsActive);

        var basePrice = stock?.LastTradePrice ?? 50m;
        var rng       = new Random(tradingCode.GetHashCode());
        var now       = DateTime.UtcNow;
        var seq       = 100 + rng.Next(500);
        var price     = basePrice;
        var entries   = new List<object>();
        var date      = now.Date.ToString("yyyyMMdd");

        for (int i = 0; i < count * 2; i++)
        {
            var delta     = (decimal)((rng.NextDouble() - 0.5) * 0.8);
            price         = Math.Max(price + delta, basePrice * 0.85m);
            var vol       = (long)(rng.Next(100, 5000) / 100) * 100;
            var aggressor = rng.NextDouble() > 0.5 ? AggressorSide.Buy : AggressorSide.Sell;

            if (aggressorFilter.HasValue && aggressor != aggressorFilter.Value) continue;

            entries.Add(new
            {
                id           = i + 1,
                tradeMatchId = $"{date}-{(seq + i):D6}",
                tradingCode  = tradingCode.ToUpper(),
                price        = Math.Round(price, 2),
                volume       = vol,
                value        = Math.Round(price * vol, 2),
                executedAt   = now.AddSeconds(-(i * 15) - rng.Next(10)),
                aggressor    = (int)aggressor,
                priceChange  = Math.Round(delta, 2),
                previousClose = stock?.ClosePrice,
            });

            if (entries.Count >= count) break;
        }

        return Ok(entries.OrderByDescending(e => ((dynamic)e).executedAt).ToList());
    }
}
