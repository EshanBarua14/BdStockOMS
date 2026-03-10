using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface ITenantProvisioningService
    {
        Task<TenantProvisionResult> ProvisionTenantAsync(ProvisionTenantRequest request);
        Task<TenantProvisionResult> ActivateTenantAsync(int brokerageHouseId);
        Task<TenantProvisionResult> DeactivateTenantAsync(int brokerageHouseId);
        Task<TenantProvisionResult> RunMigrationsAsync(int brokerageHouseId);
        Task<TenantHealthStatus> GetTenantHealthAsync(int brokerageHouseId);
        Task<List<TenantSummary>> GetAllTenantSummariesAsync();
    }

    public class ProvisionTenantRequest
    {
        public int BrokerageHouseId { get; set; }
        public string BrokerageName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public bool RunSeedData { get; set; } = true;
    }

    public class TenantProvisionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DatabaseName { get; set; }
        public string? ConnectionString { get; set; }
        public List<string> StepsCompleted { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime ProvisionedAt { get; set; } = DateTime.UtcNow;
    }

    public class TenantHealthStatus
    {
        public int BrokerageHouseId { get; set; }
        public string BrokerageName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanConnect { get; set; }
        public bool MigrationsUpToDate { get; set; }
        public int PendingMigrations { get; set; }
        public long TableCount { get; set; }
        public DateTime? LastChecked { get; set; }
        public string Status => IsActive && CanConnect && MigrationsUpToDate ? "Healthy" :
                                IsActive && CanConnect ? "NeedsMigration" :
                                IsActive ? "ConnectionFailed" : "Inactive";
    }

    public class TenantSummary
    {
        public int BrokerageHouseId { get; set; }
        public string BrokerageName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string HealthStatus { get; set; } = string.Empty;
        public DateTime? LastProvisionedAt { get; set; }
    }
}
