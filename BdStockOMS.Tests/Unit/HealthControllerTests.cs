using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class HealthControllerTests
{
    [Fact]
    public void GetHealth_Returns200OkResponse()
    {
        // ARRANGE
        var controller = new HealthController();

        // ACT
        var result = controller.GetHealth();

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetHealth_ResponseHasCorrectStatus()
    {
        // ARRANGE
        var controller = new HealthController();

        // ACT
        var result = controller.GetHealth();

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Convert the response object to JSON string
        // then check if it contains what we expect
        // This works reliably across assemblies
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("BD Stock OMS", json);
    }

    [Fact]
    public void GetHealth_MarketStatusIsOpenOrClosed()
    {
        // ARRANGE
        var controller = new HealthController();

        // ACT
        var result = controller.GetHealth();

        // ASSERT
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Serialize to JSON and check for
        // either OPEN or CLOSED in the response
        var json = JsonSerializer.Serialize(okResult.Value);
        Assert.True(
            json.Contains("OPEN") || json.Contains("CLOSED"),
            $"Expected OPEN or CLOSED in response but got: {json}"
        );
    }
}