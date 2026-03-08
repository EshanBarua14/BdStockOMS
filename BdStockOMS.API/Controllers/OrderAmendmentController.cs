using BdStockOMS.API.DTOs.OrderAmendment;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderAmendmentController : ControllerBase
{
    private readonly IOrderAmendmentService _service;

    public OrderAmendmentController(IOrderAmendmentService service)
    {
        _service = service;
    }

    [HttpPost("{orderId:int}/amend")]
    [Authorize(Roles = "Investor,Trader,Admin,SuperAdmin")]
    public async Task<IActionResult> Amend(int orderId, [FromBody] AmendOrderDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.AmendOrderAsync(orderId, userId, dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{orderId:int}/history")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var result = await _service.GetByOrderAsync(orderId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("my-amendments")]
    public async Task<IActionResult> GetMyAmendments()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetByUserAsync(userId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
