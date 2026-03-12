using BdStockOMS.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketDepthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Random _rng = new();

    public MarketDepthController(AppDbContext db) => _db = db;

    [HttpGet("{tradingCode}")]
    public async Task<IActionResult> GetDepth(string tradingCode)
    {
        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.TradingCode == tradingCode && s.IsActive);

        if (stock == null)
            return NotFound(new { message = $"Stock {tradingCode} not found." });

        double p    = (double)stock.LastTradePrice;
        double bp   = 0.5 + (_rng.NextDouble() - 0.5) * 0.3;
        double tick = Math.Max(0.01, Math.Round(p * 0.001, 2));

        var bids = Enumerable.Range(1, 10).Select(i => new
        {
            price  = Math.Round(p - i * tick, 2),
            qty    = (int)(_rng.Next(500, 8000) * (bp + 0.1) / (i * 0.4 + 1)),
            orders = _rng.Next(1, 25),
        });

        var asks = Enumerable.Range(1, 10).Select(i => new
        {
            price  = Math.Round(p + i * tick, 2),
            qty    = (int)(_rng.Next(500, 8000) * (1 - bp + 0.1) / (i * 0.4 + 1)),
            orders = _rng.Next(1, 25),
        });

        return Ok(new
        {
            stockId     = stock.Id,
            tradingCode = stock.TradingCode,
            companyName = stock.CompanyName,
            lastPrice   = Math.Round(p, 2),
            bids,
            asks,
            spread      = Math.Round(tick * 2, 2),
            buyPressure = Math.Round(bp * 100, 1),
            updatedAt   = DateTime.UtcNow,
        });
    }

    [HttpGet("pressure/{tradingCode}")]
    public async Task<IActionResult> GetPressure(string tradingCode)
    {
        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.TradingCode == tradingCode && s.IsActive);

        if (stock == null)
            return NotFound();

        double chg = (double)stock.ChangePercent;
        double bp  = Math.Clamp(0.5 + chg * 0.06 + (_rng.NextDouble() - 0.5) * 0.1, 0.1, 0.9);

        return Ok(new
        {
            tradingCode  = stock.TradingCode,
            lastPrice    = stock.LastTradePrice,
            buyPressure  = Math.Round(bp * 100, 1),
            sellPressure = Math.Round((1 - bp) * 100, 1),
            volume       = stock.Volume,
            updatedAt    = DateTime.UtcNow,
        });
    }
}
