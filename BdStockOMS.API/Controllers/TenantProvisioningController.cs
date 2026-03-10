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
    public class TenantProvisioningController : ControllerBase
    {
        private readonly ITenantProvisioningService _service;

        public TenantProvisioningController(ITenantProvisioningService service)
        {
            _service = service;
        }

        [HttpPost("provision")]
        public async Task<IActionResult> Provision([FromBody] ProvisionTenantRequest request)
        {
            if (request.BrokerageHouseId <= 0)
                return BadRequest("Valid BrokerageHouseId is required.");
            var result = await _service.ProvisionTenantAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{brokerageHouseId}/activate")]
        public async Task<IActionResult> Activate(int brokerageHouseId)
        {
            var result = await _service.ActivateTenantAsync(brokerageHouseId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{brokerageHouseId}/deactivate")]
        public async Task<IActionResult> Deactivate(int brokerageHouseId)
        {
            var result = await _service.DeactivateTenantAsync(brokerageHouseId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{brokerageHouseId}/migrate")]
        public async Task<IActionResult> RunMigrations(int brokerageHouseId)
        {
            var result = await _service.RunMigrationsAsync(brokerageHouseId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{brokerageHouseId}/health")]
        public async Task<IActionResult> GetHealth(int brokerageHouseId)
        {
            var result = await _service.GetTenantHealthAsync(brokerageHouseId);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllTenants()
        {
            var result = await _service.GetAllTenantSummariesAsync();
            return Ok(result);
        }
    }
}
