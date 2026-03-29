using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace BdStockOMS.Tests
{
    public class Day88ComplianceTests
    {
        private AppDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        private ComplianceService CreateSvc(AppDbContext db, ComplianceSettings? cfg = null)
        {
            cfg ??= new ComplianceSettings
            {
                LargeTradeThresholdBDT        = 5_000_000m,
                DailyVolumeThresholdBDT       = 20_000_000m,
                AMLStructuringWindowHours     = 24,
                AMLStructuringMaxTransactions = 10,
                WashTradeWindowMinutes        = 30,
                SuspiciousFrequencyPerHour    = 20
            };
            return new ComplianceService(db, Options.Create(cfg));
        }

        private Order MakeOrder(int qty, decimal price,
            int investorId = 1, int stockId = 10,
            OrderType type = OrderType.Buy,
            int brokerageHouseId = 99)
            => new Order
            {
                InvestorId       = investorId,
                StockId          = stockId,
                OrderType        = type,
                BrokerageHouseId = brokerageHouseId,
                Quantity         = qty,
                PriceAtOrder     = price,
                Status           = OrderStatus.Pending,
                CreatedAt        = DateTime.UtcNow
            };

        // ── Large trade ──────────────────────────────────────────────

        [Fact]
        public async Task LargeTrade_AboveThreshold_CreatesReport()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(1000, 6000m); // 6M > 5M
            var reports = await svc.ScanOrderAsync(order);
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.LargeTradeAlert);
        }

        [Fact]
        public async Task LargeTrade_BelowThreshold_NoReport()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(100, 100m); // 10K
            var reports = await svc.ScanOrderAsync(order);
            Assert.DoesNotContain(reports, r => r.AlertType == ComplianceAlertType.LargeTradeAlert);
        }

        [Fact]
        public async Task LargeTrade_4xThreshold_CriticalSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(1000, 25000m); // 25M = 5x
            var reports = await svc.ScanOrderAsync(order);
            Assert.Contains(reports, r =>
                r.AlertType == ComplianceAlertType.LargeTradeAlert &&
                r.Severity  == ComplianceSeverity.Critical);
        }

        [Fact]
        public async Task LargeTrade_2xThreshold_HighSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(1000, 12000m); // 12M = 2.4x
            var reports = await svc.ScanOrderAsync(order);
            Assert.Contains(reports, r =>
                r.AlertType == ComplianceAlertType.LargeTradeAlert &&
                r.Severity  == ComplianceSeverity.High);
        }

        [Fact]
        public async Task LargeTrade_JustAboveThreshold_MediumSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(1000, 5500m); // 5.5M = 1.1x
            var reports = await svc.ScanOrderAsync(order);
            Assert.Contains(reports, r =>
                r.AlertType == ComplianceAlertType.LargeTradeAlert &&
                r.Severity  == ComplianceSeverity.Medium);
        }

        [Fact]
        public async Task LargeTrade_ReportPersisted_InDatabase()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await svc.ScanOrderAsync(MakeOrder(2000, 5000m));
            Assert.True(await db.ComplianceReports.AnyAsync());
        }

        [Fact]
        public async Task LargeTrade_FlaggedInvestorId_Correct()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(2000, 5000m, investorId: 42);
            var reports = await svc.ScanOrderAsync(order);
            var alert = reports.First(r => r.AlertType == ComplianceAlertType.LargeTradeAlert);
            Assert.Equal(42, alert.FlaggedInvestorId);
        }

        [Fact]
        public async Task LargeTrade_TradeValue_Stored()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(1000, 6000m);
            var reports = await svc.ScanOrderAsync(order);
            var alert = reports.First(r => r.AlertType == ComplianceAlertType.LargeTradeAlert);
            Assert.Equal(6_000_000m, alert.TradeValue);
        }

        // ── AML structuring ──────────────────────────────────────────

        [Fact]
        public async Task AML_StructuringDetected_WhenCountAndValueExceedThreshold()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            // 11 prior orders @ qty=200 price=10000 = 2M each = 22M total
            for (int i = 0; i < 11; i++)
            {
                var o = MakeOrder(200, 10000m);
                o.CreatedAt = DateTime.UtcNow.AddHours(-12).AddMinutes(i * 10);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 5000m);
            trigger.CreatedAt = DateTime.UtcNow;
            db.Orders.Add(trigger);
            await db.SaveChangesAsync();

            var reports = await svc.ScanOrderAsync(trigger);
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.AMLStructuring);
        }

        [Fact]
        public async Task AML_NoAlert_WhenCountBelowThreshold()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 5; i++) db.Orders.Add(MakeOrder(200, 10000m));
            await db.SaveChangesAsync();
            var reports = await svc.ScanOrderAsync(MakeOrder(200, 10000m));
            Assert.DoesNotContain(reports, r => r.AlertType == ComplianceAlertType.AMLStructuring);
        }

        [Fact]
        public async Task AML_IsCriticalSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 11; i++)
            {
                var o = MakeOrder(200, 10000m);
                o.CreatedAt = DateTime.UtcNow.AddHours(-1);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();
            var t = MakeOrder(100, 5000m);
            var reports = await svc.ScanOrderAsync(t);
            var aml = reports.FirstOrDefault(r => r.AlertType == ComplianceAlertType.AMLStructuring);
            if (aml != null) Assert.Equal(ComplianceSeverity.Critical, aml.Severity);
        }

        [Fact]
        public async Task AML_PatternData_ContainsTxCount()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 11; i++)
            {
                var o = MakeOrder(200, 10000m);
                o.CreatedAt = DateTime.UtcNow.AddHours(-1);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();
            var t = MakeOrder(100, 5000m);
            var reports = await svc.ScanOrderAsync(t);
            var aml = reports.FirstOrDefault(r => r.AlertType == ComplianceAlertType.AMLStructuring);
            if (aml != null) Assert.Contains("txCount", aml.PatternData);
        }

        // ── Wash trade ───────────────────────────────────────────────

        [Fact]
        public async Task WashTrade_Detected_BuySellSameStockInWindow()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var existing = MakeOrder(100, 500m, investorId: 7, stockId: 5, type: OrderType.Buy);
            existing.CreatedAt = DateTime.UtcNow.AddMinutes(-20);
            db.Orders.Add(existing);
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 500m, investorId: 7, stockId: 5, type: OrderType.Sell);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.WashTrade);
        }

        [Fact]
        public async Task WashTrade_NotDetected_DifferentStock()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var existing = MakeOrder(100, 500m, investorId: 8, stockId: 5, type: OrderType.Buy);
            existing.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
            db.Orders.Add(existing);
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 500m, investorId: 8, stockId: 99, type: OrderType.Sell);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.DoesNotContain(reports, r => r.AlertType == ComplianceAlertType.WashTrade);
        }

        [Fact]
        public async Task WashTrade_NotDetected_OutsideWindow()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var existing = MakeOrder(100, 500m, investorId: 9, stockId: 5, type: OrderType.Buy);
            existing.CreatedAt = DateTime.UtcNow.AddMinutes(-60);
            db.Orders.Add(existing);
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 500m, investorId: 9, stockId: 5, type: OrderType.Sell);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.DoesNotContain(reports, r => r.AlertType == ComplianceAlertType.WashTrade);
        }

        [Fact]
        public async Task WashTrade_IsHighSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var existing = MakeOrder(100, 500m, investorId: 10, stockId: 5, type: OrderType.Buy);
            existing.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
            db.Orders.Add(existing);
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 500m, investorId: 10, stockId: 5, type: OrderType.Sell);
            var reports = await svc.ScanOrderAsync(trigger);
            var wash = reports.FirstOrDefault(r => r.AlertType == ComplianceAlertType.WashTrade);
            if (wash != null) Assert.Equal(ComplianceSeverity.High, wash.Severity);
        }

        [Fact]
        public async Task WashTrade_PatternData_ContainsStockId()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var existing = MakeOrder(100, 500m, investorId: 11, stockId: 77, type: OrderType.Buy);
            existing.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
            db.Orders.Add(existing);
            await db.SaveChangesAsync();

            var trigger = MakeOrder(100, 500m, investorId: 11, stockId: 77, type: OrderType.Sell);
            var reports = await svc.ScanOrderAsync(trigger);
            var wash = reports.FirstOrDefault(r => r.AlertType == ComplianceAlertType.WashTrade);
            if (wash != null) Assert.Contains("77", wash.PatternData);
        }

        // ── Unusual frequency ────────────────────────────────────────

        [Fact]
        public async Task UnusualFrequency_Detected_WhenOverThreshold()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 21; i++)
            {
                var o = MakeOrder(10, 100m, investorId: 20);
                o.CreatedAt = DateTime.UtcNow.AddMinutes(-i * 2);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();
            var trigger = MakeOrder(10, 100m, investorId: 20);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.UnusualFrequency);
        }

        [Fact]
        public async Task UnusualFrequency_NotDetected_BelowThreshold()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 5; i++) db.Orders.Add(MakeOrder(10, 100m, investorId: 21));
            await db.SaveChangesAsync();
            var trigger = MakeOrder(10, 100m, investorId: 21);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.DoesNotContain(reports, r => r.AlertType == ComplianceAlertType.UnusualFrequency);
        }

        [Fact]
        public async Task UnusualFrequency_IsMediumSeverity()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 25; i++)
            {
                var o = MakeOrder(10, 100m, investorId: 22);
                o.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();
            var trigger = MakeOrder(10, 100m, investorId: 22);
            var reports = await svc.ScanOrderAsync(trigger);
            var freq = reports.FirstOrDefault(r => r.AlertType == ComplianceAlertType.UnusualFrequency);
            if (freq != null) Assert.Equal(ComplianceSeverity.Medium, freq.Severity);
        }

        // ── Resolve / Escalate ───────────────────────────────────────

        [Fact]
        public async Task ResolveReport_SetsStatusAndTimestamp()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var report = new ComplianceReport
            {
                BrokerageHouseId  = 1,
                AlertType         = ComplianceAlertType.LargeTradeAlert,
                FlaggedInvestorId = 5,
                CreatedBy         = "system"
            };
            db.ComplianceReports.Add(report);
            await db.SaveChangesAsync();

            var dto = new ResolveComplianceDto
            {
                NewStatus       = ComplianceStatus.FalsePositive,
                ResolutionNotes = "Not suspicious",
                ResolvedBy      = "analyst@firm.com"
            };
            var resolved = await svc.ResolveReportAsync(report.Id, dto);
            Assert.Equal(ComplianceStatus.FalsePositive, resolved.Status);
            Assert.NotNull(resolved.ResolvedAt);
            Assert.Equal("analyst@firm.com", resolved.ResolvedBy);
        }

        [Fact]
        public async Task ResolveReport_NotFound_ThrowsKeyNotFound()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.ResolveReportAsync(Guid.NewGuid(), new ResolveComplianceDto()));
        }

        [Fact]
        public async Task EscalateReport_SetsEscalatedAndStatus()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var report = new ComplianceReport
            {
                BrokerageHouseId  = 1,
                AlertType         = ComplianceAlertType.WashTrade,
                FlaggedInvestorId = 6,
                CreatedBy         = "system"
            };
            db.ComplianceReports.Add(report);
            await db.SaveChangesAsync();

            var escalated = await svc.EscalateReportAsync(report.Id, "Needs review", "supervisor@firm.com");
            Assert.True(escalated.IsEscalated);
            Assert.Equal(ComplianceStatus.Escalated, escalated.Status);
        }

        [Fact]
        public async Task EscalateReport_NotFound_ThrowsKeyNotFound()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.EscalateReportAsync(Guid.NewGuid(), "reason", "user"));
        }

        // ── Summary ──────────────────────────────────────────────────

        [Fact]
        public async Task Summary_CountsByStatus_Correct()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var now = DateTime.UtcNow;
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Open,      AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 1, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Open,      AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Resolved,  AlertType = ComplianceAlertType.AMLStructuring,  FlaggedInvestorId = 3, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Escalated, AlertType = ComplianceAlertType.UnusualFrequency,FlaggedInvestorId = 4, CreatedBy = "s", DetectedAt = now }
            );
            await db.SaveChangesAsync();

            var summary = await svc.GetSummaryAsync(1, now.AddDays(-1), now.AddDays(1));
            Assert.Equal(2, summary.TotalOpen);
            Assert.Equal(1, summary.TotalResolved);
            Assert.Equal(1, summary.TotalEscalated);
        }

        [Fact]
        public async Task Summary_CountsBySeverity_Correct()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var now = DateTime.UtcNow;
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 2, Severity = ComplianceSeverity.Critical, AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 1, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 2, Severity = ComplianceSeverity.High,     AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 2, Severity = ComplianceSeverity.High,     AlertType = ComplianceAlertType.AMLStructuring,  FlaggedInvestorId = 3, CreatedBy = "s", DetectedAt = now }
            );
            await db.SaveChangesAsync();

            var summary = await svc.GetSummaryAsync(2, now.AddDays(-1), now.AddDays(1));
            Assert.Equal(1, summary.CriticalCount);
            Assert.Equal(2, summary.HighCount);
        }

        [Fact]
        public async Task Summary_ByAlertType_Grouped()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var now = DateTime.UtcNow;
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 3, AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 1, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 3, AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 3, AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 3, CreatedBy = "s", DetectedAt = now }
            );
            await db.SaveChangesAsync();

            var summary = await svc.GetSummaryAsync(3, now.AddDays(-1), now.AddDays(1));
            Assert.Equal(2, summary.ByAlertType["WashTrade"]);
            Assert.Equal(1, summary.ByAlertType["LargeTradeAlert"]);
        }

        [Fact]
        public async Task Summary_ExcludesOtherBrokerageHouses()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var now = DateTime.UtcNow;
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 10, AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 1, CreatedBy = "s", DetectedAt = now },
                new ComplianceReport { BrokerageHouseId = 99, AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s", DetectedAt = now }
            );
            await db.SaveChangesAsync();

            var summary = await svc.GetSummaryAsync(10, now.AddDays(-1), now.AddDays(1));
            Assert.Equal(1, summary.TotalOpen);
        }

        // ── Filter / paging ──────────────────────────────────────────

        [Fact]
        public async Task GetReports_FilterBySeverity_ReturnsCorrectSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 1, Severity = ComplianceSeverity.Critical, AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 1, CreatedBy = "s" },
                new ComplianceReport { BrokerageHouseId = 1, Severity = ComplianceSeverity.Low,      AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s" }
            );
            await db.SaveChangesAsync();

            var result = await svc.GetReportsAsync(new ComplianceFilterDto { BrokerageHouseId = 1, Severity = ComplianceSeverity.Critical });
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, r => Assert.Equal(ComplianceSeverity.Critical, r.Severity));
        }

        [Fact]
        public async Task GetReports_FilterByStatus_ReturnsCorrectSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Open,     AlertType = ComplianceAlertType.LargeTradeAlert, FlaggedInvestorId = 1, CreatedBy = "s" },
                new ComplianceReport { BrokerageHouseId = 1, Status = ComplianceStatus.Resolved, AlertType = ComplianceAlertType.WashTrade,       FlaggedInvestorId = 2, CreatedBy = "s" }
            );
            await db.SaveChangesAsync();

            var result = await svc.GetReportsAsync(new ComplianceFilterDto { BrokerageHouseId = 1, Status = ComplianceStatus.Open });
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetReports_Paging_ReturnsCorrectPage()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 25; i++)
                db.ComplianceReports.Add(new ComplianceReport
                {
                    BrokerageHouseId  = 5,
                    AlertType         = ComplianceAlertType.LargeTradeAlert,
                    FlaggedInvestorId = i + 1,
                    CreatedBy         = "s"
                });
            await db.SaveChangesAsync();

            var page2 = await svc.GetReportsAsync(new ComplianceFilterDto { BrokerageHouseId = 5, Page = 2, PageSize = 10 });
            Assert.Equal(25, page2.TotalCount);
            Assert.Equal(10, page2.Items.Count());
        }

        [Fact]
        public async Task GetReports_FilterByInvestorId_Correct()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            db.ComplianceReports.AddRange(
                new ComplianceReport { BrokerageHouseId = 1, FlaggedInvestorId = 55, AlertType = ComplianceAlertType.LargeTradeAlert, CreatedBy = "s" },
                new ComplianceReport { BrokerageHouseId = 1, FlaggedInvestorId = 66, AlertType = ComplianceAlertType.WashTrade,       CreatedBy = "s" }
            );
            await db.SaveChangesAsync();

            var result = await svc.GetReportsAsync(new ComplianceFilterDto { InvestorId = 55 });
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Items, r => Assert.Equal(55, r.FlaggedInvestorId));
        }

        // ── Export ───────────────────────────────────────────────────

        [Fact]
        public async Task ExportReports_ReturnsCsvBytes_WithHeader()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var now = DateTime.UtcNow;
            db.ComplianceReports.Add(new ComplianceReport
            {
                BrokerageHouseId  = 1,
                AlertType         = ComplianceAlertType.LargeTradeAlert,
                FlaggedInvestorId = 1,
                CreatedBy         = "s",
                DetectedAt        = now
            });
            await db.SaveChangesAsync();

            var bytes = await svc.ExportReportsAsync(new ComplianceExportDto
            {
                BrokerageHouseId = 1,
                From = now.AddDays(-1),
                To   = now.AddDays(1)
            });
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            var csv = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.Contains("LargeTradeAlert", csv);
            Assert.Contains("AlertType", csv);
        }

        [Fact]
        public async Task ExportReports_Empty_ReturnsHeaderOnly()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bytes = await svc.ExportReportsAsync(new ComplianceExportDto
            {
                BrokerageHouseId = 999,
                From = DateTime.UtcNow.AddDays(-1),
                To   = DateTime.UtcNow.AddDays(1)
            });
            var csv = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.Contains("AlertType", csv);
        }

        // ── GetReport ────────────────────────────────────────────────

        [Fact]
        public async Task GetReport_NotFound_ReturnsNull()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            Assert.Null(await svc.GetReportAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetReport_Found_ReturnsCorrectReport()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var r = new ComplianceReport
            {
                BrokerageHouseId  = 1,
                AlertType         = ComplianceAlertType.WashTrade,
                FlaggedInvestorId = 7,
                CreatedBy         = "system"
            };
            db.ComplianceReports.Add(r);
            await db.SaveChangesAsync();

            var found = await svc.GetReportAsync(r.Id);
            Assert.NotNull(found);
            Assert.Equal(r.Id, found!.Id);
        }

        // ── Multi-alert ──────────────────────────────────────────────

        [Fact]
        public async Task ScanOrder_LargeAndFrequency_BothAlertsGenerated()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            for (int i = 0; i < 21; i++)
            {
                var o = MakeOrder(10, 100m, investorId: 30);
                o.CreatedAt = DateTime.UtcNow.AddMinutes(-i * 2);
                db.Orders.Add(o);
            }
            await db.SaveChangesAsync();

            // Large trade also
            var trigger = MakeOrder(2000, 6000m, investorId: 30);
            var reports = await svc.ScanOrderAsync(trigger);
            Assert.True(reports.Count >= 2, $"Expected 2+ alerts, got {reports.Count}");
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.LargeTradeAlert);
            Assert.Contains(reports, r => r.AlertType == ComplianceAlertType.UnusualFrequency);
        }

        [Fact]
        public async Task ScanOrder_CleanOrder_NoReports()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var order = MakeOrder(10, 100m); // 1000 BDT — clean
            var reports = await svc.ScanOrderAsync(order);
            Assert.Empty(reports);
        }
    }
}
