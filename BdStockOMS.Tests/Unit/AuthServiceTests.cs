using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class AuthServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private IConfiguration CreateFakeConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey",     "TestSecretKey-That-Is-Long-Enough-32chars!" },
            { "JwtSettings:Issuer",        "BdStockOMS" },
            { "JwtSettings:Audience",      "BdStockOMS" },
            { "JwtSettings:ExpiryInDays",  "7" }
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private async Task SeedRolesAsync(AppDbContext context)
    {
        await context.Roles.AddRangeAsync(
            new Role { Id = 1, Name = "BrokerageHouse" },
            new Role { Id = 2, Name = "Admin" },
            new Role { Id = 3, Name = "CCD" },
            new Role { Id = 4, Name = "ITSupport" },
            new Role { Id = 5, Name = "Trader" },
            new Role { Id = 6, Name = "Investor" }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task RegisterBrokerage_CreatesUserAndBrokerage()
    {
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var service = new AuthService(context, CreateFakeConfiguration());

        var result = await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("Test Owner", result.FullName);
        Assert.Equal("BrokerageHouse", result.Role);
        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(1, await context.BrokerageHouses.CountAsync());
    }

    [Fact]
    public async Task RegisterBrokerage_DuplicateEmail_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var service = new AuthService(context, CreateFakeConfiguration());

        await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        // Second registration with same email — should return null
        var result = await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Another Securities",
            LicenseNumber = "LIC-002",
            FirmEmail = "another@test.com",
            FullName = "Another Owner",
            Email = "owner@test.com", // same email!
            Password = "Password@123"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var service = new AuthService(context, CreateFakeConfiguration());

        await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "owner@test.com",
            Password = "Password@123"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("owner@test.com", result.Email);
        Assert.Equal("BrokerageHouse", result.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var service = new AuthService(context, CreateFakeConfiguration());

        await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "owner@test.com",
            Password = "WrongPassword!"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var service = new AuthService(context, CreateFakeConfiguration());

        var result = await service.LoginAsync(new LoginDto
        {
            Email = "nobody@test.com",
            Password = "Password@123"
        });

        Assert.Null(result);
    }
}
