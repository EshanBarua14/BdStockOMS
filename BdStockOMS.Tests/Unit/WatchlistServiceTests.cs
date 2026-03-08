using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class WatchlistServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private WatchlistService CreateService(AppDbContext db) => new(db);

    private async Task SeedDataAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "Investor", Email = "inv@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "BRAC", CompanyName = "BRAC Bank",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 50m, IsActive = true
        });
        db.Stocks.Add(new Stock
        {
            Id = 2, TradingCode = "SQUARE", CompanyName = "Square Pharma",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 200m, IsActive = true
        });
        await db.SaveChangesAsync();
    }

    // ── CREATE WATCHLIST ──────────────────────────────────────

    [Fact]
    public async Task CreateWatchlist_ValidName_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateWatchlistAsync(1, "My Picks");

        Assert.True(result.IsSuccess);
        Assert.Equal("My Picks", result.Value!.Name);
        Assert.False(result.Value.IsDefault);
    }

    [Fact]
    public async Task CreateWatchlist_EmptyName_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateWatchlistAsync(1, "");
        Assert.False(result.IsSuccess);
        Assert.Equal("NAME_REQUIRED", result.ErrorCode);
    }

    [Fact]
    public async Task CreateWatchlist_NameTooLong_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateWatchlistAsync(1, new string('A', 51));
        Assert.False(result.IsSuccess);
        Assert.Equal("NAME_TOO_LONG", result.ErrorCode);
    }

    [Fact]
    public async Task CreateWatchlist_ExceedsMax_ReturnsFailure()
    {
        var db = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        for (int i = 0; i < 10; i++)
            await svc.CreateWatchlistAsync(1, $"List {i}");

        var result = await svc.CreateWatchlistAsync(1, "One More");
        Assert.False(result.IsSuccess);
        Assert.Equal("MAX_WATCHLISTS", result.ErrorCode);
    }

    // ── DELETE WATCHLIST ──────────────────────────────────────

    [Fact]
    public async Task DeleteWatchlist_NonDefault_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateWatchlistAsync(1, "Temp List");
        var result  = await svc.DeleteWatchlistAsync(created.Value!.Id, 1);

        Assert.True(result.IsSuccess);
        var count = await db.Watchlists.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteWatchlist_Default_ReturnsFailure()
    {
        var db = CreateDb();
        await SeedDataAsync(db);
        await db.Watchlists.AddAsync(new Watchlist
        {
            UserId = 1, Name = "My Watchlist", IsDefault = true
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var wl     = await db.Watchlists.FirstAsync();
        var result = await svc.DeleteWatchlistAsync(wl.Id, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("CANNOT_DELETE_DEFAULT", result.ErrorCode);
    }

    // ── ADD STOCK ─────────────────────────────────────────────

    [Fact]
    public async Task AddStock_ValidStock_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var wl     = await svc.CreateWatchlistAsync(1, "Tech");
        var result = await svc.AddStockAsync(wl.Value!.Id, 1, 1);

        Assert.True(result.IsSuccess);
        var count = await db.WatchlistItems.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddStock_DuplicateStock_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var wl = await svc.CreateWatchlistAsync(1, "Tech");
        await svc.AddStockAsync(wl.Value!.Id, 1, 1);
        var result = await svc.AddStockAsync(wl.Value.Id, 1, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("ALREADY_EXISTS", result.ErrorCode);
    }

    [Fact]
    public async Task AddStock_InvalidStock_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var wl     = await svc.CreateWatchlistAsync(1, "Tech");
        var result = await svc.AddStockAsync(wl.Value!.Id, 1, 999);

        Assert.False(result.IsSuccess);
        Assert.Equal("STOCK_NOT_FOUND", result.ErrorCode);
    }

    // ── REMOVE STOCK ──────────────────────────────────────────

    [Fact]
    public async Task RemoveStock_ExistingStock_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var wl = await svc.CreateWatchlistAsync(1, "Tech");
        await svc.AddStockAsync(wl.Value!.Id, 1, 1);
        var result = await svc.RemoveStockAsync(wl.Value.Id, 1, 1);

        Assert.True(result.IsSuccess);
        var count = await db.WatchlistItems.CountAsync();
        Assert.Equal(0, count);
    }

    // ── REORDER ───────────────────────────────────────────────

    [Fact]
    public async Task ReorderStocks_ValidOrder_UpdatesSortOrder()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var wl = await svc.CreateWatchlistAsync(1, "Tech");
        await svc.AddStockAsync(wl.Value!.Id, 1, 1);
        await svc.AddStockAsync(wl.Value.Id, 1, 2);

        var result = await svc.ReorderStocksAsync(wl.Value.Id, 1,
            new List<(int, int)> { (1, 2), (2, 1) });

        Assert.True(result.IsSuccess);
        var item1 = await db.WatchlistItems.FirstAsync(i => i.StockId == 1);
        Assert.Equal(2, item1.SortOrder);
    }

    // ── GET WATCHLISTS ────────────────────────────────────────

    [Fact]
    public async Task GetMyWatchlists_ReturnsAllWatchlists()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        await svc.CreateWatchlistAsync(1, "List A");
        await svc.CreateWatchlistAsync(1, "List B");

        var result = await svc.GetMyWatchlistsAsync(1);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task EnsureDefaultWatchlist_CreatesIfMissing()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        await svc.EnsureDefaultWatchlistAsync(1);

        var def = await db.Watchlists.FirstOrDefaultAsync(w => w.IsDefault && w.UserId == 1);
        Assert.NotNull(def);
        Assert.Equal("My Watchlist", def!.Name);
    }

    [Fact]
    public async Task EnsureDefaultWatchlist_DoesNotDuplicate()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        await svc.EnsureDefaultWatchlistAsync(1);
        await svc.EnsureDefaultWatchlistAsync(1);

        var count = await db.Watchlists.CountAsync(w => w.IsDefault && w.UserId == 1);
        Assert.Equal(1, count);
    }
}
