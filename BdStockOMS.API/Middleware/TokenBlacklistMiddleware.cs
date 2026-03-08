using System.IdentityModel.Tokens.Jwt;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklist)
    {
        var token = context.Request.Headers["Authorization"]
                           .FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt     = handler.ReadJwtToken(token);
                var jti     = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                var userIdStr = jwt.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

                if (!string.IsNullOrEmpty(jti) && await blacklist.IsBlacklistedAsync(jti))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message   = "Token has been revoked.",
                        errorCode = "TOKEN_REVOKED",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }

                if (!string.IsNullOrEmpty(userIdStr) &&
                    int.TryParse(userIdStr, out int userId) &&
                    await blacklist.IsUserBlacklistedAsync(userId))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message   = "Session invalidated. Please log in again.",
                        errorCode = "SESSION_REVOKED",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }
            }
            catch { /* invalid token format — let auth middleware handle it */ }
        }

        await _next(context);
    }
}
