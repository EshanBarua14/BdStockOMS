using BdStockOMS.API.DTOs.Reports;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,BrokerageHouse,CCD")]
public class BrokerageReportController : ControllerBase
{
    private readonly IBrokerageReportService _service;

    public BrokerageReportController(IBrokerageReportService service)
    {
        _service = service;
    }

    // Order counts and values for a brokerage house in a date range
    [HttpGet("{brokerageHouseId:int}/orders")]
    public async Task<IActionResult> GetOrderSummary(int brokerageHouseId, [FromQuery] ReportQueryDto query)
    {
        var result = await _service.GetOrderSummaryAsync(brokerageHouseId, query);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // Top investors ranked by trading volume
    [HttpGet("{brokerageHouseId:int}/top-investors")]
    public async Task<IActionResult> GetTopInvestors(int brokerageHouseId, [FromQuery] ReportQueryDto query, [FromQuery] int top = 10)
    {
        var result = await _service.GetTopInvestorsAsync(brokerageHouseId, query, top);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // Commission earned in a period
    [HttpGet("{brokerageHouseId:int}/commission")]
    public async Task<IActionResult> GetCommissionReport(int brokerageHouseId, [FromQuery] ReportQueryDto query)
    {
        var result = await _service.GetCommissionReportAsync(brokerageHouseId, query);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // Fund request deposit summary
    [HttpGet("{brokerageHouseId:int}/fund-requests")]
    public async Task<IActionResult> GetFundRequestReport(int brokerageHouseId, [FromQuery] ReportQueryDto query)
    {
        var result = await _service.GetFundRequestReportAsync(brokerageHouseId, query);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
