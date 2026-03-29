using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/contract-notes")]
[Authorize]
public class ContractNoteController : ControllerBase
{
    private readonly IContractNoteService _svc;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public ContractNoteController(IContractNoteService svc, AppDbContext db, ITenantContext tenant)
    {
        _svc = svc; _db = db; _tenant = tenant;
    }

    private int GetUserId() => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // POST /api/contract-notes/generate/{orderId}
    [HttpPost("generate/{orderId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Trader,CCD")]
    public async Task<IActionResult> Generate(int orderId)
    {
        var result = await _svc.GenerateContractNoteAsync(orderId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/contract-notes/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _svc.GetContractNoteAsync(id);
        return result.Success ? Ok(result.ContractNote) : NotFound(result);
    }

    // GET /api/contract-notes/order/{orderId}
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        var cn = await _db.ContractNotes
            .FirstOrDefaultAsync(c => c.OrderId == orderId && !c.IsVoid);
        if (cn == null) return NotFound(new { message = "No contract note for this order." });
        var result = await _svc.GetContractNoteAsync(cn.Id);
        return Ok(result.ContractNote);
    }

    // GET /api/contract-notes/client/{clientId}
    [HttpGet("client/{clientId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Trader,CCD")]
    public async Task<IActionResult> GetByClient(int clientId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var notes = await _svc.GetContractNotesByClientAsync(clientId, from, to);
        return Ok(notes);
    }

    // GET /api/contract-notes/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var notes = await _svc.GetContractNotesByClientAsync(GetUserId(), from, to);
        return Ok(notes);
    }

    // GET /api/contract-notes/date/{date}
    [HttpGet("date/{date}")]
    [Authorize(Roles = "SuperAdmin,Admin,Trader,CCD")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var notes = await _svc.GetContractNotesByDateAsync(date);
        return Ok(notes);
    }

    // POST /api/contract-notes/regenerate/{orderId}
    [HttpPost("regenerate/{orderId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Regenerate(int orderId)
    {
        var result = await _svc.RegenerateContractNoteAsync(orderId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/contract-notes/{id}/void
    [HttpPost("{id}/void")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Void(int id, [FromBody] VoidContractNoteRequest req)
    {
        var cn = await _db.ContractNotes.FindAsync(id);
        if (cn == null) return NotFound();
        if (cn.IsVoid) return BadRequest(new { message = "Contract note already voided." });

        cn.IsVoid     = true;
        cn.VoidedAt   = DateTime.UtcNow;
        cn.VoidReason = req.Reason;
        cn.Status     = "Voided";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Contract note voided.", id, reason = req.Reason });
    }

    // GET /api/contract-notes/{id}/export
    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(int id)
    {
        var bytes = await _svc.ExportContractNotePdfAsync(id);
        if (bytes.Length == 0) return NotFound();
        return File(bytes, "text/plain", $"contract-note-{id}.txt");
    }

    // GET /api/contract-notes/stats
    [HttpGet("stats")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime? date)
    {
        var d     = (date ?? DateTime.UtcNow).Date;
        var start = d;
        var end   = d.AddDays(1);
        var bhId  = _tenant.BrokerageHouseId;

        var todayNotes = await _db.ContractNotes
            .Where(c => c.TradeDate >= start && c.TradeDate < end && !c.IsVoid)
            .ToListAsync();

        var stats = new
        {
            date             = d.ToString("yyyy-MM-dd"),
            totalToday       = todayNotes.Count,
            buyCount         = todayNotes.Count(c => c.Side.ToUpper() == "BUY"),
            sellCount        = todayNotes.Count(c => c.Side.ToUpper() == "SELL"),
            totalGrossValue  = todayNotes.Sum(c => c.GrossAmount),
            totalNetValue    = todayNotes.Sum(c => c.NetAmount),
            totalCommission  = todayNotes.Sum(c => c.CommissionAmount),
            totalVat         = todayNotes.Sum(c => c.VatOnCommission),
            voidedToday      = await _db.ContractNotes.CountAsync(c =>
                c.IsVoid && c.VoidedAt.HasValue &&
                c.VoidedAt.Value >= start && c.VoidedAt.Value < end),
            allTimeTotal     = await _db.ContractNotes.CountAsync(c => !c.IsVoid),
        };
        return Ok(stats);
    }

    // POST /api/contract-notes/generate-bulk
    [HttpPost("generate-bulk")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GenerateBulk([FromBody] BulkGenerateRequest req)
    {
        var results = new List<object>();
        foreach (var orderId in req.OrderIds)
        {
            var result = await _svc.GenerateContractNoteAsync(orderId);
            results.Add(new { orderId, result.Success, result.Message,
                contractNoteNumber = result.ContractNote?.ContractNoteNumber });
        }
        return Ok(new {
            total   = results.Count,
            success = results.Count(r => (bool)r.GetType().GetProperty("Success")!.GetValue(r)!),
            results
        });
    }
}

public record VoidContractNoteRequest(string Reason);
public record BulkGenerateRequest(List<int> OrderIds);
