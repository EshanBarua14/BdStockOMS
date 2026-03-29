using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CorporateActionController : ControllerBase
{
    private readonly ICorporateActionService _service;
    public CorporateActionController(ICorporateActionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? stockId, [FromQuery] bool? isProcessed)
    {
        var result = await _service.GetAllAsync(stockId, isProcessed);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("stock/{stockId:int}")]
    public async Task<IActionResult> GetByStock(int stockId)
    {
        var result = await _service.GetByStockAsync(stockId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCorporateActionDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCorporateActionDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:int}/mark-processed")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> MarkProcessed(int id)
    {
        var result = await _service.MarkProcessedAsync(id);
        return result.IsSuccess
            ? Ok(new { message = "Corporate action marked as processed." })
            : BadRequest(result.Error);
    }

    [HttpPost("{id:int}/process")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Process(int id)
    {
        var result = await _service.ProcessAsync(id);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:int}/ledger")]
    public async Task<IActionResult> GetLedger(int id)
    {
        var result = await _service.GetLedgerAsync(id);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
