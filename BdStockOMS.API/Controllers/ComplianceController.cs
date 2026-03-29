using System;
using System.Threading.Tasks;
using BdStockOMS.API.Authorization;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/compliance")]
    [Authorize]
    public class ComplianceController : ControllerBase
    {
        private readonly IComplianceService _svc;
        public ComplianceController(IComplianceService svc) => _svc = svc;

        [HttpPost("scan/order/{orderId:int}")]
        [RequirePermission(Permissions.ComplianceManage)]
        public async Task<IActionResult> ScanOrder(int orderId, [FromServices] AppDbContext db)
        {
            var order = await db.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            var reports = await _svc.ScanOrderAsync(order);
            return Ok(reports);
        }

        [HttpPost("scan/investor/{investorId:int}")]
        [RequirePermission(Permissions.ComplianceManage)]
        public async Task<IActionResult> ScanInvestor(int investorId,
            [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var reports = await _svc.ScanTradeHistoryAsync(investorId, from, to);
            return Ok(reports);
        }

        [HttpGet("reports")]
        [RequirePermission(Permissions.ComplianceView)]
        public async Task<IActionResult> GetReports([FromQuery] ComplianceFilterDto filter)
            => Ok(await _svc.GetReportsAsync(filter));

        [HttpGet("reports/{id:guid}")]
        [RequirePermission(Permissions.ComplianceView)]
        public async Task<IActionResult> GetReport(Guid id)
        {
            var report = await _svc.GetReportAsync(id);
            return report == null ? NotFound() : Ok(report);
        }

        [HttpPut("reports/{id:guid}/resolve")]
        [RequirePermission(Permissions.ComplianceManage)]
        public async Task<IActionResult> ResolveReport(Guid id, [FromBody] ResolveComplianceDto dto)
        {
            try   { return Ok(await _svc.ResolveReportAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpPut("reports/{id:guid}/escalate")]
        [RequirePermission(Permissions.ComplianceManage)]
        public async Task<IActionResult> EscalateReport(Guid id, [FromBody] EscalateComplianceDto dto)
        {
            try   { return Ok(await _svc.EscalateReportAsync(id, dto.Reason, dto.EscalatedBy)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [HttpGet("summary")]
        [RequirePermission(Permissions.ComplianceView)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int brokerageHouseId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
            => Ok(await _svc.GetSummaryAsync(brokerageHouseId, from, to));

        [HttpGet("export")]
        [RequirePermission(Permissions.ComplianceView)]
        public async Task<IActionResult> Export([FromQuery] ComplianceExportDto dto)
        {
            var bytes = await _svc.ExportReportsAsync(dto);
            return File(bytes, "text/csv",
                $"compliance-{dto.From:yyyyMMdd}-{dto.To:yyyyMMdd}.csv");
        }
    }

    public class EscalateComplianceDto
    {
        public string Reason      { get; set; } = string.Empty;
        public string EscalatedBy { get; set; } = string.Empty;
    }
}
