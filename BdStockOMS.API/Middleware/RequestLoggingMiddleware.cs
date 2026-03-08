using System.Text.RegularExpressions;

namespace BdStockOMS.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private static readonly Regex _maskPattern =
        new(@"""(password|token|boNumber)"":\s*""[^""]*""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RequestLoggingMiddleware(RequestDelegate next,
                                    ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw     = System.Diagnostics.Stopwatch.StartNew();
        var userId = context.User?.FindFirst("userId")?.Value ?? "anonymous";

        await _next(context);

        sw.Stop();

        _logger.LogInformation(
            "{Method} {Path} | User={UserId} | Status={StatusCode} | {Duration}ms",
            context.Request.Method,
            context.Request.Path,
            userId,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
}
