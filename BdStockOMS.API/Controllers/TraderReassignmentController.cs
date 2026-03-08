using BdStockOMS.API.DTOs.TraderReassignment;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TraderReassignmentController : ControllerBase
{
    private readonly ITraderReassignmentService _service;

    public TraderReassignmentController(ITraderReassignmentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = "CCD,Admin,SuperAdmin")]
    public async Task<IActionResult> Reassign([FromBody] CreateTraderReassignmentDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ReassignTraderAsync(userId, dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("investor/{investorId:int}")]
    [Authorize(Roles = "CCD,Admin,SuperAdmin,Trader")]
    public async Task<IActionResult> GetByInvestor(int investorId)
    {
        var result = await _service.GetByInvestorAsync(investorId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("brokerage/{brokerageHouseId:int}")]
    [Authorize(Roles = "CCD,Admin,SuperAdmin")]
    public async Task<IActionResult> GetByBrokerageHouse(int brokerageHouseId)
    {
        var result = await _service.GetByBrokerageHouseAsync(brokerageHouseId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
