using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PortfolioSnapshotController : ControllerBase
    {
        private readonly IPortfolioSnapshotService _svc;

        public PortfolioSnapshotController(IPortfolioSnapshotService svc)
        {
            _svc = svc;
        }

        [HttpPost("capture/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Capture(int userId)
        {
            var snapshot = await _svc.CaptureSnapshotAsync(userId, DateTime.UtcNow);
            return Ok(snapshot);
        }

        [HttpPost("capture-all")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CaptureAll()
        {
            var count = await _svc.CaptureAllSnapshotsAsync(DateTime.UtcNow);
            return Ok(new { captured = count });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var history = await _svc.GetSnapshotHistoryAsync(userId, from, to);
            return Ok(history);
        }

        [HttpGet("latest/{userId}")]
        public async Task<IActionResult> GetLatest(int userId)
        {
            var snapshot = await _svc.GetLatestSnapshotAsync(userId);
            if (snapshot == null) return NotFound();
            return Ok(snapshot);
        }

        [HttpGet("roi/{userId}")]
        public async Task<IActionResult> GetRoi(int userId)
        {
            var roi = await _svc.CalculateRoiAsync(userId);
            return Ok(new { userId, roiPercent = roi });
        }

        [HttpPost("analytics")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpsertAnalytics([FromBody] StockAnalyticsResult data)
        {
            var result = await _svc.UpsertStockAnalyticsAsync(data);
            return Ok(result);
        }

        [HttpGet("analytics/{stockId}/{exchange}")]
        public async Task<IActionResult> GetAnalytics(int stockId, string exchange)
        {
            var result = await _svc.GetStockAnalyticsAsync(stockId, exchange);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("analytics/{exchange}")]
        public async Task<IActionResult> GetAllAnalytics(string exchange)
        {
            var result = await _svc.GetAllAnalyticsAsync(exchange);
            return Ok(result);
        }
    }
}
