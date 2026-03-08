using System.Security.Claims;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/watchlists")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    // GET /api/watchlists
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _watchlistService.GetMyWatchlistsAsync(GetUserId());
        return Ok(result);
    }

    // GET /api/watchlists/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _watchlistService.GetWatchlistAsync(id, GetUserId());
        if (!result.IsSuccess)
            return NotFound(new { message = result.Error });
        return Ok(result.Value);
    }

    // POST /api/watchlists
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWatchlistDto dto)
    {
        var result = await _watchlistService.CreateWatchlistAsync(GetUserId(), dto.Name);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value!.Id },
            new { message = "Watchlist created.", watchlistId = result.Value.Id });
    }

    // PUT /api/watchlists/{id}/rename
    [HttpPut("{id}/rename")]
    public async Task<IActionResult> Rename(int id, [FromBody] RenameWatchlistDto dto)
    {
        var result = await _watchlistService.RenameWatchlistAsync(id, GetUserId(), dto.Name);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return Ok(new { message = "Watchlist renamed." });
    }

    // DELETE /api/watchlists/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _watchlistService.DeleteWatchlistAsync(id, GetUserId());
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return Ok(new { message = "Watchlist deleted." });
    }

    // POST /api/watchlists/{id}/stocks
    [HttpPost("{id}/stocks")]
    public async Task<IActionResult> AddStock(int id, [FromBody] AddStockDto dto)
    {
        var result = await _watchlistService.AddStockAsync(id, GetUserId(), dto.StockId);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return Ok(new { message = "Stock added to watchlist." });
    }

    // DELETE /api/watchlists/{id}/stocks/{stockId}
    [HttpDelete("{id}/stocks/{stockId}")]
    public async Task<IActionResult> RemoveStock(int id, int stockId)
    {
        var result = await _watchlistService.RemoveStockAsync(id, GetUserId(), stockId);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return Ok(new { message = "Stock removed from watchlist." });
    }

    // PUT /api/watchlists/{id}/reorder
    [HttpPut("{id}/reorder")]
    public async Task<IActionResult> Reorder(int id, [FromBody] List<ReorderItemDto> items)
    {
        var order = items.Select(i => (i.StockId, i.SortOrder)).ToList();
        var result = await _watchlistService.ReorderStocksAsync(id, GetUserId(), order);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });
        return Ok(new { message = "Watchlist reordered." });
    }
}

public record CreateWatchlistDto(string Name);
public record RenameWatchlistDto(string Name);
public record AddStockDto(int StockId);
public record ReorderItemDto(int StockId, int SortOrder);
