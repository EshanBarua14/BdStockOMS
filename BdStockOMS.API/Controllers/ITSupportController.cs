using BdStockOMS.API.Data;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/itsupport")]
[Authorize(Roles = "ITSupport,SuperAdmin")]
public class ITSupportController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ITSupportController(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    private string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    // POST /api/itsupport/unlock/{userId}
    [HttpPost("unlock/{userId:int}")]
    public async Task<IActionResult> UnlockAccount(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!user.IsLocked && user.FailedLoginCount == 0)
            return BadRequest(new { message = "Account is not locked." });

        var oldValues = $"IsLocked={user.IsLocked}, FailedLoginCount={user.FailedLoginCount}";

        user.IsLocked          = false;
        user.FailedLoginCount  = 0;
        user.LockoutUntil      = null;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            GetCurrentUserId(), "ACCOUNT_UNLOCKED", "User", userId,
            oldValues, "IsLocked=false, FailedLoginCount=0", GetIpAddress());

        return Ok(new
        {
            message  = $"Account for user {userId} has been unlocked.",
            userId   = userId,
            unlockedAt = DateTime.UtcNow
        });
    }

    // GET /api/itsupport/locked-accounts
    [HttpGet("locked-accounts")]
    public async Task<IActionResult> GetLockedAccounts()
    {
        var locked = await _db.Users
            .Where(u => u.IsLocked)
            .Include(u => u.Role)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role           = u.Role!.Name,
                u.FailedLoginCount,
                u.LockoutUntil,
                u.IsActive
            })
            .ToListAsync();

        return Ok(locked);
    }
}
