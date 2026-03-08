using BdStockOMS.API.DTOs.SystemSettings;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemSettingController : ControllerBase
{
    private readonly ISystemSettingService _service;

    public SystemSettingController(ISystemSettingService service)
    {
        _service = service;
    }

    // Any authenticated user can read settings (e.g. to check market hours)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category)
    {
        var result = await _service.GetAllAsync(category);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // Get one setting by key
    [HttpGet("{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var result = await _service.GetByKeyAsync(key);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // Only SuperAdmin can create new settings
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateSystemSettingDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(userId, dto);
        return result.IsSuccess ? CreatedAtAction(nameof(GetByKey), new { key = result.Value!.Key }, result.Value) : BadRequest(result.Error);
    }

    // SuperAdmin and Admin can update settings
    [HttpPut("{key}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateSystemSettingDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.UpdateAsync(userId, key, dto);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // Only SuperAdmin can delete settings
    [HttpDelete("{key}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(string key)
    {
        var result = await _service.DeleteAsync(key);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
