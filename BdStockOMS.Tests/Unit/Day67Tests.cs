using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 67 — BrokerManagementService Tests
//  Coverage: Brokerage CRUD, Branch CRUD, BO Account read/update
// ============================================================

public class Day67BrokerManagementTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private BrokerManagementService Svc(AppDbContext db) => new(db);

    private async Task<BrokerageHouse> SeedBrokerage(AppDbContext db,
        string name = "Pioneer Securities", string license = "DSE-TM-0001")
    {
        var b = new BrokerageHouse
        {
            Name = name, LicenseNumber = license,
            Email = "info@pioneer.com", Phone = "01700000000",
            Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
        };
        db.BrokerageHouses.Add(b);
        await db.SaveChangesAsync();
        return b;
    }

    private async Task<BranchOffice> SeedBranch(AppDbContext db, int brokerageId,
        string name = "Main Branch", string code = "BR-001")
    {
        var b = new BranchOffice
        {
            BrokerageHouseId = brokerageId, Name = name, BranchCode = code,
            Address = "Motijheel, Dhaka", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.BranchOffices.Add(b);
        await db.SaveChangesAsync();
        return b;
    }

    private async Task<User> SeedBOUser(AppDbContext db, int brokerageId,
        string boNumber = "BO1001000001")
    {
        var u = new User
        {
            FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", BrokerageHouseId = brokerageId,
            BONumber = boNumber, AccountType = AccountType.Cash,
            CashBalance = 50000m, MarginLimit = 0m, MarginUsed = 0m,
            IsActive = true, IsBOAccountActive = true, CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    // ── GetAllBrokeragesAsync ────────────────────────────────

    [Fact]
    public async Task GetAllBrokerages_EmptyDb_ReturnsEmpty()
    {
        var result = await Svc(CreateDb()).GetAllBrokeragesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllBrokerages_WithRecords_ReturnsAll()
    {
        var db = CreateDb();
        await SeedBrokerage(db, "Pioneer", "DSE-TM-0001");
        await SeedBrokerage(db, "Alpha",   "DSE-TM-0002");
        var result = await Svc(db).GetAllBrokeragesAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllBrokerages_OrderedByName()
    {
        var db = CreateDb();
        await SeedBrokerage(db, "Zeta Brokerage", "DSE-TM-0003");
        await SeedBrokerage(db, "Alpha Brokerage","DSE-TM-0004");
        var result = (await Svc(db).GetAllBrokeragesAsync()).ToList();
        Assert.Equal("Alpha Brokerage", result[0].Name);
    }

    [Fact]
    public async Task GetAllBrokerages_IncludesBranchCount()
    {
        var db = CreateDb();
        var b  = await SeedBrokerage(db);
        await SeedBranch(db, b.Id, "Main",  "BR-001");
        await SeedBranch(db, b.Id, "North", "BR-002");
        var result = (await Svc(db).GetAllBrokeragesAsync()).First();
        Assert.Equal(2, result.BranchCount);
    }

    // ── GetBrokerageByIdAsync ────────────────────────────────

    [Fact]
    public async Task GetBrokerageById_ExistingId_Returns()
    {
        var db = CreateDb();
        var b  = await SeedBrokerage(db);
        var r  = await Svc(db).GetBrokerageByIdAsync(b.Id);
        Assert.NotNull(r);
        Assert.Equal(b.Name, r!.Name);
    }

    [Fact]
    public async Task GetBrokerageById_NonExistent_ReturnsNull()
    {
        var r = await Svc(CreateDb()).GetBrokerageByIdAsync(999);
        Assert.Null(r);
    }

    // ── CreateBrokerageAsync ─────────────────────────────────

    [Fact]
    public async Task CreateBrokerage_ValidDto_Persists()
    {
        var db  = CreateDb();
        var dto = new CreateBrokerageHouseDto("New Brokerage", "DSE-TM-0099",
            "new@brok.com", "01800000000", "Chittagong");
        var r   = await Svc(db).CreateBrokerageAsync(dto);
        Assert.NotNull(r);
        Assert.Equal("New Brokerage", r.Name);
        Assert.Equal(1, db.BrokerageHouses.Count());
    }

    [Fact]
    public async Task CreateBrokerage_SetsIsActiveTrue()
    {
        var db  = CreateDb();
        var dto = new CreateBrokerageHouseDto("X", "DSE-X", "x@x.com", "", "");
        var r   = await Svc(db).CreateBrokerageAsync(dto);
        Assert.True(r.IsActive);
    }

    // ── UpdateBrokerageAsync ─────────────────────────────────

    [Fact]
    public async Task UpdateBrokerage_ValidId_UpdatesName()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var dto = new UpdateBrokerageHouseDto("Updated Name", null, null, null, null);
        var r   = await Svc(db).UpdateBrokerageAsync(b.Id, dto);
        Assert.Equal("Updated Name", r!.Name);
    }

    [Fact]
    public async Task UpdateBrokerage_NonExistent_ReturnsNull()
    {
        var dto = new UpdateBrokerageHouseDto("X", null, null, null, null);
        var r   = await Svc(CreateDb()).UpdateBrokerageAsync(999, dto);
        Assert.Null(r);
    }

    // ── ToggleBrokerageAsync ─────────────────────────────────

    [Fact]
    public async Task ToggleBrokerage_Deactivate_SetsInactive()
    {
        var db = CreateDb();
        var b  = await SeedBrokerage(db);
        await Svc(db).ToggleBrokerageAsync(b.Id, false);
        Assert.False(db.BrokerageHouses.First().IsActive);
    }

    [Fact]
    public async Task ToggleBrokerage_NonExistent_ReturnsFalse()
    {
        var ok = await Svc(CreateDb()).ToggleBrokerageAsync(999, true);
        Assert.False(ok);
    }

    // ── GetAllBranchesAsync ──────────────────────────────────

    [Fact]
    public async Task GetAllBranches_EmptyDb_ReturnsEmpty()
    {
        var result = await Svc(CreateDb()).GetAllBranchesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllBranches_FilterByBrokerage_ReturnsFiltered()
    {
        var db = CreateDb();
        var b1 = await SeedBrokerage(db, "Brokerage 1", "DSE-1");
        var b2 = await SeedBrokerage(db, "Brokerage 2", "DSE-2");
        await SeedBranch(db, b1.Id, "Branch A", "BR-A");
        await SeedBranch(db, b2.Id, "Branch B", "BR-B");
        var result = await Svc(db).GetAllBranchesAsync(b1.Id);
        Assert.Single(result);
        Assert.Equal("Branch A", result.First().Name);
    }

    // ── CreateBranchAsync ────────────────────────────────────

    [Fact]
    public async Task CreateBranch_ValidDto_Persists()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var dto = new CreateBranchOfficeDto(b.Id, "North Branch", "BR-N",
            "Gulshan, Dhaka", "01700000001", "north@brok.com", "Mr. Ahmed");
        var r   = await Svc(db).CreateBranchAsync(dto);
        Assert.Equal("North Branch", r.Name);
        Assert.Equal("BR-N", r.BranchCode);
        Assert.Equal(1, db.BranchOffices.Count());
    }

    [Fact]
    public async Task CreateBranch_SetsIsActiveTrue()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var dto = new CreateBranchOfficeDto(b.Id, "X", "X", "X", null, null, null);
        var r   = await Svc(db).CreateBranchAsync(dto);
        Assert.True(r.IsActive);
    }

    // ── UpdateBranchAsync ────────────────────────────────────

    [Fact]
    public async Task UpdateBranch_ValidId_UpdatesName()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var br  = await SeedBranch(db, b.Id);
        var dto = new UpdateBranchOfficeDto("Updated Branch", null, null, null, null, null);
        var r   = await Svc(db).UpdateBranchAsync(br.Id, dto);
        Assert.Equal("Updated Branch", r!.Name);
    }

    [Fact]
    public async Task UpdateBranch_NonExistent_ReturnsNull()
    {
        var dto = new UpdateBranchOfficeDto("X", null, null, null, null, null);
        var r   = await Svc(CreateDb()).UpdateBranchAsync(999, dto);
        Assert.Null(r);
    }

    // ── ToggleBranchAsync ────────────────────────────────────

    [Fact]
    public async Task ToggleBranch_Deactivate_SetsInactive()
    {
        var db = CreateDb();
        var b  = await SeedBrokerage(db);
        var br = await SeedBranch(db, b.Id);
        await Svc(db).ToggleBranchAsync(br.Id, false);
        Assert.False(db.BranchOffices.First().IsActive);
    }

    [Fact]
    public async Task ToggleBranch_NonExistent_ReturnsFalse()
    {
        var ok = await Svc(CreateDb()).ToggleBranchAsync(999, false);
        Assert.False(ok);
    }

    // ── GetAllBOAccountsAsync ────────────────────────────────

    [Fact]
    public async Task GetAllBOAccounts_EmptyDb_ReturnsEmpty()
    {
        var result = await Svc(CreateDb()).GetAllBOAccountsAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllBOAccounts_ReturnsUsersWithBONumber()
    {
        var db = CreateDb();
        var b  = await SeedBrokerage(db);
        await SeedBOUser(db, b.Id, "BO1001000001");
        await SeedBOUser(db, b.Id, "BO1001000002");
        var result = await Svc(db).GetAllBOAccountsAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllBOAccounts_FilterByBrokerage()
    {
        var db = CreateDb();
        var b1 = await SeedBrokerage(db, "B1", "DSE-1");
        var b2 = await SeedBrokerage(db, "B2", "DSE-2");
        await SeedBOUser(db, b1.Id, "BO1001000001");
        await SeedBOUser(db, b2.Id, "BO1002000001");
        var result = await Svc(db).GetAllBOAccountsAsync(b1.Id);
        Assert.Single(result);
    }

    // ── UpdateBOAccountAsync ─────────────────────────────────

    [Fact]
    public async Task UpdateBOAccount_ValidUserId_UpdatesName()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var u   = await SeedBOUser(db, b.Id);
        var dto = new UpdateBOAccountDto("New Name", null, null, null, null);
        var r   = await Svc(db).UpdateBOAccountAsync(u.Id, dto);
        Assert.Equal("New Name", r!.FullName);
    }

    [Fact]
    public async Task UpdateBOAccount_NonExistent_ReturnsNull()
    {
        var dto = new UpdateBOAccountDto("X", null, null, null, null);
        var r   = await Svc(CreateDb()).UpdateBOAccountAsync(999, dto);
        Assert.Null(r);
    }

    [Fact]
    public async Task UpdateBOAccount_DeactivatesBOAccount()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var u   = await SeedBOUser(db, b.Id);
        var dto = new UpdateBOAccountDto(null, null, null, false, null);
        var r   = await Svc(db).UpdateBOAccountAsync(u.Id, dto);
        Assert.False(r!.IsBOAccountActive);
    }

    [Fact]
    public async Task UpdateBOAccount_SetsMarginLimit()
    {
        var db  = CreateDb();
        var b   = await SeedBrokerage(db);
        var u   = await SeedBOUser(db, b.Id);
        var dto = new UpdateBOAccountDto(null, null, null, null, 100000m);
        var r   = await Svc(db).UpdateBOAccountAsync(u.Id, dto);
        Assert.Equal(100000m, r!.MarginLimit);
    }
}
