using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BdStockOMS.API.Controllers;
[ApiController]
[Route("api/baskets")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class BasketController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    public BasketController(AppDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var baskets = await _db.Baskets.Include(b => b.Stocks)
            .Where(b => b.BrokerageHouseId == _tenant.BrokerageHouseId && b.IsActive)
            .Select(b => new { b.Id, b.Name, b.Description, b.IsActive, b.CreatedAt, stockCount = b.Stocks.Count })
            .ToListAsync();
        return Ok(baskets);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBasketRequest req)
    {
        var b = new Basket { Name = req.Name, Description = req.Description,
            BrokerageHouseId = _tenant.BrokerageHouseId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Baskets.Add(b);
        await _db.SaveChangesAsync();
        return Ok(b);
    }

    [HttpPost("{id}/stocks")]
    public async Task<IActionResult> AddStock(int id, [FromBody] AddStockRequest req)
    {
        if (await _db.BasketStocks.AnyAsync(s => s.BasketId == id && s.StockId == req.StockId))
            return BadRequest(new { message = "Stock already in basket." });
        _db.BasketStocks.Add(new BasketStock { BasketId = id, StockId = req.StockId, MaxOrderValue = req.MaxOrderValue });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Stock added." });
    }

    [HttpDelete("{id}/stocks/{stockId}")]
    public async Task<IActionResult> RemoveStock(int id, int stockId)
    {
        var s = await _db.BasketStocks.FirstOrDefaultAsync(s => s.BasketId == id && s.StockId == stockId);
        if (s == null) return NotFound();
        _db.BasketStocks.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Stock removed." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Baskets.FindAsync(id);
        if (b == null || b.BrokerageHouseId != _tenant.BrokerageHouseId) return NotFound();
        b.IsActive = false; b.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Basket deactivated." });
    }
}
public record CreateBasketRequest(string Name, string? Description);
public record AddStockRequest(int StockId, decimal? MaxOrderValue);
