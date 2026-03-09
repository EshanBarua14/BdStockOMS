using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KycController : ControllerBase
    {
        private readonly IKycService _kycService;

        public KycController(IKycService kycService)
        {
            _kycService = kycService;
        }

        [HttpPost("submit")]
        [Authorize(Roles = "Investor")]
        public async Task<IActionResult> Submit([FromBody] KycSubmitRequest request)
        {
            try
            {
                var doc = await _kycService.SubmitDocumentAsync(request);
                return Ok(doc);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("review")]
        [Authorize(Roles = "CCD,SuperAdmin")]
        public async Task<IActionResult> Review([FromBody] KycReviewRequest request)
        {
            try
            {
                var doc = await _kycService.ReviewDocumentAsync(request);
                return Ok(doc);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var docs = await _kycService.GetDocumentsByUserAsync(userId);
            return Ok(docs);
        }

        [HttpGet("pending/{brokerageHouseId}")]
        [Authorize(Roles = "CCD,SuperAdmin")]
        public async Task<IActionResult> GetPending(int brokerageHouseId)
        {
            var docs = await _kycService.GetPendingDocumentsAsync(brokerageHouseId);
            return Ok(docs);
        }

        [HttpGet("status/{userId}")]
        public async Task<IActionResult> GetStatus(int userId)
        {
            var approved = await _kycService.IsKycApprovedAsync(userId);
            return Ok(new { userId, isKycApproved = approved });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var doc = await _kycService.GetDocumentByIdAsync(id);
            if (doc == null) return NotFound();
            return Ok(doc);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var history = await _kycService.GetApprovalHistoryAsync(id);
            return Ok(history);
        }
    }
}
