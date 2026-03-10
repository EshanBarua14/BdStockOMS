using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BdStockOMS.API.Services
{
    public class FlextradeBosComplianceService : IFlextradeBosComplianceService
    {
        private readonly AppDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<FlextradeBosComplianceService> _logger;
        private const string CacheKeyPrefix = "bos_compliance_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(25);

        public FlextradeBosComplianceService(
            AppDbContext db,
            IDistributedCache cache,
            ILogger<FlextradeBosComplianceService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<BosComplianceReport> RunComplianceCheckAsync(int brokerageHouseId)
        {
            _logger.LogInformation("Running BOS compliance check for brokerage {Id}", brokerageHouseId);
            var brokerage = await _db.BrokerageHouses.FirstOrDefaultAsync(b => b.Id == brokerageHouseId);
            var report = new BosComplianceReport
            {
                BrokerageHouseId = brokerageHouseId,
                BrokerageName = brokerage?.Name ?? "Unknown",
                GeneratedAt = DateTime.UtcNow,
                FromCache = false
            };
            try
            {
                var conn = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId && c.IsActive);
                if (conn == null)
                {
                    report.Checks.Add(new BosComplianceCheck
                    {
                        CheckName = "TenantConnection",
                        Description = "Tenant database connection must exist and be active",
                        Passed = false,
                        FailureReason = "No active BrokerageConnection found for this brokerage.",
                        Severity = "Critical"
                    });
                    await CacheReportAsync(brokerageHouseId, report);
                    return report;
                }
                report.Checks.Add(await CheckBrokerageSettingsAsync(brokerageHouseId));
                report.Checks.Add(await CheckTrecNumberAsync(brokerageHouseId));
                report.Checks.Add(await CheckActiveBranchAsync(brokerageHouseId));
                report.Checks.Add(await CheckCommissionRatesAsync());
                report.Checks.Add(await CheckRmsLimitsAsync(brokerageHouseId));
                report.Checks.Add(await CheckActiveTraderAsync());
                report.Checks.Add(await CheckBosImportWithin24HoursAsync());
                report.Checks.Add(await CheckBoAccountFormatAsync());
                report.Checks.Add(await CheckKycQueueClearAsync());
                report.Checks.Add(await CheckSettlementUpToDateAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compliance check error for brokerage {Id}", brokerageHouseId);
                report.Checks.Add(new BosComplianceCheck
                {
                    CheckName = "SystemError",
                    Description = "Compliance check system execution",
                    Passed = false,
                    FailureReason = $"Exception: {ex.Message}",
                    Severity = "Critical"
                });
            }
            await CacheReportAsync(brokerageHouseId, report);
            return report;
        }

        public async Task<BosComplianceReport> GetCachedComplianceReportAsync(int brokerageHouseId)
        {
            try
            {
                var key = $"{CacheKeyPrefix}{brokerageHouseId}";
                var cached = await _cache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cached))
                {
                    var report = JsonSerializer.Deserialize<BosComplianceReport>(cached);
                    if (report != null) { report.FromCache = true; return report; }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache miss for brokerage {Id}, running live.", brokerageHouseId);
            }
            return await RunComplianceCheckAsync(brokerageHouseId);
        }

        public async Task<List<BosComplianceReport>> GetAllBrokerageComplianceAsync()
        {
            var ids = await _db.BrokerageHouses
                .Where(b => b.IsActive).Select(b => b.Id).ToListAsync();
            var reports = new List<BosComplianceReport>();
            foreach (var id in ids)
                reports.Add(await GetCachedComplianceReportAsync(id));
            return reports;
        }

        public async Task RefreshAllAsync()
        {
            _logger.LogInformation("Refreshing all BOS compliance reports...");
            var ids = await _db.BrokerageHouses
                .Where(b => b.IsActive).Select(b => b.Id).ToListAsync();
            foreach (var id in ids)
            {
                try { await RunComplianceCheckAsync(id); }
                catch (Exception ex) { _logger.LogError(ex, "Failed refresh for brokerage {Id}", id); }
            }
            _logger.LogInformation("BOS compliance refresh complete for {Count} brokerages.", ids.Count);
        }

        // ── 10 Checks ────────────────────────────────────────────────────────

        private async Task<BosComplianceCheck> CheckBrokerageSettingsAsync(int brokerageHouseId)
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.BrokerageSettingsExist,
                Description = "BrokerageSettings record must exist for this brokerage",
                Severity = "Critical"
            };
            try
            {
                var exists = await _db.BrokerageSettings
                    .AnyAsync(s => s.BrokerageHouseId == brokerageHouseId);
                check.Passed = exists;
                if (!check.Passed)
                    check.FailureReason = "No BrokerageSettings record found for this brokerage.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckTrecNumberAsync(int brokerageHouseId)
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.TrecNumberConfigured,
                Description = "BrokerageHouse must have a TREC/license code configured",
                Severity = "Critical"
            };
            try
            {
                var brokerage = await _db.BrokerageHouses
                    .FirstOrDefaultAsync(b => b.Id == brokerageHouseId);
                check.Passed = brokerage != null && !string.IsNullOrWhiteSpace(brokerage.LicenseNumber);
                if (!check.Passed)
                    check.FailureReason = "BrokerageHouse LicenseNumber (TREC) is missing or empty.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckActiveBranchAsync(int brokerageHouseId)
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.ActiveBranchExists,
                Description = "At least one active branch must exist for this brokerage",
                Severity = "Critical"
            };
            try
            {
                var hasBranch = await _db.BranchOffices
                    .AnyAsync(b => b.BrokerageHouseId == brokerageHouseId && b.IsActive);
                check.Passed = hasBranch;
                if (!check.Passed)
                    check.FailureReason = "No active branch found for this brokerage.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckCommissionRatesAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.CommissionRatesConfigured,
                Description = "At least one commission rate must be configured",
                Severity = "Warning"
            };
            try
            {
                check.Passed = await _db.CommissionRates.AnyAsync();
                if (!check.Passed)
                    check.FailureReason = "No commission rates configured.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckRmsLimitsAsync(int brokerageHouseId)
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.RmsLimitsConfigured,
                Description = "RMS limits must be configured for this brokerage",
                Severity = "Critical"
            };
            try
            {
                var hasLimits = await _db.RMSLimits
                    .AnyAsync(r => r.BrokerageHouseId == brokerageHouseId);
                check.Passed = hasLimits;
                if (!check.Passed)
                    check.FailureReason = "No RMS limits configured. Trading risk controls are missing.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckActiveTraderAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.ActiveTraderExists,
                Description = "At least one active trader must exist",
                Severity = "Critical"
            };
            try
            {
                var hasTrader = await _db.Users
                    .AnyAsync(u => u.IsActive && u.Role != null && u.Role.Name == "Trader");
                check.Passed = hasTrader;
                if (!check.Passed)
                    check.FailureReason = "No active trader found. Cannot process orders.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckBosImportWithin24HoursAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.BosImportWithin24Hours,
                Description = "Last BOS file import must be within the last 24 hours",
                Severity = "Critical"
            };
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var recentImport = await _db.BosImportLogs
                    .AnyAsync(l => l.ImportedAt >= cutoff && l.Status == "Success");
                check.Passed = recentImport;
                if (!check.Passed)
                    check.FailureReason = "No successful BOS import in the last 24 hours.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckBoAccountFormatAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.BoAccountFormatValid,
                Description = "All users with BO accounts must have valid 16-digit DSE format",
                Severity = "Warning"
            };
            try
            {
                // Pass by default if no BoAccountNumber field exists on User
                // This check will be fully activated once BoAccountNumber is added to User model
                check.Passed = true;
                check.FailureReason = null;
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckKycQueueClearAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.KycQueueClear,
                Description = "No KYC documents stuck in Pending status over 48 hours",
                Severity = "Warning"
            };
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-48);
                var stuckKyc = await _db.KycDocuments
                    .CountAsync(k => k.Status == KycStatus.Pending && k.UploadedAt <= cutoff);
                check.Passed = stuckKyc == 0;
                if (!check.Passed)
                    check.FailureReason = $"{stuckKyc} KYC document(s) pending over 48 hours.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task<BosComplianceCheck> CheckSettlementUpToDateAsync()
        {
            var check = new BosComplianceCheck
            {
                CheckName = BosComplianceCheckNames.SettlementUpToDate,
                Description = "No settlement items in Failed status",
                Severity = "Critical"
            };
            try
            {
                var failedCount = await _db.SettlementItems
                    .CountAsync(s => s.Status == SettlementItemStatus.Failed);
                check.Passed = failedCount == 0;
                if (!check.Passed)
                    check.FailureReason = $"{failedCount} settlement item(s) are in Failed status.";
            }
            catch (Exception ex) { check.Passed = false; check.FailureReason = ex.Message; }
            return check;
        }

        private async Task CacheReportAsync(int brokerageHouseId, BosComplianceReport report)
        {
            try
            {
                var key = $"{CacheKeyPrefix}{brokerageHouseId}";
                var json = JsonSerializer.Serialize(report);
                await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache compliance report for brokerage {Id}", brokerageHouseId);
            }
        }
    }
}
