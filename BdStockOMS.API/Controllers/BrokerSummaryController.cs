using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BrokerSummaryController : ControllerBase
    {
        private readonly IBrokerSummaryService _svc;

        public BrokerSummaryController(IBrokerSummaryService svc)
        {
            _svc = svc;
        }

        [HttpGet("all")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? date)
        {
            var result = await _svc.GetAllBrokerSummariesAsync(date ?? DateTime.UtcNow);
            return Ok(result);
        }

        [HttpGet("{brokerageHouseId}")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin")]
        public async Task<IActionResult> GetSummary(int brokerageHouseId, [FromQuery] DateTime? date)
        {
            try
            {
                var result = await _svc.GetBrokerSummaryAsync(brokerageHouseId, date ?? DateTime.UtcNow);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{brokerageHouseId}/top-traders/value")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin")]
        public async Task<IActionResult> GetTopByValue(int brokerageHouseId, [FromQuery] DateTime? date, [FromQuery] int top = 10)
        {
            var result = await _svc.GetTopTradersByValueAsync(brokerageHouseId, date ?? DateTime.UtcNow, top);
            return Ok(result);
        }

        [HttpGet("{brokerageHouseId}/top-traders/buy")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin")]
        public async Task<IActionResult> GetTopByBuy(int brokerageHouseId, [FromQuery] DateTime? date, [FromQuery] int top = 10)
        {
            var result = await _svc.GetTopTradersByBuyAsync(brokerageHouseId, date ?? DateTime.UtcNow, top);
            return Ok(result);
        }

        [HttpGet("{brokerageHouseId}/top-traders/sell")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin")]
        public async Task<IActionResult> GetTopBySell(int brokerageHouseId, [FromQuery] DateTime? date, [FromQuery] int top = 10)
        {
            var result = await _svc.GetTopTradersBySellAsync(brokerageHouseId, date ?? DateTime.UtcNow, top);
            return Ok(result);
        }

        [HttpGet("trader/{traderId}/clients")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin,Trader")]
        public async Task<IActionResult> GetClientActivity(int traderId, [FromQuery] DateTime? date)
        {
            try
            {
                var result = await _svc.GetClientActivityAsync(traderId, date ?? DateTime.UtcNow);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{brokerageHouseId}/top-clients")]
        [Authorize(Roles = "SuperAdmin,Admin,BHAdmin")]
        public async Task<IActionResult> GetTopClients(int brokerageHouseId, [FromQuery] DateTime? date, [FromQuery] int top = 10)
        {
            var result = await _svc.GetTopClientsByValueAsync(brokerageHouseId, date ?? DateTime.UtcNow, top);
            return Ok(result);
        }
    }
}
