using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;
using System.Text;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
    public class AuditComplianceController : ControllerBase
    {
        private readonly IAuditComplianceService _svc;

        public AuditComplianceController(IAuditComplianceService svc)
        {
            _svc = svc;
        }

        // GET api/auditcompliance/logs
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] AuditLogFilter filter)
        {
            var logs = await _svc.GetLogsAsync(filter);
            var total = await _svc.CountLogsAsync(filter);
            return Ok(new { total, page = filter.Page, pageSize = filter.PageSize, data = logs });
        }

        // GET api/auditcompliance/logs/export
        [HttpGet("logs/export")]
        public async Task<IActionResult> ExportCsv([FromQuery] AuditLogFilter filter)
        {
            var csv = await _svc.ExportCsvAsync(filter);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"audit-log-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // GET api/auditcompliance/suspicious
        [HttpGet("suspicious")]
        [Authorize(Roles = "SuperAdmin,Compliance")]
        public async Task<IActionResult> GetSuspiciousActivity()
        {
            var results = await _svc.DetectSuspiciousActivityAsync();
            return Ok(results);
        }
    }
}
