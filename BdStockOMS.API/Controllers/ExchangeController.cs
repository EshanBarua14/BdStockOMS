using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/exchange")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IExchangeScraperFactory _factory;

    public ExchangeController(AppDbContext db, IExchangeScraperFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    [HttpGet("status")]
    public IActionResult GetMarketStatus() => Ok(new
    {
        dse = new { isOpen = _factory.IsDseMarketOpen(), name = "Dhaka Stock Exchange" },
        cse = new { isOpen = _factory.IsCseMarketOpen(), name = "Chittagong Stock Exchange" },
        timestamp = DateTime.UtcNow
    });

    [HttpGet("stocks/{exchange}")]
    public async Task<IActionResult> GetStocksByExchange(string exchange)
    {
        var ex = exchange.ToUpperInvariant();
        var stocks = await _db.Stocks
            .Where(s => s.IsActive && s.Exchange.ToUpper() == ex)
            .OrderBy(s => s.TradingCode)
            .ToListAsync();
        return Ok(stocks);
    }

    [HttpGet("route")]
    public IActionResult RouteOrder(
        [FromQuery] string exchange,
        [FromQuery] string board,
        [FromQuery] string category)
    {
        if (!Enum.TryParse<ExchangeId>(exchange, true, out var ex))
            return BadRequest(new { message = "Invalid exchange. Use DSE or CSE." });
        if (!Enum.TryParse<Board>(board, true, out var b))
            return BadRequest(new { message = "Invalid board." });
        if (!Enum.TryParse<OrderCategory>(category, true, out var cat))
            return BadRequest(new { message = "Invalid order category." });

        var result = ExchangeOrderRouter.Route(ex, b, cat);
        return Ok(new
        {
            isAccepted = result.IsAccepted,
            exchange   = result.Exchange?.ToString(),
            message    = result.Message
        });
    }

    [HttpGet("boards")]
    public IActionResult GetBoards() => Ok(new
    {
        dse = new[] { "Public", "SME", "ATBPublic", "Government", "Debt", "Block", "BuyIn", "SPublic" },
        cse = new[] { "Public", "ATBPublic", "Block" },
        rules = new[]
        {
            "Block/BuyIn/SPublic: Limit orders only",
            "Government/Debt/SME: DSE only",
            "Market/MarketAtBest not allowed on Block board",
        }
    });
}
