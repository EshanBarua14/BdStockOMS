using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.User;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class UserServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Roles.AddRange(
            new Role { Id = 1, Name = "BrokerageHouse" },
            new Role { Id = 2, Name = "Admin" },
            new Role { Id = 5, Name = "Trader" },
            new Role { Id = 6, Name = "Investor" }
        );
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "LIC001",
            Email = "test@brokerage.com", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateUserAsync_ValidTrader_ReturnsUser()
    {
        var db = CreateDb();
        var service = new UserService(db);

        var (user, error) = await service.CreateUserAsync(new CreateUserDto
        {
            FullName = "John Trader",
            Email = "john@test.com",
            Password = "pass123",
            Role = "Trader"
        }, creatorBrokerageHouseId: 1);

        Assert.Null(error);
        Assert.NotNull(user);
        Assert.Equal("Trader", user.Role);
        Assert.Equal(1, user.BrokerageHouseId);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_ReturnsError()
    {
        var db = CreateDb();
        var service = new UserService(db);

        var (user, error) = await service.CreateUserAsync(new CreateUserDto
        {
            FullName = "Hacker", Email = "h@test.com",
            Password = "pass123", Role = "BrokerageHouse"
        }, 1);

        Assert.Null(user);
        Assert.NotNull(error);
        Assert.Contains("not allowed", error);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ReturnsError()
    {
        var db = CreateDb();
        var service = new UserService(db);
        var dto = new CreateUserDto
        {
            FullName = "Alice", Email = "alice@test.com",
            Password = "pass123", Role = "Trader"
        };

        await service.CreateUserAsync(dto, 1);
        var (user, error) = await service.CreateUserAsync(dto, 1);

        Assert.Null(user);
        Assert.NotNull(error);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task GetUsersByBrokerageAsync_ReturnsOnlyBrokerageUsers()
    {
        var db = CreateDb();
        var service = new UserService(db);

        await service.CreateUserAsync(new CreateUserDto
            { FullName = "U1", Email = "u1@t.com", Password = "p", Role = "Trader" }, 1);
        await service.CreateUserAsync(new CreateUserDto
            { FullName = "U2", Email = "u2@t.com", Password = "p", Role = "Investor" }, 1);

        var users = await service.GetUsersByBrokerageAsync(1);

        Assert.Equal(2, users.Count);
        Assert.All(users, u => Assert.Equal(1, u.BrokerageHouseId));
    }

    [Fact]
    public async Task DeactivateUserAsync_ValidUser_DeactivatesSuccessfully()
    {
        var db = CreateDb();
        var service = new UserService(db);
        var (created, _) = await service.CreateUserAsync(new CreateUserDto
            { FullName = "Bob", Email = "bob@t.com", Password = "p", Role = "Trader" }, 1);

        var result = await service.DeactivateUserAsync(created!.Id, 1);
        var found = await service.GetUserByIdAsync(created.Id);

        Assert.True(result);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeactivateUserAsync_WrongBrokerage_ReturnsFalse()
    {
        var db = CreateDb();
        var service = new UserService(db);
        var (created, _) = await service.CreateUserAsync(new CreateUserDto
            { FullName = "Bob", Email = "bob@t.com", Password = "p", Role = "Trader" }, 1);

        var result = await service.DeactivateUserAsync(created!.Id, 99);

        Assert.False(result);
    }
}
