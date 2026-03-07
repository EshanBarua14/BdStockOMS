// Controllers/StockController.cs
using BdStockOMS.API.DTOs.Stock;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/stocks")]
[Authorize] // all endpoints need a valid JWT
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/stocks
    // All logged-in users can view stocks
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAllStocks()
    {
        var stocks = await _stockService.GetAllStocksAsync();
        return Ok(stocks);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/stocks/search?query=GP
    // Search by TradingCode or CompanyName
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("search")]
    public async Task<IActionResult> SearchStocks([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Search query cannot be empty." });

        var stocks = await _stockService.SearchStocksAsync(query);
        return Ok(stocks);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/stocks/{id}
    // All logged-in users can view a single stock
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStockById(int id)
    {
        var stock = await _stockService.GetStockByIdAsync(id);
        if (stock == null)
            return NotFound(new { message = "Stock not found." });

        return Ok(stock);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/stocks
    // Only CCD and Admin can add stocks
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "CCD,Admin")]
    public async Task<IActionResult> CreateStock([FromBody] CreateStockDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (stock, error) = await _stockService.CreateStockAsync(dto);
        if (error != null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetStockById),
            new { id = stock!.Id }, stock);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/stocks/{id}
    // Only CCD and Admin can update stock prices
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "CCD,Admin")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (stock, error) = await _stockService.UpdateStockAsync(id, dto);
        if (error != null)
            return NotFound(new { message = error });

        return Ok(stock);
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE /api/stocks/{id}  (soft delete)
    // Only CCD and Admin can deactivate stocks
    // ─────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "CCD,Admin")]
    public async Task<IActionResult> DeactivateStock(int id)
    {
        var success = await _stockService.DeactivateStockAsync(id);
        if (!success)
            return NotFound(new { message = "Stock not found." });

        return Ok(new { message = "Stock deactivated successfully." });
    }
}
