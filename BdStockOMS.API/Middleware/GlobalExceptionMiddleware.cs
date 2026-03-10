using System.Net;
using System.Text.Json;

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

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, message) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,     "UNAUTHORIZED",   "Access denied."),
            KeyNotFoundException        => (HttpStatusCode.NotFound,         "NOT_FOUND",      ex.Message),
            ArgumentException           => (HttpStatusCode.BadRequest,       "INVALID_INPUT",  ex.Message),
            InvalidOperationException   => (HttpStatusCode.BadRequest,       "INVALID_OPERATION", ex.Message),
            _                           => (HttpStatusCode.InternalServerError, "SERVER_ERROR","An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success   = false,
            errorCode = errorCode,
            message   = message,
            traceId   = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
