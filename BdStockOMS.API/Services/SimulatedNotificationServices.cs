using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BdStockOMS.API.Models;
using BdStockOMS.API.Data;

namespace BdStockOMS.API.Services
{
    public class SimulatedEmailService : IEmailService
    {
        private readonly ILogger<SimulatedEmailService> _logger;
        private readonly AppDbContext _db;

        public SimulatedEmailService(ILogger<SimulatedEmailService> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation("[SIMULATED EMAIL] To: {Email} | Subject: {Subject}", toEmail, subject);

            var log = new NotificationLog
            {
                Channel = NotificationChannel.Email,
                Recipient = toEmail,
                Subject = subject,
                Body = body,
                IsSuccess = true,
                SentAt = DateTime.UtcNow
            };
            _db.NotificationLogs.Add(log);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendBulkAsync(IEnumerable<string> toEmails, string subject, string body)
        {
            var allSuccess = true;
            foreach (var email in toEmails)
            {
                var result = await SendAsync(email, subject, body);
            }
            return allSuccess;
        }
    }

    public class SimulatedSmsService : ISmsService
    {
        private readonly ILogger<SimulatedSmsService> _logger;
        private readonly AppDbContext _db;

        public SimulatedSmsService(ILogger<SimulatedSmsService> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<bool> SendAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("[SIMULATED SMS] To: {Phone} | Message: {Message}", phoneNumber, message);

            var log = new NotificationLog
            {
                Channel = NotificationChannel.Sms,
                Recipient = phoneNumber,
                Subject = "SMS",
                Body = message,
                IsSuccess = true,
                SentAt = DateTime.UtcNow
            };
            _db.NotificationLogs.Add(log);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SendBulkAsync(IEnumerable<string> phoneNumbers, string message)
        {
            var allSuccess = true;
            foreach (var phone in phoneNumbers)
            {
                var result = await SendAsync(phone, message);
            }
            return allSuccess;
        }
    }
}
