using System.Text;
using StackExchange.Redis;

namespace BdStockOMS.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConnectionMultiplexer redis)
    {
        // Only apply to mutating requests
        if (context.Request.Method is not ("POST" or "PUT" or "PATCH"))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"]
                                    .FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var db        = redis.GetDatabase();
        var cacheKey  = $"idempotency:{idempotencyKey}";
        var cached    = await db.StringGetAsync(cacheKey);

        if (!cached.IsNullOrEmpty)
        {
            context.Response.StatusCode  = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cached!);
            return;
        }

        // Capture response body
        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memStream).ReadToEndAsync();

        // Cache for 24 hours on success
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            await db.StringSetAsync(cacheKey, responseBody, TimeSpan.FromHours(24));
        }

        memStream.Seek(0, SeekOrigin.Begin);
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}
