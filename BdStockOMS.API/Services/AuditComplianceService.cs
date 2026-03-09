using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class AuditLogFilter
    {
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SuspiciousActivityResult
    {
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IAuditComplianceService
    {
        Task<IEnumerable<AuditLog>> GetLogsAsync(AuditLogFilter filter);
        Task<int> CountLogsAsync(AuditLogFilter filter);
        Task<string> ExportCsvAsync(AuditLogFilter filter);
        Task<IEnumerable<SuspiciousActivityResult>> DetectSuspiciousActivityAsync();
    }

    public class AuditComplianceService : IAuditComplianceService
    {
        private readonly AppDbContext _db;

        // Thresholds
        private const int FailedLoginThreshold = 5;
        private const int RapidCancellationThreshold = 3;
        private const decimal LargeOrderThreshold = 1_000_000m;
        private const int WindowMinutes = 30;

        public AuditComplianceService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AuditLog>> GetLogsAsync(AuditLogFilter filter)
        {
            var query = BuildQuery(filter);
            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountLogsAsync(AuditLogFilter filter)
        {
            return await BuildQuery(filter).CountAsync();
        }

        public async Task<string> ExportCsvAsync(AuditLogFilter filter)
        {
            var noPageFilter = new AuditLogFilter
            {
                UserId = filter.UserId,
                Action = filter.Action,
                EntityType = filter.EntityType,
                IpAddress = filter.IpAddress,
                From = filter.From,
                To = filter.To,
                Page = 1,
                PageSize = int.MaxValue
            };

            var logs = await BuildQuery(noPageFilter)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,UserId,Action,EntityType,EntityId,IpAddress,CreatedAt");
            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id},{log.UserId},{EscapeCsv(log.Action)},{EscapeCsv(log.EntityType)},{log.EntityId},{EscapeCsv(log.IpAddress ?? "")},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return sb.ToString();
        }

        public async Task<IEnumerable<SuspiciousActivityResult>> DetectSuspiciousActivityAsync()
        {
            var results = new List<SuspiciousActivityResult>();
            var windowStart = DateTime.UtcNow.AddMinutes(-WindowMinutes);

            // 1. Failed login spike
            var failedLogins = await _db.AuditLogs
                .Where(l => l.Action == "LoginFailed" && l.CreatedAt >= windowStart)
                .ToListAsync();

            var failedByUser = failedLogins
                .GroupBy(l => l.UserId)
                .Where(g => g.Count() >= FailedLoginThreshold);

            foreach (var group in failedByUser)
            {
                results.Add(new SuspiciousActivityResult
                {
                    UserId = group.Key,
                    Reason = $"Failed login attempts: {group.Count()} in {WindowMinutes} minutes",
                    EventCount = group.Count()
                });
            }

            // 2. Rapid order cancellations
            var cancellations = await _db.AuditLogs
                .Where(l => l.Action == "OrderCancelled" && l.CreatedAt >= windowStart)
                .ToListAsync();

            var cancelByUser = cancellations
                .GroupBy(l => l.UserId)
                .Where(g => g.Count() >= RapidCancellationThreshold);

            foreach (var group in cancelByUser)
            {
                results.Add(new SuspiciousActivityResult
                {
                    UserId = group.Key,
                    Reason = $"Rapid order cancellations: {group.Count()} in {WindowMinutes} minutes",
                    EventCount = group.Count()
                });
            }

            // 3. Large order placements
            var largeOrders = await _db.Orders
                .Where(o => o.Quantity * o.PriceAtOrder >= LargeOrderThreshold)
                .ToListAsync();

            var largeByUser = largeOrders
                .GroupBy(o => o.InvestorId)
                .Where(g => g.Any());

            foreach (var group in largeByUser)
            {
                results.Add(new SuspiciousActivityResult
                {
                    UserId = group.Key,
                    Reason = $"Large order detected: total value >= {LargeOrderThreshold:N0} BDT",
                    EventCount = group.Count()
                });
            }

            return results;
        }

        private IQueryable<AuditLog> BuildQuery(AuditLogFilter filter)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (filter.UserId.HasValue)
                query = query.Where(l => l.UserId == filter.UserId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(l => l.Action.Contains(filter.Action));

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                query = query.Where(l => l.EntityType == filter.EntityType);

            if (!string.IsNullOrWhiteSpace(filter.IpAddress))
                query = query.Where(l => l.IpAddress == filter.IpAddress);

            if (filter.From.HasValue)
                query = query.Where(l => l.CreatedAt >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(l => l.CreatedAt <= filter.To.Value);

            return query;
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
