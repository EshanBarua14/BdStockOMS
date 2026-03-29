using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/settlement")]
[Authorize]
public class SettlementController : ControllerBase
{
    private readonly ISettlementService _settlement;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public SettlementController(ISettlementService settlement, AppDbContext db, ITenantContext tenant)
    {
        _settlement = settlement; _db = db; _tenant = tenant;
    }

    private int GetUserId() => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    // GET /api/settlement/batches
    [HttpGet("batches")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetBatches(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var q = _db.SettlementBatches
            .Where(b => b.BrokerageHouseId == _tenant.BrokerageHouseId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<SettlementBatchStatus>(status, true, out var s))
            q = q.Where(b => b.Status == s);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(b => b.SettlementDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new {
                b.Id, b.Exchange, b.TradeDate, b.SettlementDate,
                b.Status, b.TotalTrades, b.TotalBuyValue,
                b.TotalSellValue, b.NetObligations, b.CreatedAt, b.ProcessedAt
            }).ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/settlement/batches/{id}
    [HttpGet("batches/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetBatch(int id)
    {
        var batch = await _db.SettlementBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id && b.BrokerageHouseId == _tenant.BrokerageHouseId);
        return batch == null ? NotFound() : Ok(batch);
    }

    // POST /api/settlement/batches/create
    [HttpPost("batches/create")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest req)
    {
        var batch = await _settlement.CreateBatchAsync(
            _tenant.BrokerageHouseId, req.Exchange, req.TradeDate);
        return Ok(batch);
    }

    // POST /api/settlement/batches/{id}/process
    [HttpPost("batches/{id}/process")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> ProcessBatch(int id)
    {
        var batch = await _db.SettlementBatches
            .FirstOrDefaultAsync(b => b.Id == id && b.BrokerageHouseId == _tenant.BrokerageHouseId);
        if (batch == null) return NotFound();
        if (batch.Status == SettlementBatchStatus.Completed)
            return BadRequest(new { message = "Batch already completed." });

        var result = await _settlement.ProcessBatchAsync(id);
        return Ok(result);
    }

    // POST /api/settlement/batches/{id}/retry
    [HttpPost("batches/{id}/retry")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RetryBatch(int id)
    {
        var batch = await _db.SettlementBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id && b.BrokerageHouseId == _tenant.BrokerageHouseId);
        if (batch == null) return NotFound();

        // Reset failed items to pending
        foreach (var item in batch.Items.Where(i => i.Status == SettlementItemStatus.Failed))
        {
            item.Status        = SettlementItemStatus.Pending;
            item.FailureReason = null;
        }
        batch.Status = SettlementBatchStatus.Pending;
        await _db.SaveChangesAsync();

        var result = await _settlement.ProcessBatchAsync(id);
        return Ok(result);
    }

    // GET /api/settlement/pending
    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetPending()
    {
        var batches = await _settlement.GetPendingBatchesAsync();
        return Ok(batches);
    }

    // GET /api/settlement/calculate-date
    [HttpGet("calculate-date")]
    public IActionResult CalculateDate(
        [FromQuery] string tradeDate,
        [FromQuery] string type = "T2")
    {
        if (!DateTime.TryParse(tradeDate, out var date))
            return BadRequest(new { message = "Invalid date format." });

        var settlType = type.ToUpper() == "T0" ? SettlementType.T0 : SettlementType.T2;
        var settlDate = _settlement.CalculateSettlementDate(date, settlType);
        return Ok(new {
            tradeDate    = date.ToString("yyyy-MM-dd"),
            settlementDate = settlDate.ToString("yyyy-MM-dd"),
            type         = settlType.ToString(),
            businessDays = (settlDate - date.Date).Days,
        });
    }

    // GET /api/settlement/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMySettlements(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var q = _db.SettlementItems
            .Where(i => i.InvestorId == userId && i.BrokerageHouseId == _tenant.BrokerageHouseId)
            .OrderByDescending(i => i.SettlementDate);

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/settlement/stats
    [HttpGet("stats")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetStats()
    {
        var bhId = _tenant.BrokerageHouseId;
        var today = DateTime.UtcNow.Date;

        var stats = new
        {
            pendingBatches    = await _db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == bhId && b.Status == SettlementBatchStatus.Pending),
            processingBatches = await _db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == bhId && b.Status == SettlementBatchStatus.Processing),
            completedToday    = await _db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == bhId && b.Status == SettlementBatchStatus.Completed && b.ProcessedAt.HasValue && b.ProcessedAt.Value.Date == today),
            failedBatches     = await _db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == bhId && b.Status == SettlementBatchStatus.Failed),
            totalSettledValue = await _db.SettlementItems.Where(i => i.BrokerageHouseId == bhId && i.Status == SettlementItemStatus.Settled).SumAsync(i => (decimal?)i.TradeValue) ?? 0m,
            pendingItems      = await _db.SettlementItems.CountAsync(i => i.BrokerageHouseId == bhId && i.Status == SettlementItemStatus.Pending),
            dueTodayBatches   = await _db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == bhId && b.Status == SettlementBatchStatus.Pending && b.SettlementDate.Date <= today),
        };
        return Ok(stats);
    }
}

public record CreateBatchRequest(string Exchange, DateTime TradeDate);
