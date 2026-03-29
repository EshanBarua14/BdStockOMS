using BdStockOMS.API.Authorization;
using BdStockOMS.API.Models;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day81Tests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    // ── Permissions constants ────────────────────────────────────────

    [Fact]
    public void Permissions_All_ReturnsAtLeast60()
        => Assert.True(Permissions.All().Count() >= 60);

    [Fact]
    public void Permissions_AllKeys_AreUnique()
    {
        var all = Permissions.All().ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void Permissions_AllKeys_HaveDotSeparator()
    {
        foreach (var p in Permissions.All())
            Assert.Contains(".", p);
    }

    [Theory]
    [InlineData("orders.place")]
    [InlineData("orders.cancel")]
    [InlineData("orders.amend")]
    [InlineData("orders.view.own")]
    [InlineData("orders.view.all")]
    [InlineData("portfolio.view.own")]
    [InlineData("kyc.approve")]
    [InlineData("rms.set_limits")]
    [InlineData("admin.permissions")]
    [InlineData("compliance.freeze")]
    [InlineData("tenant.provision")]
    public void Permissions_KeyExists(string key)
        => Assert.Contains(key, Permissions.All());

    // ── Default permissions by role ──────────────────────────────────

    [Theory]
    [InlineData("Investor")]
    [InlineData("Trader")]
    [InlineData("Admin")]
    [InlineData("CCD")]
    [InlineData("Compliance")]
    [InlineData("SuperAdmin")]
    public void Permissions_DefaultsForRole_NotEmpty(string role)
        => Assert.NotEmpty(Permissions.DefaultsForRole(role));

    [Fact]
    public void Permissions_SuperAdmin_GetsAll()
    {
        var superAdminPerms = Permissions.DefaultsForRole("SuperAdmin").ToList();
        var all = Permissions.All().ToList();
        Assert.Equal(all.Count, superAdminPerms.Count);
    }

    [Fact]
    public void Permissions_Investor_CanPlaceOrders()
        => Assert.Contains("orders.place", Permissions.DefaultsForRole("Investor"));

    [Fact]
    public void Permissions_Investor_CannotViewAllOrders()
        => Assert.DoesNotContain("orders.view.all", Permissions.DefaultsForRole("Investor"));

    [Fact]
    public void Permissions_Trader_CanViewAllOrders()
        => Assert.Contains("orders.view.all", Permissions.DefaultsForRole("Trader"));

    [Fact]
    public void Permissions_Admin_CanSetRmsLimits()
        => Assert.Contains("rms.set_limits", Permissions.DefaultsForRole("Admin"));

    [Fact]
    public void Permissions_UnknownRole_ReturnsEmpty()
        => Assert.Empty(Permissions.DefaultsForRole("UnknownRole"));

    // ── BOGroup model ────────────────────────────────────────────────

    [Fact]
    public void BOGroup_DefaultsCorrect()
    {
        var g = new BOGroup();
        Assert.True(g.IsActive);
        Assert.Equal(string.Empty, g.Name);
        Assert.Null(g.Description);
        Assert.Empty(g.Members);
    }

    [Fact]
    public async Task BOGroup_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.BOGroups.Add(new BOGroup { Name = "Group A", BrokerageHouseId = 1 });
        await db.SaveChangesAsync();

        var g = await db.BOGroups.FirstOrDefaultAsync(g => g.Name == "Group A");
        Assert.NotNull(g);
        Assert.True(g.IsActive);
        Assert.Equal(1, g.BrokerageHouseId);
    }

    [Fact]
    public async Task BOGroupMember_CanAddAndRemove()
    {
        using var db = CreateDb();
        db.BOGroups.Add(new BOGroup { Id = 1, Name = "Group B", BrokerageHouseId = 1 });
        await db.SaveChangesAsync();

        db.BOGroupMembers.Add(new BOGroupMember { BOGroupId = 1, UserId = 42 });
        await db.SaveChangesAsync();

        var count = await db.BOGroupMembers.CountAsync(m => m.BOGroupId == 1);
        Assert.Equal(1, count);

        var member = await db.BOGroupMembers.FirstAsync();
        db.BOGroupMembers.Remove(member);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.BOGroupMembers.CountAsync());
    }

    [Fact]
    public async Task BOGroup_MultiTenant_Isolation()
    {
        using var db = CreateDb();
        db.BOGroups.AddRange(
            new BOGroup { Name = "T1 Group", BrokerageHouseId = 1 },
            new BOGroup { Name = "T2 Group", BrokerageHouseId = 2 }
        );
        await db.SaveChangesAsync();

        var t1 = await db.BOGroups.Where(g => g.BrokerageHouseId == 1).CountAsync();
        var t2 = await db.BOGroups.Where(g => g.BrokerageHouseId == 2).CountAsync();
        Assert.Equal(1, t1);
        Assert.Equal(1, t2);
    }

    // ── Basket model ─────────────────────────────────────────────────

    [Fact]
    public void Basket_DefaultsCorrect()
    {
        var b = new Basket();
        Assert.True(b.IsActive);
        Assert.Equal(string.Empty, b.Name);
        Assert.Empty(b.Stocks);
    }

    [Fact]
    public async Task Basket_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.Baskets.Add(new Basket { Name = "Tech Stocks", BrokerageHouseId = 1 });
        await db.SaveChangesAsync();

        var b = await db.Baskets.FirstOrDefaultAsync(b => b.Name == "Tech Stocks");
        Assert.NotNull(b);
        Assert.Equal(1, b.BrokerageHouseId);
    }

    [Fact]
    public async Task BasketStock_CanAddAndRemove()
    {
        using var db = CreateDb();
        db.Baskets.Add(new Basket { Id = 1, Name = "B1", BrokerageHouseId = 1 });
        await db.SaveChangesAsync();

        db.BasketStocks.Add(new BasketStock { BasketId = 1, StockId = 5, MaxOrderValue = 50000m });
        await db.SaveChangesAsync();

        var bs = await db.BasketStocks.FirstOrDefaultAsync(s => s.BasketId == 1);
        Assert.NotNull(bs);
        Assert.Equal(50000m, bs.MaxOrderValue);

        db.BasketStocks.Remove(bs);
        await db.SaveChangesAsync();
        Assert.Equal(0, await db.BasketStocks.CountAsync());
    }

    [Fact]
    public async Task Basket_Deactivate()
    {
        using var db = CreateDb();
        db.Baskets.Add(new Basket { Name = "Old Basket", BrokerageHouseId = 1 });
        await db.SaveChangesAsync();

        var b = await db.Baskets.FirstAsync();
        b.IsActive = false;
        await db.SaveChangesAsync();

        var active = await db.Baskets.Where(x => x.IsActive).CountAsync();
        Assert.Equal(0, active);
    }

    // ── UserPermission model ─────────────────────────────────────────

    [Fact]
    public void UserPermission_IsActive_TrueWhenGrantedNoExpiry()
    {
        var p = new UserPermission { IsGranted = true, ExpiresAt = null };
        Assert.True(p.IsActive);
    }

    [Fact]
    public void UserPermission_IsActive_FalseWhenExpired()
    {
        var p = new UserPermission { IsGranted = true, ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        Assert.False(p.IsActive);
    }

    [Fact]
    public void UserPermission_IsActive_FalseWhenRevoked()
    {
        var p = new UserPermission { IsGranted = false, ExpiresAt = null };
        Assert.False(p.IsActive);
    }

    [Fact]
    public void UserPermission_IsActive_TrueWhenFutureExpiry()
    {
        var p = new UserPermission { IsGranted = true, ExpiresAt = DateTime.UtcNow.AddDays(30) };
        Assert.True(p.IsActive);
    }

    [Fact]
    public async Task UserPermission_CanGrantAndRevoke()
    {
        using var db = CreateDb();
        db.UserPermissions.Add(new UserPermission
        {
            UserId = 1, Permission = "orders.place",
            Module = "orders", IsGranted = true, GrantedByUserId = 99
        });
        await db.SaveChangesAsync();

        var p = await db.UserPermissions.FirstAsync();
        Assert.True(p.IsGranted);

        p.IsGranted = false;
        await db.SaveChangesAsync();

        var updated = await db.UserPermissions.FirstAsync();
        Assert.False(updated.IsGranted);
    }
}
