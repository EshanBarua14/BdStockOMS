using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.DTOs.News;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 61 — Market Analytics Widget Backend Tests
//  ScoreBoard / MostActive / Time & Sales / NewsFeed
// ============================================================

public class Day61MarketDataTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private void SeedStocks(AppDbContext db)
    {
        var stocks = new[]
        {
            new Stock { Id=1, TradingCode="GP",        CompanyName="Grameenphone",    Exchange="DSE", Category=StockCategory.A, LastTradePrice=380m,  ChangePercent=0.61m,  Volume=18234000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=2, TradingCode="BATBC",     CompanyName="BAT Bangladesh",  Exchange="DSE", Category=StockCategory.A, LastTradePrice=615m,  ChangePercent=-0.66m, Volume=4321000,  IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=3, TradingCode="BRACBANK",  CompanyName="BRAC Bank",       Exchange="DSE", Category=StockCategory.A, LastTradePrice=48m,   ChangePercent=1.68m,  Volume=32410000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=4, TradingCode="NBL",       CompanyName="National Bank",   Exchange="CSE", Category=StockCategory.A, LastTradePrice=14m,   ChangePercent=0.71m,  Volume=54320000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=5, TradingCode="ISLAMIBANK",CompanyName="Islami Bank",     Exchange="DSE", Category=StockCategory.A, LastTradePrice=35m,   ChangePercent=-1.14m, Volume=41230000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
        };
        db.Stocks.AddRange(stocks);
        db.SaveChanges();
    }

    // ── MarketData create & validate ───────────────────────────

    [Fact]
    public async Task CreateMarketData_ValidDSE_ReturnsSuccess()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=1, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=375m, High=385m, Low=370m, Close=380m, Volume=500000
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("GP", result.Value!.TradingCode);
    }

    [Fact]
    public async Task CreateMarketData_ValidCSE_ReturnsSuccess()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=4, Exchange="CSE", Date=DateTime.UtcNow.Date,
            Open=13m, High=15m, Low=13m, Close=14m, Volume=1000000
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("NBL", result.Value!.TradingCode);
        Assert.Equal("CSE", result.Value.Exchange);
    }

    [Fact]
    public async Task CreateMarketData_InvalidStock_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=999, Exchange="DSE", Date=DateTime.UtcNow.Date
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Stock not found", result.Error);
    }

    [Fact]
    public async Task CreateMarketData_DuplicateDate_ReturnsFail()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);
        var date = DateTime.UtcNow.Date;

        await svc.CreateAsync(new CreateMarketDataDto { StockId=1, Exchange="DSE", Date=date, Open=375m, High=385m, Low=370m, Close=380m });
        var result = await svc.CreateAsync(new CreateMarketDataDto { StockId=1, Exchange="DSE", Date=date, Open=375m, High=385m, Low=370m, Close=380m });

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task CreateMarketData_CloseAboveOpen_IsGainer()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=1, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=370m, High=390m, Low=368m, Close=385m, Volume=100000
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Close > result.Value.Open);
    }

    [Fact]
    public async Task CreateMarketData_CloseBelowOpen_IsLoser()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=1, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=385m, High=390m, Low=368m, Close=370m, Volume=100000
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Close < result.Value.Open);
    }

    [Fact]
    public async Task CreateMarketData_ZeroVolume_NotTraded()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=1, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=380m, High=380m, Low=380m, Close=380m, Volume=0
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Volume);
    }

    [Fact]
    public async Task CreateMarketData_HighVolume_MostActive()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=3, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=46m, High=50m, Low=46m, Close=48m, Volume=54320000
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(54320000, result.Value!.Volume);
    }

    [Fact]
    public async Task CreateMarketData_HighValue_TopByValue()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var result = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=2, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=610m, High=620m, Low=608m, Close=615m,
            Volume=1200000, ValueInMillionTaka=738m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(738m, result.Value!.ValueInMillionTaka);
    }

    [Fact]
    public async Task BulkCreate_SkipsDuplicates_ReturnsCorrectCounts()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);
        var date = DateTime.UtcNow.Date;

        await svc.CreateAsync(new CreateMarketDataDto { StockId=1, Exchange="DSE", Date=date, Open=375m, High=385m, Low=370m, Close=380m });

        var result = await svc.BulkCreateAsync(new BulkMarketDataDto
        {
            Items = new List<CreateMarketDataDto>
            {
                new() { StockId=1, Exchange="DSE", Date=date,              Open=375m, High=385m, Low=370m, Close=380m },
                new() { StockId=1, Exchange="DSE", Date=date.AddDays(-1),  Open=370m, High=380m, Low=365m, Close=375m }
            }
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Created);
        Assert.Equal(1, result.Value.Skipped);
    }

    [Fact]
    public async Task DeleteMarketData_Existing_ReturnsSuccess()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new MarketDataService(db);

        var created = await svc.CreateAsync(new CreateMarketDataDto
        {
            StockId=1, Exchange="DSE", Date=DateTime.UtcNow.Date,
            Open=375m, High=385m, Low=370m, Close=380m
        });

        var result = await svc.DeleteAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
    }
}

// ============================================================
//  Day 61 — Stock Service Tests (ScoreBoard / MostActive data)
// ============================================================

public class Day61StockServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private void SeedStocks(AppDbContext db)
    {
        var stocks = new[]
        {
            new Stock { Id=1, TradingCode="GP",        CompanyName="Grameenphone",   Exchange="DSE", Category=StockCategory.A, LastTradePrice=380m,  ChangePercent=0.61m,  Volume=18234000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=2, TradingCode="BATBC",     CompanyName="BAT Bangladesh", Exchange="DSE", Category=StockCategory.A, LastTradePrice=615m,  ChangePercent=-0.66m, Volume=4321000,  IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=3, TradingCode="BRACBANK",  CompanyName="BRAC Bank",      Exchange="DSE", Category=StockCategory.A, LastTradePrice=48m,   ChangePercent=1.68m,  Volume=32410000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=4, TradingCode="NBL",       CompanyName="National Bank",  Exchange="CSE", Category=StockCategory.A, LastTradePrice=14m,   ChangePercent=0.71m,  Volume=54320000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
            new Stock { Id=5, TradingCode="ISLAMIBANK",CompanyName="Islami Bank",    Exchange="DSE", Category=StockCategory.A, LastTradePrice=35m,   ChangePercent=-1.14m, Volume=41230000, IsActive=true, LastUpdatedAt=DateTime.UtcNow },
        };
        db.Stocks.AddRange(stocks);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAllStocks_ReturnsAllActive()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetAllStocks_DSECount_IsFour()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();
        var dse = result.Where(s => s.Exchange == "DSE").ToList();

        Assert.Equal(4, dse.Count);
    }

    [Fact]
    public async Task GetAllStocks_CSECount_IsOne()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();
        var cse = result.Where(s => s.Exchange == "CSE").ToList();

        Assert.Single(cse);
        Assert.Equal("NBL", cse[0].TradingCode);
    }

    [Fact]
    public async Task GetAllStocks_Gainers_ThreePositive()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();
        var gainers = result.Where(s => s.ChangePercent > 0).ToList();

        Assert.Equal(3, gainers.Count);
    }

    [Fact]
    public async Task GetAllStocks_Losers_TwoNegative()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();
        var losers = result.Where(s => s.ChangePercent < 0).ToList();

        Assert.Equal(2, losers.Count);
    }

    [Fact]
    public async Task GetAllStocks_TopByVolume_IsNBL()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetAllStocksAsync();
        var top = result.OrderByDescending(s => s.Volume).First();

        Assert.Equal("NBL", top.TradingCode);
    }

    [Fact]
    public async Task GetStockById_ExistingStock_ReturnsGP()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetStockByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("GP", result!.TradingCode);
    }

    [Fact]
    public async Task GetStockById_NonExistent_ReturnsNull()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.GetStockByIdAsync(9999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchStocks_ByPrefix_ReturnsMatches()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.SearchStocksAsync("BR");

        Assert.True(result.Count >= 1);
        Assert.Contains(result, s => s.TradingCode == "BRACBANK");
    }

    [Fact]
    public async Task SearchStocks_NoMatch_ReturnsEmpty()
    {
        var db = CreateDb(); SeedStocks(db);
        var svc = new StockService(db);

        var result = await svc.SearchStocksAsync("ZZZZZ");

        Assert.Empty(result);
    }
}

// ============================================================
//  Day 61 — News Feed Tests (NewsFeedWidget backend)
// ============================================================

public class Day61NewsFeedTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateNews_MarketUpdate_ReturnsSuccess()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title="DSEX rises 1.2% on banking gains",
            Content="The Dhaka Stock Exchange index rose driven by banking stocks.",
            Category="MarketUpdate", IsPublished=true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("MarketUpdate", result.Value!.Category);
    }

    [Fact]
    public async Task CreateNews_RegulatoryUpdate_ReturnsSuccess()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title="BSEC issues new margin trading guidelines",
            Content="The Bangladesh Securities and Exchange Commission issued updated guidelines.",
            Category="RegulatoryUpdate", IsPublished=true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("RegulatoryUpdate", result.Value!.Category);
    }

    [Fact]
    public async Task CreateNews_Published_IsPublished()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title="Test news item day 61",
            Content="Test content for day 61 news feed widget.",
            Category="MarketUpdate", IsPublished=true
        });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublished);
    }

    [Fact]
    public async Task CreateNews_InvalidCategory_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title="Test", Content="Test content", Category="BadCategory"
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task PublishNews_Unpublished_BecomesPublished()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var created = await svc.CreateAsync(new CreateNewsDto
        {
            Title="Circuit breaker triggered on 3 stocks",
            Content="Trading halted after circuit breaker triggered.",
            Category="MarketUpdate", IsPublished=false
        });

        var published = await svc.PublishAsync(created.Value!.Id);
        var fetched   = await svc.GetByIdAsync(created.Value.Id);

        Assert.True(published.IsSuccess);
        Assert.True(fetched.Value!.IsPublished);
    }

    [Fact]
    public async Task CreateNews_EmptyTitle_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.CreateAsync(new CreateNewsDto
        {
            Title="", Content="Some content", Category="MarketUpdate"
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteNews_Existing_ReturnsSuccess()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var created = await svc.CreateAsync(new CreateNewsDto
        {
            Title="Temp news", Content="Temp content", Category="MarketUpdate"
        });

        var deleted = await svc.DeleteAsync(created.Value!.Id);

        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsFail()
    {
        var db = CreateDb();
        var svc = new NewsService(db);

        var result = await svc.GetByIdAsync(9999);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }
}
