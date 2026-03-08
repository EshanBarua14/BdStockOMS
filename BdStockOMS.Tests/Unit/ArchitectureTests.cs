using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class ArchitectureTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── RESULT<T> PATTERN ─────────────────────────────────────

    [Fact]
    public void Result_Success_HasCorrectState()
    {
        var result = Result<string>.Success("hello");
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Result_Failure_HasCorrectState()
    {
        var result = Result<string>.Failure("Something went wrong", "SOME_ERROR");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("Something went wrong", result.Error);
        Assert.Equal("SOME_ERROR", result.ErrorCode);
    }

    [Fact]
    public void Result_Failure_DefaultErrorCode_IsERROR()
    {
        var result = Result<int>.Failure("bad");
        Assert.Equal("ERROR", result.ErrorCode);
    }

    [Fact]
    public void Result_NonGeneric_Success_HasCorrectState()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Result_NonGeneric_Failure_HasCorrectState()
    {
        var result = Result.Failure("failed", "FAIL_CODE");
        Assert.False(result.IsSuccess);
        Assert.Equal("FAIL_CODE", result.ErrorCode);
    }

    // ── PAGED RESULT ──────────────────────────────────────────

    [Fact]
    public void PagedResult_TotalPages_CalculatedCorrectly()
    {
        var paged = PagedResult<string>.Create(
            new[] { "a", "b", "c" }, 25, 1, 10);
        Assert.Equal(3, paged.TotalPages);
        Assert.Equal(25, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(10, paged.PageSize);
    }

    [Fact]
    public void PagedResult_HasNextPage_TrueWhenNotLastPage()
    {
        var paged = PagedResult<int>.Create(Enumerable.Range(1, 10), 30, 1, 10);
        Assert.True(paged.HasNextPage);
        Assert.False(paged.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_TrueWhenNotFirstPage()
    {
        var paged = PagedResult<int>.Create(Enumerable.Range(1, 10), 30, 2, 10);
        Assert.True(paged.HasPreviousPage);
        Assert.True(paged.HasNextPage);
    }

    [Fact]
    public void PagedResult_LastPage_HasNoNextPage()
    {
        var paged = PagedResult<int>.Create(Enumerable.Range(1, 5), 25, 3, 10);
        Assert.False(paged.HasNextPage);
        Assert.True(paged.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_ExactDivision_TotalPagesCorrect()
    {
        var paged = PagedResult<int>.Create(Enumerable.Range(1, 20), 20, 1, 20);
        Assert.Equal(1, paged.TotalPages);
    }

    // ── USER REPOSITORY ───────────────────────────────────────

    [Fact]
    public async Task UserRepository_GetByEmailAsync_ReturnsUser()
    {
        var db   = CreateDb();
        var repo = new UserRepository(db);

        db.Roles.Add(new Role { Id = 1, Name = "BrokerageHouse" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "John", Email = "john@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        await db.SaveChangesAsync();

        var user = await repo.GetByEmailAsync("john@test.com");
        Assert.NotNull(user);
        Assert.Equal("John", user.FullName);
    }

    [Fact]
    public async Task UserRepository_GetByEmailAsync_NotFound_ReturnsNull()
    {
        var db   = CreateDb();
        var repo = new UserRepository(db);

        var user = await repo.GetByEmailAsync("nobody@test.com");
        Assert.Null(user);
    }

    [Fact]
    public async Task UserRepository_GetPagedAsync_ReturnsCorrectPage()
    {
        var db = CreateDb();
        db.Roles.Add(new Role { Id = 1, Name = "BrokerageHouse" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        for (int i = 1; i <= 15; i++)
            db.Users.Add(new User
            {
                FullName = $"User {i}", Email = $"user{i}@test.com",
                PasswordHash = "hash", Phone = "01700000000",
                RoleId = 1, BrokerageHouseId = 1
            });
        await db.SaveChangesAsync();

        var repo = new UserRepository(db);
        var (items, total) = await repo.GetPagedAsync(1, 10);

        Assert.Equal(15, total);
        Assert.Equal(10, items.Count());
    }

    // ── STOCK REPOSITORY ──────────────────────────────────────

    [Fact]
    public async Task StockRepository_GetByTradingCode_ReturnsStock()
    {
        var db = CreateDb();
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A,
            BoardLotSize = 1, LastTradePrice = 380m,
            CircuitBreakerHigh = 418m, CircuitBreakerLow = 342m,
            IsActive = true, LastUpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var repo  = new StockRepository(db);
        var stock = await repo.GetByTradingCodeAsync("GP", "DSE");

        Assert.NotNull(stock);
        Assert.Equal("Grameenphone", stock.CompanyName);
    }

    [Fact]
    public async Task StockRepository_GetPagedAsync_FiltersExchange()
    {
        var db = CreateDb();
        db.Stocks.AddRange(
            new Stock { TradingCode = "GP",       CompanyName = "GP DSE",   Exchange = "DSE",
                        Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 380m,
                        CircuitBreakerHigh = 418m, CircuitBreakerLow = 342m,
                        IsActive = true, LastUpdatedAt = DateTime.UtcNow },
            new Stock { TradingCode = "GP",       CompanyName = "GP CSE",   Exchange = "CSE",
                        Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 379m,
                        CircuitBreakerHigh = 417m, CircuitBreakerLow = 341m,
                        IsActive = true, LastUpdatedAt = DateTime.UtcNow },
            new Stock { TradingCode = "BRACBANK", CompanyName = "BRAC DSE", Exchange = "DSE",
                        Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 52m,
                        CircuitBreakerHigh = 57m, CircuitBreakerLow = 47m,
                        IsActive = true, LastUpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = new StockRepository(db);
        var (items, total) = await repo.GetPagedAsync(1, 10, exchange: "DSE");

        Assert.Equal(2, total);
        Assert.All(items, s => Assert.Equal("DSE", s.Exchange));
    }

    // ── REFRESH TOKEN REPOSITORY ──────────────────────────────

    [Fact]
    public async Task RefreshTokenRepository_GetActiveToken_ReturnsToken()
    {
        var db = CreateDb();
        db.Roles.Add(new Role { Id = 1, Name = "BrokerageHouse" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "John", Email = "john@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1, Token = "active-token-123",
            CreatedByIp = "127.0.0.1",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var repo  = new RefreshTokenRepository(db);
        var token = await repo.GetActiveTokenAsync("active-token-123");

        Assert.NotNull(token);
        Assert.Equal("active-token-123", token.Token);
    }

    [Fact]
    public async Task RefreshTokenRepository_GetActiveToken_ExpiredToken_ReturnsNull()
    {
        var db = CreateDb();
        db.Roles.Add(new Role { Id = 1, Name = "BrokerageHouse" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "John", Email = "john@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 1, Token = "expired-token",
            CreatedByIp = "127.0.0.1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // already expired
        });
        await db.SaveChangesAsync();

        var repo  = new RefreshTokenRepository(db);
        var token = await repo.GetActiveTokenAsync("expired-token");

        Assert.Null(token);
    }
}
