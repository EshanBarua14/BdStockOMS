using BdStockOMS.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PriceHistoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public PriceHistoryController(AppDbContext db) => _db = db;

    /// <summary>
    /// GET /api/pricehistory/{tradingCode}?fromDate=2026-01-01&toDate=2026-03-24&interval=daily
    /// </summary>
    [HttpGet("{tradingCode}")]
    public async Task<IActionResult> GetPriceHistory(
        string tradingCode,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate   = null,
        [FromQuery] string    interval = "daily")
    {
        var from = fromDate ?? DateTime.Today.AddMonths(-3);
        var to   = toDate   ?? DateTime.Today;

        if (from > to)
            return BadRequest("fromDate must be before toDate.");
        if ((to - from).TotalDays > 730)
            return BadRequest("Date range cannot exceed 2 years.");

        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.TradingCode == tradingCode.ToUpper() && s.IsActive);

        var basePrice = stock?.LastTradePrice ?? 50m;
        var rng       = new Random(tradingCode.GetHashCode());
        var price     = basePrice * (0.85m + (decimal)(rng.NextDouble() * 0.3));
        decimal? prevClose = null;
        var daily     = new List<object>();

        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday)
                continue;

            var vol  = (decimal)(rng.NextDouble() * 0.04 - 0.02);
            var open = price;
            var cls  = Math.Round(open * (1 + vol), 2);
            var high = Math.Round(Math.Max(open, cls) * (1 + (decimal)(rng.NextDouble() * 0.015)), 2);
            var low  = Math.Round(Math.Min(open, cls) * (1 - (decimal)(rng.NextDouble() * 0.015)), 2);
            var volume = (long)(rng.Next(50000, 2000000) / 100) * 100;
            var change = prevClose.HasValue ? Math.Round(cls - prevClose.Value, 2) : 0m;
            var changePct = prevClose.HasValue && prevClose.Value != 0
                ? Math.Round((cls - prevClose.Value) / prevClose.Value * 100, 2) : 0m;

            daily.Add(new
            {
                date       = date.ToString("yyyy-MM-dd"),
                open,
                high,
                low,
                close      = cls,
                volume,
                value      = Math.Round(cls * volume / 1_000_000, 2),
                trades     = rng.Next(50, 800),
                previousClose = prevClose,
                change,
                changePct,
                isUp       = change >= 0,
            });

            prevClose = cls;
            price     = cls;
        }

        daily.Reverse(); // newest first

        // Aggregate if needed
        var data = interval.ToLower() switch
        {
            "weekly"  => AggregateWeekly(daily),
            "monthly" => AggregateMonthly(daily),
            _         => daily
        };

        var yearAgo   = DateTime.Today.AddYears(-1);
        var yearData  = daily.Cast<dynamic>().Where(d => DateTime.Parse((string)d.date) >= yearAgo).ToList();

        return Ok(new
        {
            tradingCode  = tradingCode.ToUpper(),
            companyName  = stock?.CompanyName ?? tradingCode.ToUpper(),
            board        = stock?.Category.ToString() ?? "A",
            data,
            fiftyTwoWeekHigh = yearData.Any() ? (decimal?)yearData.Max(d => (decimal)d.high) : null,
            fiftyTwoWeekLow  = yearData.Any() ? (decimal?)yearData.Min(d => (decimal)d.low)  : null,
            averageVolume    = daily.Any() ? (decimal?)daily.Cast<dynamic>().Average(d => (long)d.volume) : null,
            averageClose     = daily.Any() ? (decimal?)daily.Cast<dynamic>().Average(d => (decimal)d.close) : null,
        });
    }

    private static List<object> AggregateWeekly(List<object> daily)
    {
        return daily.Cast<dynamic>()
            .GroupBy(d => {
                var dt = DateTime.Parse((string)d.date);
                return $"{ISOWeek.GetYear(dt)}-W{ISOWeek.GetWeekOfYear(dt):D2}";
            })
            .Select(g => {
                var ordered = g.OrderBy(d => (string)d.date).ToList();
                var first   = ordered.First();
                var last    = ordered.Last();
                return (object)new {
                    date      = (string)first.date,
                    open      = (decimal)first.open,
                    high      = g.Max(d => (decimal)d.high),
                    low       = g.Min(d => (decimal)d.low),
                    close     = (decimal)last.close,
                    volume    = g.Sum(d => (long)d.volume),
                    value     = g.Sum(d => (decimal)d.value),
                    trades    = g.Sum(d => (int)d.trades),
                    previousClose = (decimal?)first.previousClose,
                    change    = (decimal)last.change,
                    changePct = (decimal)last.changePct,
                    isUp      = (bool)last.isUp,
                };
            })
            .OrderByDescending(d => ((dynamic)d).date)
            .ToList<object>();
    }

    private static List<object> AggregateMonthly(List<object> daily)
    {
        return daily.Cast<dynamic>()
            .GroupBy(d => ((string)d.date)[..7]) // "yyyy-MM"
            .Select(g => {
                var ordered = g.OrderBy(d => (string)d.date).ToList();
                var first   = ordered.First();
                var last    = ordered.Last();
                return (object)new {
                    date      = (string)first.date,
                    open      = (decimal)first.open,
                    high      = g.Max(d => (decimal)d.high),
                    low       = g.Min(d => (decimal)d.low),
                    close     = (decimal)last.close,
                    volume    = g.Sum(d => (long)d.volume),
                    value     = g.Sum(d => (decimal)d.value),
                    trades    = g.Sum(d => (int)d.trades),
                    previousClose = (decimal?)first.previousClose,
                    change    = (decimal)last.change,
                    changePct = (decimal)last.changePct,
                    isUp      = (bool)last.isUp,
                };
            })
            .OrderByDescending(d => ((dynamic)d).date)
            .ToList<object>();
    }
}
