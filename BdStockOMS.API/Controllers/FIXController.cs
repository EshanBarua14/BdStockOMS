using BdStockOMS.API.Data;
using BdStockOMS.API.FIX;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/fix")]
[Authorize(Roles = "SuperAdmin,Admin,Trader")]
public class FIXController : ControllerBase
{
    private readonly IFIXConnectorFactory _factory;
    private readonly IFIXOrderService     _fixOrders;
    private readonly AppDbContext         _db;
    private readonly ITenantContext       _tenant;

    public FIXController(IFIXConnectorFactory factory, IFIXOrderService fixOrders,
        AppDbContext db, ITenantContext tenant)
    {
        _factory   = factory;
        _fixOrders = fixOrders;
        _db        = db;
        _tenant    = tenant;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string exchange = "DSE")
    {
        var connector = _factory.GetConnector(exchange);
        var status    = await connector.GetSessionStatusAsync();
        return Ok(new
        {
            exchange,
            connectorName = connector.ConnectorName,
            isSimulated   = connector.IsSimulated,
            sessionState  = connector.SessionState.ToString(),
            isRealConfigured = _factory.IsRealFIXConfigured(exchange),
            status
        });
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromQuery] string exchange = "DSE")
    {
        var connector = _factory.GetConnector(exchange);
        await connector.ConnectAsync();
        return Ok(new { message = $"Connected to {exchange} FIX session.", state = connector.SessionState.ToString() });
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect([FromQuery] string exchange = "DSE")
    {
        var connector = _factory.GetConnector(exchange);
        await connector.DisconnectAsync();
        return Ok(new { message = $"Disconnected from {exchange} FIX session." });
    }

    [HttpPost("orders/{orderId}/submit")]
    public async Task<IActionResult> SubmitOrder(int orderId)
    {
        var result = await _fixOrders.PlaceViaFIXAsync(orderId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("orders/{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var result = await _fixOrders.CancelViaFIXAsync(orderId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("orders/{orderId}/amend")]
    public async Task<IActionResult> AmendOrder(int orderId, [FromBody] FIXAmendRequest req)
    {
        var result = await _fixOrders.AmendViaFIXAsync(orderId, req.Quantity, req.Price);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? direction = null,
        [FromQuery] string? msgType = null)
    {
        var q = _db.FIXMessageLogs
            .Where(m => m.BrokerageHouseId == _tenant.BrokerageHouseId);

        if (!string.IsNullOrEmpty(direction)) q = q.Where(m => m.Direction == direction.ToUpper());
        if (!string.IsNullOrEmpty(msgType))   q = q.Where(m => m.MsgType == msgType);

        var total = await q.CountAsync();
        var msgs  = await q.OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { total, page, pageSize, items = msgs });
    }

    [HttpGet("messages/{id}/raw")]
    public async Task<IActionResult> GetRawMessage(int id)
    {
        var msg = await _db.FIXMessageLogs
            .FirstOrDefaultAsync(m => m.Id == id && m.BrokerageHouseId == _tenant.BrokerageHouseId);
        if (msg == null) return NotFound();
        return Ok(new { id = msg.Id, raw = msg.RawMessage.Replace("|", ""),
            msgType = msg.MsgType, direction = msg.Direction, sentAt = msg.SentAt });
    }
}

public record FIXAmendRequest(int Quantity, decimal? Price);
