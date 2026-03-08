using System.Net;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next,
                                     ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            try
            {
                db.SystemLogs.Add(new SystemLog
                {
                    Level      = (BdStockOMS.API.Models.LogLevel)3,
                    Source     = context.Request.Path,
                    Message    = ex.Message,
                    StackTrace = ex.StackTrace,
                    CreatedAt  = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch { /* never let logging crash the app */ }

            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                message   = "An unexpected error occurred. Please try again later.",
                errorCode = "INTERNAL_ERROR",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
