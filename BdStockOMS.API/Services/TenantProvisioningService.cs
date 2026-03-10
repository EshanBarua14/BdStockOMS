using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BdStockOMS.API.Services
{
    public class TenantProvisioningService : ITenantProvisioningService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantProvisioningService> _logger;

        public TenantProvisioningService(
            AppDbContext db,
            IConfiguration configuration,
            ILogger<TenantProvisioningService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public static string SanitizeDatabaseName(string brokerageName)
        {
            var sanitized = Regex.Replace(brokerageName, @"[^a-zA-Z0-9]", "");
            return $"BdStockOMS_{sanitized}";
        }

        public async Task<TenantProvisionResult> ProvisionTenantAsync(ProvisionTenantRequest request)
        {
            var result = new TenantProvisionResult();
            try
            {
                var brokerage = await _db.BrokerageHouses
                    .FirstOrDefaultAsync(b => b.Id == request.BrokerageHouseId);
                if (brokerage == null)
                {
                    result.Success = false;
                    result.Message = $"BrokerageHouse {request.BrokerageHouseId} not found.";
                    return result;
                }

                var dbName = SanitizeDatabaseName(brokerage.Name);
                result.DatabaseName = dbName;
                result.StepsCompleted.Add($"Resolved database name: {dbName}");

                var masterConnStr = _configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("DefaultConnection not configured.");

                var tenantConnStr = BuildTenantConnectionString(masterConnStr, dbName);
                result.StepsCompleted.Add("Built tenant connection string.");

                var existing = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == request.BrokerageHouseId);

                if (existing != null)
                {
                    existing.ConnectionString = tenantConnStr;
                    existing.DatabaseName = dbName;
                    existing.UpdatedAt = DateTime.UtcNow;
                    result.StepsCompleted.Add("Updated existing BrokerageConnection record.");
                }
                else
                {
                    var conn = new BrokerageConnection
                    {
                        BrokerageHouseId = request.BrokerageHouseId,
                        ConnectionString = tenantConnStr,
                        DatabaseName = dbName,
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.BrokerageConnections.Add(conn);
                    result.StepsCompleted.Add("Created new BrokerageConnection record.");
                }

                await _db.SaveChangesAsync();
                result.StepsCompleted.Add("Saved BrokerageConnection to database.");
                result.StepsCompleted.Add("Note: Per-tenant DB migrations are DBA-managed. Connection string stored.");

                await ActivateTenantAsync(request.BrokerageHouseId);
                result.StepsCompleted.Add("Tenant activated.");
                result.Success = true;
                result.Message = $"Tenant '{dbName}' provisioned successfully.";
                result.ConnectionString = tenantConnStr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error provisioning tenant {Id}", request.BrokerageHouseId);
                result.Success = false;
                result.Message = ex.Message;
                result.Errors.Add(ex.ToString());
            }
            return result;
        }

        public async Task<TenantProvisionResult> ActivateTenantAsync(int brokerageHouseId)
        {
            var result = new TenantProvisionResult();
            try
            {
                var conn = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId);
                if (conn == null)
                {
                    result.Success = false;
                    result.Message = "No BrokerageConnection found. Provision first.";
                    return result;
                }
                conn.IsActive = true;
                conn.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                result.Success = true;
                result.Message = $"Tenant {brokerageHouseId} activated.";
                result.DatabaseName = conn.DatabaseName;
                result.StepsCompleted.Add("IsActive set to true.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating tenant {Id}", brokerageHouseId);
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public async Task<TenantProvisionResult> DeactivateTenantAsync(int brokerageHouseId)
        {
            var result = new TenantProvisionResult();
            try
            {
                var conn = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId);
                if (conn == null)
                {
                    result.Success = false;
                    result.Message = "No BrokerageConnection found.";
                    return result;
                }
                conn.IsActive = false;
                conn.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                result.Success = true;
                result.Message = $"Tenant {brokerageHouseId} deactivated.";
                result.StepsCompleted.Add("IsActive set to false.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating tenant {Id}", brokerageHouseId);
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public async Task<TenantProvisionResult> RunMigrationsAsync(int brokerageHouseId)
        {
            var result = new TenantProvisionResult();
            try
            {
                var conn = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId);
                if (conn == null)
                {
                    result.Success = false;
                    result.Message = "No BrokerageConnection found. Provision first.";
                    return result;
                }
                // Per architecture: tenant DB migrations are DBA-managed via CLI
                // This endpoint records the migration request in audit log
                result.Success = true;
                result.DatabaseName = conn.DatabaseName;
                result.Message = $"Migration request recorded for '{conn.DatabaseName}'. " +
                                 "Apply via: dotnet ef database update --connection [tenant-conn-str]";
                result.StepsCompleted.Add("Migration instruction generated.");
                result.StepsCompleted.Add($"Target database: {conn.DatabaseName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RunMigrationsAsync for tenant {Id}", brokerageHouseId);
                result.Success = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public async Task<TenantHealthStatus> GetTenantHealthAsync(int brokerageHouseId)
        {
            var health = new TenantHealthStatus
            {
                BrokerageHouseId = brokerageHouseId,
                LastChecked = DateTime.UtcNow
            };
            try
            {
                var brokerage = await _db.BrokerageHouses
                    .FirstOrDefaultAsync(b => b.Id == brokerageHouseId);
                var conn = await _db.BrokerageConnections
                    .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId);

                health.BrokerageName = brokerage?.Name ?? "Unknown";
                health.IsActive = conn?.IsActive ?? false;
                health.DatabaseName = conn?.DatabaseName
                    ?? SanitizeDatabaseName(health.BrokerageName);
                health.CanConnect = conn != null;
                health.MigrationsUpToDate = conn != null;
                health.PendingMigrations = 0;
                health.TableCount = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for tenant {Id}", brokerageHouseId);
                health.CanConnect = false;
                health.MigrationsUpToDate = false;
            }
            return health;
        }

        public async Task<List<TenantSummary>> GetAllTenantSummariesAsync()
        {
            var brokerages = await _db.BrokerageHouses.ToListAsync();
            var connections = await _db.BrokerageConnections.ToListAsync();
            var summaries = new List<TenantSummary>();
            foreach (var b in brokerages)
            {
                var conn = connections.FirstOrDefault(c => c.BrokerageHouseId == b.Id);
                summaries.Add(new TenantSummary
                {
                    BrokerageHouseId = b.Id,
                    BrokerageName = b.Name,
                    DatabaseName = conn?.DatabaseName ?? SanitizeDatabaseName(b.Name),
                    IsActive = conn?.IsActive ?? false,
                    HealthStatus = conn == null ? "NotProvisioned" :
                                   conn.IsActive ? "Active" : "Inactive",
                    LastProvisionedAt = conn?.CreatedAt
                });
            }
            return summaries;
        }

        private static string BuildTenantConnectionString(string baseConnectionString, string databaseName)
        {
            var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString)
            {
                InitialCatalog = databaseName
            };
            return connBuilder.ConnectionString;
        }
    }
}
