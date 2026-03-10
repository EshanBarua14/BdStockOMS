using System;
using System.Threading.Tasks;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class BosComplianceController : ControllerBase
    {
        private readonly IFlextradeBosComplianceService _service;

        public BosComplianceController(IFlextradeBosComplianceService service)
        {
            _service = service;
        }

        [HttpGet("{brokerageHouseId}")]
        public async Task<IActionResult> GetCompliance(int brokerageHouseId)
        {
            var result = await _service.GetCachedComplianceReportAsync(brokerageHouseId);
            return Ok(result);
        }

        [HttpPost("{brokerageHouseId}/refresh")]
        public async Task<IActionResult> RefreshCompliance(int brokerageHouseId)
        {
            var result = await _service.RunComplianceCheckAsync(brokerageHouseId);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllCompliance()
        {
            var result = await _service.GetAllBrokerageComplianceAsync();
            return Ok(result);
        }

        [HttpPost("refresh-all")]
        public async Task<IActionResult> RefreshAll()
        {
            await _service.RefreshAllAsync();
            return Ok(new { Message = "BOS compliance refresh triggered for all brokerages." });
        }
    }
}
