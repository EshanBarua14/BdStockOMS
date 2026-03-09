using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;

namespace BdStockOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationDispatcherService _dispatcher;

        public NotificationController(INotificationDispatcherService dispatcher)
        {
            _dispatcher = dispatcher;
        }

        // GET api/notification/logs
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin,Admin,Compliance")]
        public async Task<IActionResult> GetLogs([FromQuery] int? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var logs = await _dispatcher.GetLogsAsync(userId, page, pageSize);
            return Ok(logs);
        }

        // GET api/notification/preferences/{userId}
        [HttpGet("preferences/{userId}")]
        public async Task<IActionResult> GetPreferences(int userId)
        {
            var prefs = await _dispatcher.GetPreferencesAsync(userId);
            return Ok(prefs);
        }

        // POST api/notification/preferences
        [HttpPost("preferences")]
        public async Task<IActionResult> UpsertPreference([FromBody] UpsertPreferenceRequest request)
        {
            await _dispatcher.UpsertPreferenceAsync(request.UserId, request.EventType, request.Channel, request.IsEnabled);
            return Ok(new { message = "Preference saved." });
        }

        // POST api/notification/dispatch
        [HttpPost("dispatch")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchRequest request)
        {
            await _dispatcher.DispatchAsync(request.UserId, request.EventType, request.Subject, request.Body);
            return Ok(new { message = "Notification dispatched." });
        }

        // POST api/notification/dispatch-role
        [HttpPost("dispatch-role")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DispatchToRole([FromBody] DispatchRoleRequest request)
        {
            await _dispatcher.DispatchToRoleAsync(request.Role, request.EventType, request.Subject, request.Body);
            return Ok(new { message = "Role notification dispatched." });
        }
    }

    public record UpsertPreferenceRequest(int UserId, NotificationEventType EventType, NotificationChannel Channel, bool IsEnabled);
    public record DispatchRequest(int UserId, NotificationEventType EventType, string Subject, string Body);
    public record DispatchRoleRequest(string Role, NotificationEventType EventType, string Subject, string Body);
}
