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
    // Creates fresh in-memory DB for each test
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // Creates a fake IConfiguration with JWT settings
    // We don't want to read appsettings.json in tests
    // We provide known values we control
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

    // Seeds the 6 roles into in-memory DB
    // AuthService needs roles to exist
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
        // ARRANGE
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var config = CreateFakeConfiguration();
        var service = new AuthService(context, config);

        var dto = new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        };

        // ACT
        var result = await service.RegisterBrokerageAsync(dto);

        // ASSERT
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("Test Owner", result.FullName);
        Assert.Equal("BrokerageHouse", result.Role);

        // Verify records actually saved to DB
        var userCount = await context.Users.CountAsync();
        var brokerageCount = await context.BrokerageHouses.CountAsync();
        Assert.Equal(1, userCount);
        Assert.Equal(1, brokerageCount);
    }

    [Fact]
    public async Task RegisterBrokerage_DuplicateEmail_ThrowsException()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var config = CreateFakeConfiguration();
        var service = new AuthService(context, config);

        var dto = new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        };

        // Register once successfully
        await service.RegisterBrokerageAsync(dto);

        // ACT + ASSERT
        // Second registration with same email
        // must throw InvalidOperationException
        var dto2 = new RegisterBrokerageDto
        {
            FirmName = "Another Securities",
            LicenseNumber = "LIC-002",
            FirmEmail = "another@test.com",
            FullName = "Another Owner",
            Email = "owner@test.com", // same email!
            Password = "Password@123"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterBrokerageAsync(dto2)
        );
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var config = CreateFakeConfiguration();
        var service = new AuthService(context, config);

        // Register first so user exists
        await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        // ACT
        var result = await service.LoginAsync(new LoginDto
        {
            Email = "owner@test.com",
            Password = "Password@123"
        });

        // ASSERT
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("owner@test.com", result.Email);
        Assert.Equal("BrokerageHouse", result.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsException()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var config = CreateFakeConfiguration();
        var service = new AuthService(context, config);

        await service.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",
            FullName = "Test Owner",
            Email = "owner@test.com",
            Password = "Password@123"
        });

        // ACT + ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoginAsync(new LoginDto
            {
                Email = "owner@test.com",
                Password = "WrongPassword!" // wrong!
            })
        );
    }

    [Fact]
    public async Task Login_NonExistentEmail_ThrowsException()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();
        await SeedRolesAsync(context);
        var config = CreateFakeConfiguration();
        var service = new AuthService(context, config);

        // ACT + ASSERT
        // Login with email that was never registered
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoginAsync(new LoginDto
            {
                Email = "nobody@test.com",
                Password = "Password@123"
            })
        );
    }
}