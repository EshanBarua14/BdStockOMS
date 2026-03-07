// Controllers/TraderInvestorController.cs
using System.Security.Claims;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/traders")]
[Authorize]
public class TraderInvestorController : ControllerBase
{
    private readonly AppDbContext _db;

    public TraderInvestorController(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/traders/{traderId}/investors/{investorId}
    // Admin assigns an investor to a trader
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("{traderId:int}/investors/{investorId:int}")]
    [Authorize(Roles = "Admin,BrokerageHouse")]
    public async Task<IActionResult> AssignInvestor(int traderId, int investorId)
    {
        var brokerageHouseId = GetBrokerageHouseId();

        var trader = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == traderId &&
                u.Role.Name == "Trader" &&
                u.BrokerageHouseId == brokerageHouseId &&
                u.IsActive);

        if (trader == null)
            return NotFound(new { message = "Trader not found in your brokerage." });

        var investor = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == investorId &&
                u.Role.Name == "Investor" &&
                u.BrokerageHouseId == brokerageHouseId &&
                u.IsActive);

        if (investor == null)
            return NotFound(new { message = "Investor not found in your brokerage." });

        if (investor.AssignedTraderId != null)
            return BadRequest(new { message = $"Investor is already assigned to a trader (TraderId: {investor.AssignedTraderId}). Remove first." });

        investor.AssignedTraderId = traderId;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Investor '{investor.FullName}' assigned to Trader '{trader.FullName}'." });
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE /api/traders/{traderId}/investors/{investorId}
    // Admin removes investor from trader
    // ─────────────────────────────────────────────────────────────────
    [HttpDelete("{traderId:int}/investors/{investorId:int}")]
    [Authorize(Roles = "Admin,BrokerageHouse")]
    public async Task<IActionResult> RemoveInvestor(int traderId, int investorId)
    {
        var brokerageHouseId = GetBrokerageHouseId();

        var investor = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Id == investorId &&
                u.AssignedTraderId == traderId &&
                u.BrokerageHouseId == brokerageHouseId &&
                u.IsActive);

        if (investor == null)
            return NotFound(new { message = "Investor not found or not assigned to this trader." });

        investor.AssignedTraderId = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Investor '{investor.FullName}' removed from trader." });
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/traders/{traderId}/investors
    // List all investors assigned to a trader
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{traderId:int}/investors")]
    [Authorize(Roles = "Admin,BrokerageHouse,Trader")]
    public async Task<IActionResult> GetAssignedInvestors(int traderId)
    {
        var userId = GetUserId();
        var role = GetRole();

        // Trader can only see their own investors
        if (role == "Trader" && userId != traderId)
            return Forbid();

        var investors = await _db.Users
            .Include(u => u.Role)
            .Where(u =>
                u.AssignedTraderId == traderId &&
                u.IsActive)
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.Name,
                BrokerageHouseId = u.BrokerageHouseId,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(investors);
    }

    // ─── Helpers ─────────────────────────────────────────────────────
    private int GetBrokerageHouseId()
    {
        var claim = User.FindFirst("BrokerageHouseId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
    }
}
