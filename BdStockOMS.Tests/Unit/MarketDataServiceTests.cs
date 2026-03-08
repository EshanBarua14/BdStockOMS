using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.DTOs.News;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class MarketDataServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private Stock SeedStock(AppDbContext db)
    {
        var stock = new Stock
        {
            Id = 1,
            TradingCode = "GP",
            CompanyName = "Grameenphone Ltd",
            Exchange = "DSE",
            Category = StockCategory.A,
            BoardLotSize = 1,
            LastTradePrice = 380m,
            CircuitBreakerHigh = 418m,
            CircuitBreakerLow = 342m,
            IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        };
        db.Stocks.Add(stock);
        db.SaveChanges();
        return stock;
    }

    // ── MARKET DATA TESTS ──────────────────────────

    [Fact]
    public async Task CreateMarketData_ValidInput_ReturnsSuccess()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Open = 375m, High = 385m,
            Low = 370m, Close = 380m, Volume = 100000, ValueInMillionTaka = 38m,
            Trades = 500, Date = DateTime.UtcNow.Date
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("GP", result.Value!.TradingCode);
        Assert.Equal(380m, result.Value.Close);
    }

    [Fact]
    public async Task CreateMarketData_InvalidStock_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 999, Exchange = "DSE", Date = DateTime.UtcNow.Date
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Stock not found", result.Error);
    }

    [Fact]
    public async Task CreateMarketData_DuplicateDate_ReturnsFail()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new MarketDataService(db);
        var date = DateTime.UtcNow.Date;

        await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Date = date,
            Open = 375m, High = 385m, Low = 370m, Close = 380m
        });

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Date = date,
            Open = 375m, High = 385m, Low = 370m, Close = 380m
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task GetById_ExistingRecord_ReturnsDto()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new MarketDataService(db);

        var created = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Date = DateTime.UtcNow.Date,
            Open = 375m, High = 385m, Low = 370m, Close = 380m
        });

        var result = await svc.GetByIdAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new MarketDataService(db);

        var result = await svc.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task BulkCreate_SkipsDuplicates_ReturnsCorrectCounts()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new MarketDataService(db);
        var date = DateTime.UtcNow.Date;

        await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Date = date,
            Open = 375m, High = 385m, Low = 370m, Close = 380m
        });

        var bulk = new BulkMarketDataDto
        {
            Items = new List<CreateMarketDataDto>
            {
                new() { StockId = 1, Exchange = "DSE", Date = date, Open = 375m, High = 385m, Low = 370m, Close = 380m },
                new() { StockId = 1, Exchange = "DSE", Date = date.AddDays(-1), Open = 370m, High = 380m, Low = 365m, Close = 375m }
            }
        };

        var result = await svc.BulkCreateAsync(bulk);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Created);
        Assert.Equal(1, result.Value.Skipped);
    }

    [Fact]
    public async Task DeleteMarketData_ExistingRecord_ReturnsSuccess()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new MarketDataService(db);

        var created = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Date = DateTime.UtcNow.Date,
            Open = 375m, High = 385m, Low = 370m, Close = 380m
        });

        var result = await svc.DeleteAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    // ── CORPORATE ACTION TESTS ─────────────────────

    [Fact]
    public async Task CreateCorporateAction_ValidDividend_ReturnsSuccess()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new CorporateActionService(db);

        var result = await svc.CreateAsync(new CreateCorporateActionDto
        {
            StockId = 1, Type = "Dividend", Value = 5.0m,
            RecordDate = DateTime.UtcNow.AddDays(30)
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Dividend", result.Value!.Type);
        Assert.Equal(5.0m, result.Value.Value);
    }

    [Fact]
    public async Task CreateCorporateAction_InvalidType_ReturnsFail()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new CorporateActionService(db);

        var result = await svc.CreateAsync(new CreateCorporateActionDto
        {
            StockId = 1, Type = "InvalidType", Value = 5.0m,
            RecordDate = DateTime.UtcNow.AddDays(30)
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid", result.Error);
    }

    [Fact]
    public async Task MarkProcessed_UnprocessedAction_ReturnsSuccess()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new CorporateActionService(db);

        var created = await svc.CreateAsync(new CreateCorporateActionDto
        {
            StockId = 1, Type = "BonusShare", Value = 10m,
            RecordDate = DateTime.UtcNow.AddDays(10)
        });

        var result = await svc.MarkProcessedAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task MarkProcessed_AlreadyProcessed_ReturnsFail()
    {
        var db = CreateDb();
        SeedStock(db);
        var svc = new CorporateActionService(db);

        var created = await svc.CreateAsync(new CreateCorporateActionDto
        {
            StockId = 1, Type = "Dividend", Value = 2m,
            RecordDate = DateTime.UtcNow.AddDays(10)
        });

        await svc.MarkProcessedAsync(created.Value!.Id);
        var result = await svc.MarkProcessedAsync(created.Value.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("already processed", result.Error);
    }

    // ── NEWS SERVICE TESTS ─────────────────────────

    [Fact]
    public async Task CreateNews_ValidInput_ReturnsSuccess()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title = "DSE index rises 2%",
            Content = "The Dhaka Stock Exchange index rose 2% today.",
            Category = "MarketUpdate",
            IsPublished = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("DSE index rises 2%", result.Value!.Title);
        Assert.Equal("MarketUpdate", result.Value.Category);
    }

    [Fact]
    public async Task CreateNews_InvalidCategory_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title = "Test", Content = "Test content", Category = "BadCategory"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid category", result.Error);
    }

    [Fact]
    public async Task PublishNews_UnpublishedItem_ReturnsSuccess()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var created = await svc.CreateAsync(new CreateNewsDto
        {
            Title = "Regulatory update", Content = "New BSEC rules announced.",
            Category = "RegulatoryUpdate", IsPublished = false
        });

        var result = await svc.PublishAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }
}
