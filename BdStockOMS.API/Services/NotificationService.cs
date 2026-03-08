using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using BdStockOMS.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface INotificationService
{
    Task<Notification> CreateAsync(
        int userId, NotificationType type,
        string title, string message, string? actionUrl = null);
    Task CreateForMultipleUsersAsync(
        IEnumerable<int> userIds, NotificationType type,
        string title, string message, string? actionUrl = null);
    Task<Result> MarkAsReadAsync(int notificationId, int userId);
    Task<int> MarkAllAsReadAsync(int userId);
    Task<Result> DeleteAsync(int notificationId, int userId);
    Task<PagedResult<Notification>> GetMyNotificationsAsync(
        int userId, int page, int pageSize, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task<Notification> CreateAsync(
        int userId, NotificationType type,
        string title, string message, string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId    = userId,
            Type      = type,
            Title     = title,
            Message   = message,
            ActionUrl = actionUrl,
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Push via SignalR
        await _hub.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.CreatedAt
        });

        return notification;
    }

    public async Task CreateForMultipleUsersAsync(
        IEnumerable<int> userIds, NotificationType type,
        string title, string message, string? actionUrl = null)
    {
        foreach (var userId in userIds)
            await CreateAsync(userId, type, title, message, actionUrl);
    }

    public async Task<Result> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return Result.Failure("Notification not found.", "NOT_FOUND");

        if (notification.IsRead) return Result.Success();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<Result> DeleteAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return Result.Failure("Notification not found.", "NOT_FOUND");

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<PagedResult<Notification>> GetMyNotificationsAsync(
        int userId, int page, int pageSize, bool unreadOnly = false)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<Notification>.Create(items, total, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(int userId) =>
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
}
