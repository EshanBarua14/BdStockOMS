using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Models.Admin;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class Day66AdminFeeServiceTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private AdminFeeService Svc(AppDbContext db) => new(db);
    private FeeStructureDto MakeDto(string name = "DSE Standard", bool active = true) =>
        new(name, 0.0040m, 0.0015m, 0.0015m, 15m, 0.05m, 10m, "ALL", active);

    private async Task<FeeStructureDto> SeedAsync(AppDbContext db, string name = "DSE Standard")
        => await new AdminFeeService(db).CreateAsync(MakeDto(name));

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
        => Assert.Empty(await Svc(CreateDb()).GetAllAsync());

    [Fact]
    public async Task GetAllAsync_TwoRecords_ReturnsBoth()
    {
        var db = CreateDb();
        await SeedAsync(db, "DSE Standard");
        await SeedAsync(db, "CSE Standard");
        Assert.Equal(2, (await Svc(db).GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetAllAsync_OrderedByName()
    {
        var db = CreateDb();
        await SeedAsync(db, "Z Fee");
        await SeedAsync(db, "A Fee");
        var result = await Svc(db).GetAllAsync();
        Assert.Equal("A Fee", result[0].Name);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreated()
    {
        var result = await Svc(CreateDb()).CreateAsync(MakeDto("DSE Standard"));
        Assert.Equal("DSE Standard", result.Name);
    }

    [Fact]
    public async Task CreateAsync_AssignsId()
    {
        var result = await Svc(CreateDb()).CreateAsync(MakeDto());
        Assert.NotNull(result.Id);
        Assert.True(Guid.TryParse(result.Id, out _));
    }

    [Fact]
    public async Task CreateAsync_PersistsToDb()
    {
        var db = CreateDb();
        await Svc(db).CreateAsync(MakeDto());
        Assert.Equal(1, db.FeeStructures.Count());
    }

    [Fact]
    public async Task CreateAsync_SetsIsActive()
    {
        var result = await Svc(CreateDb()).CreateAsync(MakeDto(active: true));
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsTrue()
    {
        var db   = CreateDb();
        var seed = await SeedAsync(db);
        Assert.True(await Svc(db).UpdateAsync(seed.Id!, MakeDto("Updated")));
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsFalse()
        => Assert.False(await Svc(CreateDb()).UpdateAsync("bad-id", MakeDto()));

    [Fact]
    public async Task UpdateAsync_ChangesName_InDb()
    {
        var db   = CreateDb();
        var seed = await SeedAsync(db, "Old Name");
        await Svc(db).UpdateAsync(seed.Id!, MakeDto("New Name"));
        Assert.Equal("New Name", db.FeeStructures.First().Name);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var db   = CreateDb();
        var seed = await SeedAsync(db);
        Assert.True(await Svc(db).DeleteAsync(seed.Id!));
        Assert.Equal(0, db.FeeStructures.Count());
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
        => Assert.False(await Svc(CreateDb()).DeleteAsync("bad-id"));

    [Fact]
    public async Task DeleteAsync_OnlyDeletesTargetFee()
    {
        var db    = CreateDb();
        var seed1 = await SeedAsync(db, "Fee 1");
        var seed2 = await SeedAsync(db, "Fee 2");
        await Svc(db).DeleteAsync(seed1.Id!);
        Assert.Equal(1, db.FeeStructures.Count());
        Assert.Equal("Fee 2", db.FeeStructures.First().Name);
    }

    [Fact]
    public void FeeStructure_DefaultIsActive_IsTrue()
        => Assert.True(new FeeStructure().IsActive);

    [Fact]
    public void FeeStructure_DefaultVatPercent_IsFifteen()
        => Assert.Equal(15m, new FeeStructure().VatPercent);

    [Fact]
    public void FeeStructure_DefaultApplyToCategory_IsAll()
        => Assert.Equal("ALL", new FeeStructure().ApplyToCategory);

    [Fact]
    public void FeeStructure_Id_DefaultsToGuid()
        => Assert.True(Guid.TryParse(new FeeStructure().Id, out _));
}
