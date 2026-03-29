using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
namespace BdStockOMS.API.Authorization;
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _permission;
    public RequirePermissionAttribute(string permission) => _permission = permission;
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true) { context.Result = new UnauthorizedResult(); return; }
        var role = user.FindFirstValue(ClaimTypes.Role) ?? "";
        if (role == "SuperAdmin") { await next(); return; }
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) { context.Result = new ForbidResult(); return; }
        var svc = context.HttpContext.RequestServices.GetRequiredService<IUserPermissionService>();
        if (!await svc.HasPermissionAsync(userId, _permission))
        {
            context.Result = new ObjectResult(new { message = $"Permission denied: {_permission}", permission = _permission }) { StatusCode = 403 };
            return;
        }
        await next();
    }
}
