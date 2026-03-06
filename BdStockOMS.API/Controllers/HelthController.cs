using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        var bangladeshZone = TimeZoneInfo.FindSystemTimeZoneById(
            "Bangladesh Standard Time"
        );

        var bdTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            bangladeshZone
        );

        var marketOpen = IsMarketOpen(bdTime);

        return Ok(new
        {
            status = "BD Stock OMS is running",
            serverUtcTime = DateTime.UtcNow,
            bdTime = bdTime.ToString("yyyy-MM-dd HH:mm:ss"),
            marketStatus = marketOpen ? "OPEN" : "CLOSED",
            tradingDays = "Sunday - Thursday",
            tradingHours = "10:00 AM - 2:30 PM BST",
            exchanges = new[] { "DSE", "CSE" },
            apiVersion = "1.0.0"
        });
    }

    private bool IsMarketOpen(DateTime bdTime)
    {
        var tradingDays = new[]
        {
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday
        };

        bool isTradingDay = tradingDays.Contains(bdTime.DayOfWeek);

        var opens = new TimeSpan(10, 0, 0);
        var closes = new TimeSpan(14, 30, 0);

        bool isDuringHours = bdTime.TimeOfDay >= opens
                          && bdTime.TimeOfDay <= closes;

        return isTradingDay && isDuringHours;
    }
}