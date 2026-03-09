using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileImportController : ControllerBase
    {
        private readonly IFileImportService _svc;

        public FileImportController(IFileImportService svc)
        {
            _svc = svc;
        }

        [HttpPost("stage")]
        [Authorize(Roles = "CCD,SuperAdmin,Admin")]
        public async Task<IActionResult> Stage([FromBody] FileImportRequest request)
        {
            var batch = await _svc.StageAsync(request);
            return Ok(batch);
        }

        [HttpPost("validate/{batchId}")]
        [Authorize(Roles = "CCD,SuperAdmin,Admin")]
        public async Task<IActionResult> Validate(int batchId)
        {
            try
            {
                var summary = await _svc.ValidateAsync(batchId);
                return Ok(summary);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("approve/{batchId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Approve(int batchId, [FromQuery] int approverUserId)
        {
            try
            {
                var batch = await _svc.ApproveAsync(batchId, approverUserId);
                return Ok(batch);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("reject/{batchId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Reject(int batchId, [FromQuery] int approverUserId, [FromQuery] string reason)
        {
            try
            {
                var batch = await _svc.RejectAsync(batchId, approverUserId, reason);
                return Ok(batch);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("commit/{batchId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Commit(int batchId)
        {
            try
            {
                var committed = await _svc.CommitAsync(batchId);
                return Ok(new { committedRows = committed });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("{batchId}")]
        public async Task<IActionResult> GetBatch(int batchId)
        {
            var batch = await _svc.GetBatchAsync(batchId);
            if (batch == null) return NotFound();
            return Ok(batch);
        }

        [HttpGet("brokerage/{brokerageHouseId}")]
        public async Task<IActionResult> GetByBrokerageHouse(int brokerageHouseId)
        {
            var batches = await _svc.GetBatchesByBrokerageHouseAsync(brokerageHouseId);
            return Ok(batches);
        }

        [HttpGet("{batchId}/rows")]
        public async Task<IActionResult> GetRows(int batchId)
        {
            var rows = await _svc.GetRowsAsync(batchId);
            return Ok(rows);
        }
    }
}
