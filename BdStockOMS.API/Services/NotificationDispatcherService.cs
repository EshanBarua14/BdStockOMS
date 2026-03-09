using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class NotificationDispatcherService : INotificationDispatcherService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ISmsService _sms;
        private readonly ILogger<NotificationDispatcherService> _logger;

        public NotificationDispatcherService(
            AppDbContext db,
            IEmailService email,
            ISmsService sms,
            ILogger<NotificationDispatcherService> logger)
        {
            _db = db;
            _email = email;
            _sms = sms;
            _logger = logger;
        }

        public async Task DispatchAsync(int userId, NotificationEventType eventType, string subject, string body)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("DispatchAsync: User {UserId} not found", userId);
                return;
            }

            var allPrefs = await _db.NotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var eventPrefs = allPrefs.Where(p => p.EventType == eventType).ToList();
            var prefs = eventPrefs.Where(p => p.IsEnabled).ToList();

            if (prefs.Count == 0)
            {
                // Only send default email if no preference record exists at all for this event
                if (eventPrefs.Count == 0 && user.Email.Length > 0)
                    await _email.SendAsync(user.Email, subject, body);
                return;
            }

            foreach (var pref in prefs)
            {
                if (pref.Channel == NotificationChannel.Email || pref.Channel == NotificationChannel.Both)
                {
                    if (user.Email.Length > 0)
                        await _email.SendAsync(user.Email, subject, body);
                }
                if (pref.Channel == NotificationChannel.Sms || pref.Channel == NotificationChannel.Both)
                {
                    if (user.Phone.Length > 0)
                        await _sms.SendAsync(user.Phone, body);
                }
            }
        }

        public async Task DispatchToRoleAsync(string role, NotificationEventType eventType, string subject, string body)
        {
            var users = await _db.Users
                .Where(u => u.Role.Name == role && u.IsActive)
                .ToListAsync();

            foreach (var user in users)
            {
                await DispatchAsync(user.Id, eventType, subject, body);
            }
        }

        public async Task<IEnumerable<NotificationLog>> GetLogsAsync(int? userId = null, int page = 1, int pageSize = 20)
        {
            var query = _db.NotificationLogs.AsQueryable();

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            return await query
                .OrderByDescending(l => l.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<NotificationPreference>> GetPreferencesAsync(int userId)
        {
            return await _db.NotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task UpsertPreferenceAsync(int userId, NotificationEventType eventType, NotificationChannel channel, bool isEnabled)
        {
            var allPrefs = await _db.NotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var existing = allPrefs.FirstOrDefault(p => p.EventType == eventType);

            if (existing == null)
            {
                _db.NotificationPreferences.Add(new NotificationPreference
                {
                    UserId = userId,
                    EventType = eventType,
                    Channel = channel,
                    IsEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Channel = channel;
                existing.IsEnabled = isEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }
    }
}
