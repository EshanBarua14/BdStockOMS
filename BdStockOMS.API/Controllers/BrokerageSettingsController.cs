using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BrokerageSettingsController : ControllerBase
    {
        private readonly IBrokerageSettingsService _svc;

        public BrokerageSettingsController(IBrokerageSettingsService svc)
        {
            _svc = svc;
        }

        [HttpGet("{brokerageHouseId}")]
        public async Task<IActionResult> GetSettings(int brokerageHouseId)
        {
            var settings = await _svc.GetOrCreateSettingsAsync(brokerageHouseId);
            return Ok(settings);
        }

        [HttpPut("{brokerageHouseId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateSettings(int brokerageHouseId, [FromBody] UpdateSettingsRequest request)
        {
            var settings = await _svc.UpdateSettingsAsync(brokerageHouseId, request);
            return Ok(settings);
        }

        [HttpGet("{brokerageHouseId}/feature/{featureName}")]
        public async Task<IActionResult> IsFeatureEnabled(int brokerageHouseId, string featureName)
        {
            var enabled = await _svc.IsFeatureEnabledAsync(brokerageHouseId, featureName);
            return Ok(new { brokerageHouseId, featureName, enabled });
        }

        [HttpGet("{brokerageHouseId}/trading-hours")]
        public async Task<IActionResult> IsWithinTradingHours(int brokerageHouseId)
        {
            var within = await _svc.IsWithinTradingHoursAsync(brokerageHouseId);
            return Ok(new { brokerageHouseId, isWithinTradingHours = within });
        }

        [HttpPost("branches")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
        {
            try
            {
                var branch = await _svc.CreateBranchAsync(request);
                return Ok(branch);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("branches/{branchId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateBranch(int branchId, [FromBody] CreateBranchRequest request)
        {
            try
            {
                var branch = await _svc.UpdateBranchAsync(branchId, request);
                return Ok(branch);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("branches/{branchId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeactivateBranch(int branchId)
        {
            var result = await _svc.DeactivateBranchAsync(branchId);
            if (!result) return NotFound();
            return Ok(new { deactivated = true });
        }

        [HttpGet("{brokerageHouseId}/branches")]
        public async Task<IActionResult> GetBranches(int brokerageHouseId)
        {
            var branches = await _svc.GetBranchesAsync(brokerageHouseId);
            return Ok(branches);
        }

        [HttpGet("branches/{branchId}")]
        public async Task<IActionResult> GetBranch(int branchId)
        {
            var branch = await _svc.GetBranchByIdAsync(branchId);
            if (branch == null) return NotFound();
            return Ok(branch);
        }
    }
}
