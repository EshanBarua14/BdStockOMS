// Controllers/UserController.cs
using System.Security.Claims;
using BdStockOMS.API.DTOs.User;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]   // All endpoints require a valid JWT
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/users
    // Only BrokerageHouse role can create users
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "BrokerageHouse")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var brokerageHouseId = GetBrokerageHouseId();
        if (brokerageHouseId == null)
            return Unauthorized(new { message = "BrokerageHouseId claim missing." });

        var (user, error) = await _userService.CreateUserAsync(dto, brokerageHouseId.Value);
        if (error != null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetUserById),
            new { id = user!.Id }, user);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/users
    // BrokerageHouse and Admin see their brokerage's users
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "BrokerageHouse,Admin")]
    public async Task<IActionResult> GetUsers()
    {
        var brokerageHouseId = GetBrokerageHouseId();
        if (brokerageHouseId == null)
            return Unauthorized(new { message = "BrokerageHouseId claim missing." });

        var users = await _userService.GetUsersByBrokerageAsync(brokerageHouseId.Value);
        return Ok(users);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/users/{id}
    // BrokerageHouse, Admin, and the user themselves
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    [Authorize(Roles = "BrokerageHouse,Admin,Trader,Investor")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Non-brokerage/admin users can only see themselves
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Trader" or "Investor")
        {
            var myId = GetUserId();
            if (myId != id)
                return Forbid();
        }

        return Ok(user);
    }

    // ─────────────────────────────────────────────────────────────────
    // DELETE /api/users/{id}  (soft delete — sets IsActive = false)
    // Only BrokerageHouse can deactivate
    // ─────────────────────────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "BrokerageHouse")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var brokerageHouseId = GetBrokerageHouseId();
        if (brokerageHouseId == null)
            return Unauthorized(new { message = "BrokerageHouseId claim missing." });

        var success = await _userService.DeactivateUserAsync(id, brokerageHouseId.Value);
        if (!success)
            return NotFound(new { message = "User not found or not in your brokerage." });

        return Ok(new { message = "User deactivated successfully." });
    }

    // ─── Helpers ─────────────────────────────────────────────────────
    private int? GetBrokerageHouseId()
    {
        var claim = User.FindFirst("BrokerageHouseId")?.Value;
        return int.TryParse(claim, out int id) ? id : null;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }
}