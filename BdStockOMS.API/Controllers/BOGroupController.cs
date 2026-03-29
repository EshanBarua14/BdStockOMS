using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BdStockOMS.API.Controllers;
[ApiController]
[Route("api/bo-groups")]
[Authorize(Roles = "SuperAdmin,Admin,Trader")]
public class BOGroupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    public BOGroupController(AppDbContext db, ITenantContext tenant) { _db = db; _tenant = tenant; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _db.BOGroups.Include(g => g.Members)
            .Where(g => g.BrokerageHouseId == _tenant.BrokerageHouseId && g.IsActive)
            .Select(g => new { g.Id, g.Name, g.Description, g.IsActive, g.CreatedAt, memberCount = g.Members.Count })
            .ToListAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var g = await _db.BOGroups.Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == id && g.BrokerageHouseId == _tenant.BrokerageHouseId);
        return g == null ? NotFound() : Ok(g);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateBOGroupRequest req)
    {
        var g = new BOGroup { Name = req.Name, Description = req.Description,
            BrokerageHouseId = _tenant.BrokerageHouseId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.BOGroups.Add(g);
        await _db.SaveChangesAsync();
        return Ok(g);
    }

    [HttpPost("{id}/members")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest req)
    {
        if (await _db.BOGroupMembers.AnyAsync(m => m.BOGroupId == id && m.UserId == req.UserId))
            return BadRequest(new { message = "User already in group." });
        _db.BOGroupMembers.Add(new BOGroupMember { BOGroupId = id, UserId = req.UserId });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Member added." });
    }

    [HttpDelete("{id}/members/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var m = await _db.BOGroupMembers.FirstOrDefaultAsync(m => m.BOGroupId == id && m.UserId == userId);
        if (m == null) return NotFound();
        _db.BOGroupMembers.Remove(m);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Member removed." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var g = await _db.BOGroups.FindAsync(id);
        if (g == null || g.BrokerageHouseId != _tenant.BrokerageHouseId) return NotFound();
        g.IsActive = false; g.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Group deactivated." });
    }
}
public record CreateBOGroupRequest(string Name, string? Description);
public record AddMemberRequest(int UserId);
