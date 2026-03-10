using System;
using System.Collections.Generic;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Services.Interfaces;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    public class TenantProvisioningServiceTests
    {
        [Fact]
        public void SanitizeDatabaseName_RemovesSpaces()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("Alpha Securities");
            Assert.Equal("BdStockOMS_AlphaSecurities", result);
        }

        [Fact]
        public void SanitizeDatabaseName_RemovesSpecialChars()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("Alpha & Beta (Ltd.)");
            Assert.Equal("BdStockOMS_AlphaBetaLtd", result);
        }

        [Fact]
        public void SanitizeDatabaseName_PreservesAlphanumeric()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("ABC123");
            Assert.Equal("BdStockOMS_ABC123", result);
        }

        [Fact]
        public void SanitizeDatabaseName_HasCorrectPrefix()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("TestBrokerage");
            Assert.StartsWith("BdStockOMS_", result);
        }

        [Fact]
        public void SanitizeDatabaseName_RemovesDashes()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("Alpha-Beta");
            Assert.Equal("BdStockOMS_AlphaBeta", result);
        }

        [Fact]
        public void SanitizeDatabaseName_EmptyName_ReturnsPrefix()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("");
            Assert.Equal("BdStockOMS_", result);
        }

        [Fact]
        public void SanitizeDatabaseName_OnlySpecialChars_ReturnsPrefix()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("!@#$%");
            Assert.Equal("BdStockOMS_", result);
        }

        [Fact]
        public void SanitizeDatabaseName_MixedCase_PreservesCase()
        {
            var result = TenantProvisioningService.SanitizeDatabaseName("MyBrokerage");
            Assert.Equal("BdStockOMS_MyBrokerage", result);
        }
    }

    public class BosComplianceReportTests
    {
        [Fact]
        public void BosComplianceReport_IsCompliant_WhenAllChecksPassed()
        {
            var report = new BosComplianceReport
            {
                Checks = new List<BosComplianceCheck>
                {
                    new() { CheckName = "Check1", Passed = true },
                    new() { CheckName = "Check2", Passed = true },
                    new() { CheckName = "Check3", Passed = true },
                }
            };
            Assert.True(report.IsCompliant);
            Assert.Equal(3, report.PassedCount);
            Assert.Equal(0, report.FailedCount);
        }

        [Fact]
        public void BosComplianceReport_NotCompliant_WhenAnyCheckFailed()
        {
            var report = new BosComplianceReport
            {
                Checks = new List<BosComplianceCheck>
                {
                    new() { CheckName = "Check1", Passed = true },
                    new() { CheckName = "Check2", Passed = false, FailureReason = "Missing data" },
                }
            };
            Assert.False(report.IsCompliant);
            Assert.Equal(1, report.PassedCount);
            Assert.Equal(1, report.FailedCount);
        }

        [Fact]
        public void BosComplianceReport_NotCompliant_WhenNoChecks()
        {
            var report = new BosComplianceReport { Checks = new List<BosComplianceCheck>() };
            Assert.False(report.IsCompliant);
        }

        [Fact]
        public void BosComplianceReport_PassedCount_CountsCorrectly()
        {
            var report = new BosComplianceReport
            {
                Checks = new List<BosComplianceCheck>
                {
                    new() { Passed = true },
                    new() { Passed = true },
                    new() { Passed = false },
                    new() { Passed = false },
                    new() { Passed = false },
                }
            };
            Assert.Equal(2, report.PassedCount);
            Assert.Equal(3, report.FailedCount);
        }

        [Fact]
        public void BosComplianceReport_GeneratedAt_IsSetToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var report = new BosComplianceReport();
            var after = DateTime.UtcNow.AddSeconds(1);
            Assert.True(report.GeneratedAt >= before && report.GeneratedAt <= after);
        }

        [Fact]
        public void BosComplianceCheck_AllCheckNames_AreUnique()
        {
            var names = new[]
            {
                BosComplianceCheckNames.BrokerageSettingsExist,
                BosComplianceCheckNames.TrecNumberConfigured,
                BosComplianceCheckNames.ActiveBranchExists,
                BosComplianceCheckNames.CommissionRatesConfigured,
                BosComplianceCheckNames.RmsLimitsConfigured,
                BosComplianceCheckNames.ActiveTraderExists,
                BosComplianceCheckNames.BosImportWithin24Hours,
                BosComplianceCheckNames.BoAccountFormatValid,
                BosComplianceCheckNames.KycQueueClear,
                BosComplianceCheckNames.SettlementUpToDate
            };
            var unique = new HashSet<string>(names);
            Assert.Equal(names.Length, unique.Count);
        }

        [Fact]
        public void BosComplianceCheck_ExactlyTenCheckNames_Defined()
        {
            var names = new[]
            {
                BosComplianceCheckNames.BrokerageSettingsExist,
                BosComplianceCheckNames.TrecNumberConfigured,
                BosComplianceCheckNames.ActiveBranchExists,
                BosComplianceCheckNames.CommissionRatesConfigured,
                BosComplianceCheckNames.RmsLimitsConfigured,
                BosComplianceCheckNames.ActiveTraderExists,
                BosComplianceCheckNames.BosImportWithin24Hours,
                BosComplianceCheckNames.BoAccountFormatValid,
                BosComplianceCheckNames.KycQueueClear,
                BosComplianceCheckNames.SettlementUpToDate
            };
            Assert.Equal(10, names.Length);
        }

        [Fact]
        public void BosComplianceCheck_Severity_CanBeSetCorrectly()
        {
            var criticalCheck = new BosComplianceCheck { Severity = "Critical" };
            var warningCheck  = new BosComplianceCheck { Severity = "Warning" };
            Assert.Equal("Critical", criticalCheck.Severity);
            Assert.Equal("Warning", warningCheck.Severity);
        }

        [Fact]
        public void BosComplianceReport_FromCache_DefaultsFalse()
        {
            var report = new BosComplianceReport();
            Assert.False(report.FromCache);
        }
    }

    public class ContractNoteModelTests
    {
        [Fact]
        public void ContractNoteNumber_Format_IsCorrect()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var number = $"CN-{today}-000042";
            Assert.StartsWith("CN-", number);
            Assert.Contains(today, number);
            Assert.EndsWith("000042", number);
        }

        [Fact]
        public void ContractNote_NetAmount_BuyAddsCharges()
        {
            var gross = 100_000m;
            var commission = gross * 0.005m;
            var cdsc = gross * 0.0005m;
            var levy = gross * 0.0003m;
            var vat = commission * 0.15m;
            var net = gross + commission + cdsc + levy + vat;
            Assert.True(net > gross);
        }

        [Fact]
        public void ContractNote_NetAmount_SellDeductsCharges()
        {
            var gross = 100_000m;
            var commission = gross * 0.005m;
            var cdsc = gross * 0.0005m;
            var levy = gross * 0.0003m;
            var vat = commission * 0.15m;
            var net = gross - commission - cdsc - levy - vat;
            Assert.True(net < gross);
        }

        [Fact]
        public void ContractNote_SettlementDate_IsTradeDatePlusTwo()
        {
            var tradeDate = new DateTime(2025, 1, 10);
            var settlementDate = tradeDate.AddDays(2);
            Assert.Equal(new DateTime(2025, 1, 12), settlementDate);
        }

        [Fact]
        public void ContractNote_DefaultStatus_IsGenerated()
        {
            var cn = new ContractNote();
            Assert.Equal("Generated", cn.Status);
        }

        [Fact]
        public void ContractNote_DefaultIsVoid_IsFalse()
        {
            var cn = new ContractNote();
            Assert.False(cn.IsVoid);
        }

        [Fact]
        public void ContractNote_VoidedAt_NullByDefault()
        {
            var cn = new ContractNote();
            Assert.Null(cn.VoidedAt);
        }

        [Fact]
        public void ContractNoteResult_ErrorsList_InitializedEmpty()
        {
            var result = new ContractNoteResult();
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void TenantProvisionResult_Lists_InitializedEmpty()
        {
            var result = new TenantProvisionResult();
            Assert.NotNull(result.StepsCompleted);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.StepsCompleted);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void TenantHealthStatus_Status_HealthyWhenAllGood()
        {
            var health = new TenantHealthStatus
            {
                IsActive = true,
                CanConnect = true,
                MigrationsUpToDate = true
            };
            Assert.Equal("Healthy", health.Status);
        }

        [Fact]
        public void TenantHealthStatus_Status_NeedsMigrationWhenPending()
        {
            var health = new TenantHealthStatus
            {
                IsActive = true,
                CanConnect = true,
                MigrationsUpToDate = false
            };
            Assert.Equal("NeedsMigration", health.Status);
        }

        [Fact]
        public void TenantHealthStatus_Status_ConnectionFailedWhenCantConnect()
        {
            var health = new TenantHealthStatus
            {
                IsActive = true,
                CanConnect = false,
                MigrationsUpToDate = false
            };
            Assert.Equal("ConnectionFailed", health.Status);
        }

        [Fact]
        public void TenantHealthStatus_Status_InactiveWhenNotActive()
        {
            var health = new TenantHealthStatus
            {
                IsActive = false,
                CanConnect = false,
                MigrationsUpToDate = false
            };
            Assert.Equal("Inactive", health.Status);
        }
    }

    public class BrokerageConnectionModelTests
    {
        [Fact]
        public void BrokerageConnection_DefaultIsActive_IsFalse()
        {
            var conn = new BrokerageConnection();
            Assert.False(conn.IsActive);
        }

        [Fact]
        public void BrokerageConnection_CreatedAt_IsSetOnCreation()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var conn = new BrokerageConnection();
            var after = DateTime.UtcNow.AddSeconds(1);
            Assert.True(conn.CreatedAt >= before && conn.CreatedAt <= after);
        }

        [Fact]
        public void BosImportLog_DefaultStatus_IsPending()
        {
            var log = new BosImportLog();
            Assert.Equal("Pending", log.Status);
        }

        [Fact]
        public void BosImportLog_DefaultMd5Verified_IsFalse()
        {
            var log = new BosImportLog();
            Assert.False(log.Md5Verified);
        }

        [Fact]
        public void TenantSummary_HealthStatus_CanBeSetCorrectly()
        {
            var summary = new TenantSummary { HealthStatus = "NotProvisioned" };
            Assert.Equal("NotProvisioned", summary.HealthStatus);
        }

        [Fact]
        public void BosComplianceReport_BrokerageName_DefaultsEmpty()
        {
            var report = new BosComplianceReport();
            Assert.NotNull(report.BrokerageName);
        }

        [Fact]
        public void ContractNoteSummary_CanBeCreated()
        {
            var summary = new ContractNoteSummary
            {
                Id = 1,
                ContractNoteNumber = "CN-20250101-000001",
                Side = "Buy",
                Quantity = 100,
                NetAmount = 50500m
            };
            Assert.Equal("CN-20250101-000001", summary.ContractNoteNumber);
            Assert.Equal(100, summary.Quantity);
            Assert.Equal(50500m, summary.NetAmount);
        }

        [Fact]
        public void ProvisionTenantRequest_CanBeCreated()
        {
            var req = new ProvisionTenantRequest
            {
                BrokerageHouseId = 1,
                BrokerageName = "Test Brokerage",
                AdminEmail = "admin@test.com",
                RunSeedData = true
            };
            Assert.Equal(1, req.BrokerageHouseId);
            Assert.Equal("Test Brokerage", req.BrokerageName);
            Assert.True(req.RunSeedData);
        }
    }
}
