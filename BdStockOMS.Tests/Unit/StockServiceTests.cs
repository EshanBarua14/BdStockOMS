// Tests/Unit/StockServiceTests.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Stock;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class StockServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static CreateStockDto SampleStock(
        string code = "GP",
        string exchange = "DSE") => new()
    {
        TradingCode = code,
        CompanyName = "Grameenphone Ltd",
        Exchange = exchange,
        LastTradePrice = 380.50m,
        HighPrice = 385.00m,
        LowPrice = 375.00m,
        ClosePrice = 378.00m,
        Change = 2.50m,
        ChangePercent = 0.66m,
        Volume = 150000,
        ValueInMillionTaka = 57.08m
    };

    [Fact]
    public async Task CreateStockAsync_ValidStock_ReturnsStock()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var (stock, error) = await service.CreateStockAsync(SampleStock());

        Assert.Null(error);
        Assert.NotNull(stock);
        Assert.Equal("GP", stock.TradingCode);
        Assert.Equal("DSE", stock.Exchange);
    }

    [Fact]
    public async Task CreateStockAsync_DuplicateTradingCode_ReturnsError()
    {
        var db = CreateDb();
        var service = new StockService(db);

        await service.CreateStockAsync(SampleStock());
        var (stock, error) = await service.CreateStockAsync(SampleStock());

        Assert.Null(stock);
        Assert.NotNull(error);
        Assert.Contains("already exists", error);
    }

    [Fact]
    public async Task CreateStockAsync_InvalidExchange_ReturnsError()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var (stock, error) = await service.CreateStockAsync(SampleStock("GP", "NYSE"));

        Assert.Null(stock);
        Assert.NotNull(error);
        Assert.Contains("DSE", error);
    }

    [Fact]
    public async Task GetAllStocksAsync_ReturnsOnlyActiveStocks()
    {
        var db = CreateDb();
        var service = new StockService(db);

        await service.CreateStockAsync(SampleStock("GP", "DSE"));
        await service.CreateStockAsync(SampleStock("BRACBANK", "DSE"));

        var stocks = await service.GetAllStocksAsync();

        Assert.Equal(2, stocks.Count);
        Assert.All(stocks, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task SearchStocksAsync_MatchesTradingCode()
    {
        var db = CreateDb();
        var service = new StockService(db);

        await service.CreateStockAsync(SampleStock("GP", "DSE"));
        await service.CreateStockAsync(SampleStock("BRACBANK", "DSE"));

        var results = await service.SearchStocksAsync("GP");

        Assert.Single(results);
        Assert.Equal("GP", results[0].TradingCode);
    }

    [Fact]
    public async Task UpdateStockAsync_ValidId_UpdatesPrices()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var (created, _) = await service.CreateStockAsync(SampleStock());

        var (updated, error) = await service.UpdateStockAsync(created!.Id, new UpdateStockDto
        {
            LastTradePrice = 400.00m,
            HighPrice = 405.00m,
            LowPrice = 395.00m,
            ClosePrice = 398.00m,
            Change = 20.00m,
            ChangePercent = 5.26m,
            Volume = 200000,
            ValueInMillionTaka = 80.00m
        });

        Assert.Null(error);
        Assert.Equal(400.00m, updated!.LastTradePrice);
    }

    [Fact]
    public async Task UpdateStockAsync_InvalidId_ReturnsError()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var (stock, error) = await service.UpdateStockAsync(999, new UpdateStockDto());

        Assert.Null(stock);
        Assert.NotNull(error);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task DeactivateStockAsync_ValidId_DeactivatesStock()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var (created, _) = await service.CreateStockAsync(SampleStock());
        var result = await service.DeactivateStockAsync(created!.Id);
        var found = await service.GetStockByIdAsync(created.Id);

        Assert.True(result);
        Assert.Null(found); // deactivated — not returned
    }

    [Fact]
    public async Task DeactivateStockAsync_InvalidId_ReturnsFalse()
    {
        var db = CreateDb();
        var service = new StockService(db);

        var result = await service.DeactivateStockAsync(999);

        Assert.False(result);
    }
}
