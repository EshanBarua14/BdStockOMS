using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BdStockOMS.API.Services
{
    public class ComplianceService : IComplianceService
    {
        private readonly AppDbContext _db;
        private readonly ComplianceSettings _cfg;

        public ComplianceService(AppDbContext db, IOptions<ComplianceSettings> cfg)
        {
            _db = db;
            _cfg = cfg.Value;
        }

        public async Task<List<ComplianceReport>> ScanOrderAsync(Order order, CancellationToken ct = default)
        {
            var reports = new List<ComplianceReport>();

            var largeAlert = CheckLargeTrade(order);
            if (largeAlert != null) reports.Add(largeAlert);

            var amlAlert = await CheckAMLStructuringAsync(order, ct);
            if (amlAlert != null) reports.Add(amlAlert);

            var washAlert = await CheckWashTradeAsync(order, ct);
            if (washAlert != null) reports.Add(washAlert);

            var freqAlert = await CheckUnusualFrequencyAsync(order, ct);
            if (freqAlert != null) reports.Add(freqAlert);

            if (reports.Any())
            {
                _db.ComplianceReports.AddRange(reports);
                await _db.SaveChangesAsync(ct);
            }

            return reports;
        }

        public async Task<List<ComplianceReport>> ScanTradeHistoryAsync(int investorId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            var orders = await _db.Orders
                .Where(o => o.InvestorId == investorId && o.CreatedAt >= from && o.CreatedAt <= to)
                .ToListAsync(ct);

            var allReports = new List<ComplianceReport>();
            foreach (var order in orders)
            {
                var r = await ScanOrderAsync(order, ct);
                allReports.AddRange(r);
            }
            return allReports;
        }

        public async Task<ComplianceReport?> GetReportAsync(Guid reportId, CancellationToken ct = default)
            => await _db.ComplianceReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);

        public async Task<PagedResult<ComplianceReport>> GetReportsAsync(ComplianceFilterDto filter, CancellationToken ct = default)
        {
            var q = _db.ComplianceReports.AsQueryable();
            if (filter.BrokerageHouseId.HasValue) q = q.Where(r => r.BrokerageHouseId == filter.BrokerageHouseId.Value);
            if (filter.AlertType.HasValue)        q = q.Where(r => r.AlertType == filter.AlertType.Value);
            if (filter.Severity.HasValue)         q = q.Where(r => r.Severity == filter.Severity.Value);
            if (filter.Status.HasValue)           q = q.Where(r => r.Status == filter.Status.Value);
            if (filter.InvestorId.HasValue)       q = q.Where(r => r.FlaggedInvestorId == filter.InvestorId.Value);
            if (filter.From.HasValue)             q = q.Where(r => r.DetectedAt >= filter.From.Value);
            if (filter.To.HasValue)               q = q.Where(r => r.DetectedAt <= filter.To.Value);

            var total = await q.CountAsync(ct);
            var items = await q.OrderByDescending(r => r.DetectedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            return PagedResult<ComplianceReport>.Create(items, total, filter.Page, filter.PageSize);
        }

        public async Task<ComplianceReport> ResolveReportAsync(Guid reportId, ResolveComplianceDto dto, CancellationToken ct = default)
        {
            var report = await _db.ComplianceReports.FindAsync(new object[] { reportId }, ct)
                ?? throw new KeyNotFoundException($"Report {reportId} not found");
            report.Status = dto.NewStatus;
            report.ResolutionNotes = dto.ResolutionNotes;
            report.ResolvedBy = dto.ResolvedBy;
            report.ResolvedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return report;
        }

        public async Task<ComplianceReport> EscalateReportAsync(Guid reportId, string reason, string escalatedBy, CancellationToken ct = default)
        {
            var report = await _db.ComplianceReports.FindAsync(new object[] { reportId }, ct)
                ?? throw new KeyNotFoundException($"Report {reportId} not found");
            report.IsEscalated = true;
            report.Status = ComplianceStatus.Escalated;
            report.ResolutionNotes = reason;
            report.CreatedBy = escalatedBy;
            await _db.SaveChangesAsync(ct);
            return report;
        }

        public async Task<ComplianceSummaryDto> GetSummaryAsync(int brokerageHouseId, DateTime from, DateTime to, CancellationToken ct = default)
        {
            var reports = await _db.ComplianceReports
                .Where(r => r.BrokerageHouseId == brokerageHouseId && r.DetectedAt >= from && r.DetectedAt <= to)
                .ToListAsync(ct);

            return new ComplianceSummaryDto
            {
                TotalOpen      = reports.Count(r => r.Status == ComplianceStatus.Open),
                TotalResolved  = reports.Count(r => r.Status == ComplianceStatus.Resolved),
                TotalEscalated = reports.Count(r => r.Status == ComplianceStatus.Escalated),
                CriticalCount  = reports.Count(r => r.Severity == ComplianceSeverity.Critical),
                HighCount      = reports.Count(r => r.Severity == ComplianceSeverity.High),
                ByAlertType    = reports.GroupBy(r => r.AlertType.ToString())
                                        .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<byte[]> ExportReportsAsync(ComplianceExportDto dto, CancellationToken ct = default)
        {
            var reports = await _db.ComplianceReports
                .Where(r => r.BrokerageHouseId == dto.BrokerageHouseId
                         && r.DetectedAt >= dto.From
                         && r.DetectedAt <= dto.To)
                .OrderByDescending(r => r.DetectedAt)
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine("Id,AlertType,Severity,Status,FlaggedInvestorId,TradeValue,DetectedAt,ResolvedAt");
            foreach (var r in reports)
                sb.AppendLine(string.Join(",",
                    r.Id, r.AlertType, r.Severity, r.Status,
                    r.FlaggedInvestorId, r.TradeValue,
                    r.DetectedAt.ToString("O"),
                    r.ResolvedAt?.ToString("O") ?? ""));

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // ── Detection helpers ────────────────────────────────────────────

        private ComplianceReport? CheckLargeTrade(Order order)
        {
            var value = (decimal)order.Quantity * order.PriceAtOrder;
            if (value < _cfg.LargeTradeThresholdBDT) return null;

            var severity = value >= _cfg.LargeTradeThresholdBDT * 4 ? ComplianceSeverity.Critical
                         : value >= _cfg.LargeTradeThresholdBDT * 2 ? ComplianceSeverity.High
                         : ComplianceSeverity.Medium;

            return new ComplianceReport
            {
                BrokerageHouseId = order.BrokerageHouseId,
                AlertType        = ComplianceAlertType.LargeTradeAlert,
                Severity         = severity,
                FlaggedInvestorId = order.InvestorId,
                FlaggedEntityType = "Investor",
                OrderId          = order.Id,
                TradeValue       = value,
                Description      = "Large trade alert: BDT " + value.ToString("N0")
                                 + " exceeds threshold of BDT " + _cfg.LargeTradeThresholdBDT.ToString("N0"),
                CreatedBy        = "system"
            };
        }

        private async Task<ComplianceReport?> CheckAMLStructuringAsync(Order order, CancellationToken ct)
        {
            var windowStart  = order.CreatedAt.AddHours(-_cfg.AMLStructuringWindowHours);
            var recentOrders = await _db.Orders
                .Where(o => o.InvestorId == order.InvestorId
                         && o.CreatedAt >= windowStart
                         && o.CreatedAt <= order.CreatedAt)
                .ToListAsync(ct);

            var totalValue = recentOrders.Sum(o => (decimal)o.Quantity * o.PriceAtOrder);
            if (recentOrders.Count < _cfg.AMLStructuringMaxTransactions
             || totalValue < _cfg.DailyVolumeThresholdBDT)
                return null;

            return new ComplianceReport
            {
                BrokerageHouseId  = order.BrokerageHouseId,
                AlertType         = ComplianceAlertType.AMLStructuring,
                Severity          = ComplianceSeverity.Critical,
                FlaggedInvestorId = order.InvestorId,
                FlaggedEntityType = "Investor",
                OrderId           = order.Id,
                TradeValue        = totalValue,
                Description       = "AML structuring: " + recentOrders.Count
                                  + " transactions totalling BDT " + totalValue.ToString("N0")
                                  + " in " + _cfg.AMLStructuringWindowHours + "h window",
                PatternData       = "{\"txCount\":" + recentOrders.Count
                                  + ",\"totalBDT\":" + totalValue + "}",
                CreatedBy         = "system"
            };
        }

        private async Task<ComplianceReport?> CheckWashTradeAsync(Order order, CancellationToken ct)
        {
            var windowStart  = order.CreatedAt.AddMinutes(-_cfg.WashTradeWindowMinutes);
            var oppositeType = order.OrderType == OrderType.Buy ? OrderType.Sell : OrderType.Buy;

            var matchingOrder = await _db.Orders
                .Where(o => o.InvestorId == order.InvestorId
                         && o.StockId    == order.StockId
                         && o.OrderType  == oppositeType
                         && o.CreatedAt  >= windowStart
                         && o.CreatedAt  <= order.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (matchingOrder == null) return null;

            return new ComplianceReport
            {
                BrokerageHouseId  = order.BrokerageHouseId,
                AlertType         = ComplianceAlertType.WashTrade,
                Severity          = ComplianceSeverity.High,
                FlaggedInvestorId = order.InvestorId,
                FlaggedEntityType = "Investor",
                OrderId           = order.Id,
                Description       = "Wash trade detected: StockId " + order.StockId
                                  + " buy/sell within " + _cfg.WashTradeWindowMinutes + "min window",
                PatternData       = "{\"stockId\":" + order.StockId
                                  + ",\"orderId1\":" + order.Id
                                  + ",\"orderId2\":" + matchingOrder.Id + "}",
                CreatedBy         = "system"
            };
        }

        private async Task<ComplianceReport?> CheckUnusualFrequencyAsync(Order order, CancellationToken ct)
        {
            var windowStart = order.CreatedAt.AddHours(-1);
            var count = await _db.Orders
                .CountAsync(o => o.InvestorId == order.InvestorId
                              && o.CreatedAt  >= windowStart
                              && o.CreatedAt  <= order.CreatedAt, ct);

            if (count <= _cfg.SuspiciousFrequencyPerHour) return null;

            return new ComplianceReport
            {
                BrokerageHouseId  = order.BrokerageHouseId,
                AlertType         = ComplianceAlertType.UnusualFrequency,
                Severity          = ComplianceSeverity.Medium,
                FlaggedInvestorId = order.InvestorId,
                FlaggedEntityType = "Investor",
                OrderId           = order.Id,
                Description       = "Unusual order frequency: " + count
                                  + " orders in 1 hour (threshold: "
                                  + _cfg.SuspiciousFrequencyPerHour + ")",
                CreatedBy         = "system"
            };
        }
    }
}
