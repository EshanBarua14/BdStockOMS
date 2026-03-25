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

    private static readonly string[] Categories =
        ["price-sensitive", "regulatory", "corporate", "general"];

    private static readonly string[] Boards =
        ["A", "B", "N", "Z", "SME"];

    public NewsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Paginated + filtered news feed
    /// GET /api/news?page=1&pageSize=15&keyword=bank&board=A&category=price-sensitive
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNews(
        [FromQuery] string?  keyword  = null,
        [FromQuery] string?  board    = null,
        [FromQuery] string?  category = null,
        [FromQuery] string?  tradingCode = null,
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 20,
        // Legacy param — kept for backward compat
        [FromQuery] int      count    = 0)
    {
        if (count > 0 && page == 1 && pageSize == 20) pageSize = Math.Min(count, 50);
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var stocks = await _db.Stocks
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Volume)
            .Take(50)
            .ToListAsync();

        if (stocks.Count == 0)
            return Ok(new { items = Array.Empty<object>(), totalCount = 0, page, pageSize, totalPages = 0 });

        // Generate a stable seed pool of news items
        var allNews = Enumerable.Range(0, 40).Select(i =>
        {
            var stock    = stocks[i % stocks.Count];
            var tmpl     = Templates[i % Templates.Length];
            var cat      = Categories[i % Categories.Length];
            var brd      = Boards[i % Boards.Length];
            var priceSens = cat == "price-sensitive";
            var title    = string.Format(tmpl,
                stock.TradingCode,
                _rng.Next(5, 150),
                DateTime.UtcNow.Year);

            return new
            {
                id             = i + 1,
                title,
                summary        = $"{title}. Market participants are closely monitoring developments as trading activity picks up.",
                category       = cat,
                board          = brd,
                tradingCode    = stock.TradingCode,
                source         = "DSE Market Feed",
                sourceUrl      = (string?)null,
                isPriceSensitive = priceSens,
                publishedAt    = DateTime.UtcNow.AddMinutes(-(i * 18)),
                keywords       = new[] { stock.TradingCode.ToLower(), cat },
            };
        }).ToList();

        // Apply filters
        var filtered = allNews.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            filtered = filtered.Where(n =>
                n.title.ToLower().Contains(kw) ||
                n.tradingCode.ToLower().Contains(kw) ||
                n.keywords.Any(k => k.Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(board) && board != "ALL")
            filtered = filtered.Where(n => n.board == board);

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            filtered = filtered.Where(n => n.category == category);

        if (!string.IsNullOrWhiteSpace(tradingCode))
            filtered = filtered.Where(n => n.tradingCode == tradingCode.ToUpper());

        var list       = filtered.ToList();
        var totalCount = list.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var items      = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new { items, totalCount, page, pageSize, totalPages });
    }
}
