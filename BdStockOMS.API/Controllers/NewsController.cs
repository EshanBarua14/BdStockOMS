using BdStockOMS.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Random _rng = new();

    private static readonly string[] Templates =
    [
        "{0} volume surges {1}% above 30-day average",
        "BSEC approves new margin rules affecting {0} stocks",
        "{0} quarterly earnings beat analyst estimates by {1}%",
        "Bangladesh Bank policy impacts {0} banking sector",
        "{0} announces {1}% cash dividend for FY{2}",
        "Foreign investors net buyers of {0} worth ৳{1} crore",
        "DSE turnover rises as {0} leads sector gainers",
        "CDBL system upgrade improves {0} settlement efficiency",
        "{0} rights issue oversubscribed {1}x — shares resume",
        "Circuit breaker triggered for {0} on high volatility",
    ];

    public NewsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLatest([FromQuery] int count = 20)
    {
        var stocks = await _db.Stocks
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Volume)
            .Take(30)
            .ToListAsync();

        var news = Enumerable.Range(0, Math.Min(count, 20)).Select(i =>
        {
            var stock   = stocks[_rng.Next(stocks.Count)];
            var tmpl    = Templates[i % Templates.Length];
            var quarter = ((DateTime.UtcNow.Month - 1) / 3) + 1;
            var title   = string.Format(tmpl,
                stock.TradingCode,
                _rng.Next(5, 150),
                DateTime.UtcNow.Year);

            return new
            {
                id         = Guid.NewGuid(),
                title,
                tag        = stock.TradingCode,
                importance = _rng.NextDouble() switch { < 0.2 => "high", < 0.6 => "medium", _ => "low" },
                time       = DateTime.UtcNow.AddMinutes(-i * 15).ToString("hh:mm tt"),
                timestamp  = DateTime.UtcNow.AddMinutes(-i * 15),
            };
        }).ToList();

        return Ok(news);
    }
}
