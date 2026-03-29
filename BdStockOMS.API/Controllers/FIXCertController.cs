using BdStockOMS.API.FIX;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/fix/cert")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class FIXCertController : ControllerBase
{
    private readonly IFIXConnectorFactory _factory;

    public FIXCertController(IFIXConnectorFactory factory) => _factory = factory;

    [HttpGet("scenarios")]
    public IActionResult GetScenarios()
    {
        var scenarios = Enum.GetValues<FIXCertScenario>().Select(s => new
        {
            id   = (int)s,
            key  = s.ToString(),
            name = s.ToString().Replace("_", " "),
        });
        return Ok(scenarios);
    }

    [HttpPost("run/{scenarioId}")]
    public async Task<IActionResult> RunScenario(int scenarioId, [FromQuery] string exchange = "DSE")
    {
        if (!Enum.TryParse<FIXCertScenario>(scenarioId.ToString(), out var scenario))
            return BadRequest(new { message = $"Unknown scenario ID {scenarioId}" });

        var connector = _factory.GetConnector(exchange);
        if (connector.SessionState != FIXSessionState.Active)
            await connector.ConnectAsync();

        var result = await FIXCertScenarioRunner.RunAsync(scenario, connector);
        return Ok(result);
    }

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll([FromQuery] string exchange = "DSE")
    {
        var connector = _factory.GetConnector(exchange);
        if (connector.SessionState != FIXSessionState.Active)
            await connector.ConnectAsync();

        var results = await FIXCertScenarioRunner.RunAllAsync(connector);
        var summary = new
        {
            total   = results.Count,
            passed  = results.Count(r => r.Passed),
            failed  = results.Count(r => !r.Passed),
            results,
        };
        return Ok(summary);
    }

    [HttpPost("validate")]
    public IActionResult ValidateOrder([FromBody] FIXOrderRequest req)
    {
        var result = FIXOrderTypeValidator.Validate(req);
        return Ok(new
        {
            isValid       = result.IsValid,
            errors        = result.Errors,
            warnings      = result.Warnings,
            fixOrdType    = result.FIXOrdType,
            fixTimeInForce= result.FIXTimeInForce,
            fixSide       = result.FIXSide,
        });
    }
}
