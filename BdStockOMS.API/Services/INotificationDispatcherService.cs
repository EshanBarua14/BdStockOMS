using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string toEmail, string subject, string body);
        Task<bool> SendBulkAsync(IEnumerable<string> toEmails, string subject, string body);
    }

    public interface ISmsService
    {
        Task<bool> SendAsync(string phoneNumber, string message);
        Task<bool> SendBulkAsync(IEnumerable<string> phoneNumbers, string message);
    }

    public interface INotificationDispatcherService
    {
        Task DispatchAsync(int userId, NotificationEventType eventType, string subject, string body);
        Task DispatchToRoleAsync(string role, NotificationEventType eventType, string subject, string body);
        Task<IEnumerable<NotificationLog>> GetLogsAsync(int? userId = null, int page = 1, int pageSize = 20);
        Task<IEnumerable<NotificationPreference>> GetPreferencesAsync(int userId);
        Task UpsertPreferenceAsync(int userId, NotificationEventType eventType, NotificationChannel channel, bool isEnabled);
    }
}
