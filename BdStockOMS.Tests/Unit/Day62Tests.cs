using BdStockOMS.API.Data;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class Day62AuditServiceTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task LogAsync_ValidEntry_SavesAuditLog()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        await svc.LogAsync("admin", "CREATE", "FeeStructure", null, "DSE Standard", "127.0.0.1");
        Assert.Equal(1, db.AuditLogs.Count());
    }

    [Fact]
    public async Task LogAsync_SetsIpAddress()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        await svc.LogAsync("user1", "DELETE", "ApiKey", "abc", null, "192.168.1.1");
        Assert.Equal("192.168.1.1", db.AuditLogs.First().IpAddress);
    }

    [Fact]
    public async Task LogAsync_MultipleEntries_AllPersisted()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        await svc.LogAsync("admin", "CREATE", "Role",    null, "Dealer", "127.0.0.1");
        await svc.LogAsync("admin", "UPDATE", "Setting", "k",  "v",      "127.0.0.1");
        await svc.LogAsync("admin", "DELETE", "ApiKey",  "1",  null,     "127.0.0.1");
        Assert.Equal(3, db.AuditLogs.Count());
    }

    [Fact]
    public async Task LogAsync_NeverThrows_EvenWithNullDetail()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        var ex  = await Record.ExceptionAsync(() =>
            svc.LogAsync("admin", "READ", "Health", null, null, "127.0.0.1"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task GetLogsAsync_EmptyDb_ReturnsTotalZero()
    {
        var db     = CreateDb();
        var svc    = new AdminAuditService(db);
        var result = await svc.GetLogsAsync(1, 10, null, null, null, null, null);
        dynamic d  = result;
        Assert.Equal(0, (int)d.total);
    }

    [Fact]
    public async Task GetLogsAsync_DateFilter_FiltersCorrectly()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        await svc.LogAsync("admin", "CREATE", "A", null, null, "127.0.0.1");
        var future = DateTime.UtcNow.AddDays(1);
        var result = await svc.GetLogsAsync(1, 10, null, null, null, future, null);
        dynamic d  = result;
        Assert.Equal(0, (int)d.total);
    }

    [Fact]
    public async Task ExportCsvAsync_ReturnsHeader()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        var csv = await svc.ExportCsvAsync(null, null);
        Assert.StartsWith("Timestamp,Action,IpAddress", csv);
    }

    [Fact]
    public async Task ExportCsvAsync_EmptyDb_OnlyHeader()
    {
        var db    = CreateDb();
        var svc   = new AdminAuditService(db);
        var csv   = await svc.ExportCsvAsync(null, null);
        var lines = csv.Trim().Split("\n");
        Assert.Single(lines);
    }

    [Fact]
    public async Task ExportCsvAsync_WithLogs_ContainsEntries()
    {
        var db  = CreateDb();
        var svc = new AdminAuditService(db);
        await svc.LogAsync("admin", "CREATE", "FeeStructure", null, null, "10.0.0.1");
        var csv   = await svc.ExportCsvAsync(null, null);
        var lines = csv.Trim().Split("\n");
        Assert.True(lines.Length >= 2);
    }
}

public class Day62SystemHealthTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetHealthSnapshot_ReturnsObject()
    {
        var result = await new SystemHealthService(CreateDb()).GetHealthSnapshotAsync();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetHealthSnapshot_ContainsDbStatus()
    {
        var result = await new SystemHealthService(CreateDb()).GetHealthSnapshotAsync();
        var props  = result.GetType().GetProperties().Select(p => p.Name);
        Assert.Contains("dbStatus", props);
    }

    [Fact]
    public async Task GetHealthSnapshot_ContainsUptimeSeconds()
    {
        var result = await new SystemHealthService(CreateDb()).GetHealthSnapshotAsync();
        var prop   = result.GetType().GetProperty("uptimeSeconds");
        Assert.NotNull(prop);
        Assert.True((long)prop.GetValue(result)! >= 0);
    }

    [Fact]
    public async Task GetHealthSnapshot_ApiVersionPresent()
    {
        var result = await new SystemHealthService(CreateDb()).GetHealthSnapshotAsync();
        var prop   = result.GetType().GetProperty("apiVersion");
        Assert.NotNull(prop);
        Assert.NotNull(prop.GetValue(result));
    }

    [Fact]
    public async Task GetHealthSnapshot_NeverThrows()
    {
        var ex = await Record.ExceptionAsync(() =>
            new SystemHealthService(CreateDb()).GetHealthSnapshotAsync());
        Assert.Null(ex);
    }
}
