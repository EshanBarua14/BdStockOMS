using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _service;

    public MarketDataController(IMarketDataService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] MarketDataQueryDto query)
    {
        var result = await _service.GetAllAsync(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("stock/{stockId:int}/{exchange}")]
    public async Task<IActionResult> GetByStock(int stockId, string exchange, [FromQuery] int days = 30)
    {
        var result = await _service.GetByStockAsync(stockId, exchange, days);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,ITSupport")]
    public async Task<IActionResult> Create([FromBody] CreateMarketDataDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(result.Error);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin,ITSupport")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkMarketDataDto dto)
    {
        var result = await _service.BulkCreateAsync(dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
