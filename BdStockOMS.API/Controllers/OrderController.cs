// Controllers/OrderController.cs
using System.Security.Claims;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/orders — Investor or Trader places an order
    // ─────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Investor,Trader,SuperAdmin,Admin")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var role = GetRole();

        var (order, error) = await _orderService.PlaceOrderAsync(dto, userId, role);
        if (error != null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetOrderById), new { id = order!.Id }, order);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/orders — View orders (role scoped)
    // ─────────────────────────────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Investor,Trader,Admin,CCD,BrokerageHouse,SuperAdmin")]
    public async Task<IActionResult> GetOrders()
    {
        var userId = GetUserId();
        var role = GetRole();

        var orders = await _orderService.GetOrdersAsync(userId, role);
        return Ok(orders);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/orders/{id}
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userId = GetUserId();
        var role = GetRole();

        var order = await _orderService.GetOrderByIdAsync(id, userId, role);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        return Ok(order);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/orders/{id}/execute — Trader executes a pending order
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}/execute")]
    [Authorize(Roles = "Trader")]
    public async Task<IActionResult> ExecuteOrder(int id)
    {
        var traderId = GetUserId();

        var (order, error) = await _orderService.ExecuteOrderAsync(id, traderId);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(order);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/orders/{id}/cancel
    // Investor cancels own order, Trader/Admin can also cancel
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = "Investor,Trader,Admin")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var role = GetRole();

        var (order, error) = await _orderService.CancelOrderAsync(id, userId, role, dto.Reason);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(order);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/orders/portfolio/{investorId}
    // View investor portfolio
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("portfolio/{investorId:int}")]
    [Authorize(Roles = "Investor,Trader,Admin,CCD,BrokerageHouse")]
    public async Task<IActionResult> GetPortfolio(int investorId)
    {
        var userId = GetUserId();
        var role = GetRole();

        // Investor can only see their own portfolio
        if (role == "Investor" && userId != investorId)
            return Forbid();

        var portfolio = await _orderService.GetPortfolioAsync(investorId);
        return Ok(portfolio);
    }

    // ─── Helpers ─────────────────────────────────────────────────────
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
