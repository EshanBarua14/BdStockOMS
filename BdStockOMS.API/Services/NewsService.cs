using BdStockOMS.API.Models;
using Microsoft.AspNetCore.SignalR;

namespace BdStockOMS.API.Services;

public interface INewsService
{
    Task<NewsPagedResult> GetNewsAsync(NewsFilterRequest filter);
    Task<NewsItem?> GetNewsByIdAsync(int id);
    Task BroadcastNewsAsync(NewsItem item);
}

public class NewsService : INewsService
{
    private readonly IHubContext<Hubs.NewsHub> _hubContext;
    private readonly ILogger<NewsService> _logger;

    // In production, replace with EF Core DbContext query
    private static readonly List<NewsItem> _seedData = GenerateSeedData();

    public NewsService(
        IHubContext<Hubs.NewsHub> hubContext,
        ILogger<NewsService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<NewsPagedResult> GetNewsAsync(NewsFilterRequest filter)
    {
        var query = _seedData.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.ToLower();
            query = query.Where(n =>
                n.Title.ToLower().Contains(kw) ||
                n.Summary.ToLower().Contains(kw) ||
                n.Keywords.Any(k => k.ToLower().Contains(kw)) ||
                (n.TradingCode != null && n.TradingCode.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Board) && filter.Board != "ALL")
            query = query.Where(n => n.Board == filter.Board.ToUpper() || n.Board == "ALL");

        if (!string.IsNullOrWhiteSpace(filter.Category) && filter.Category != "all")
            query = query.Where(n => n.Category == filter.Category.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.TradingCode))
            query = query.Where(n => n.TradingCode == filter.TradingCode.ToUpper());

        if (filter.FromDate.HasValue)
            query = query.Where(n => n.PublishedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(n => n.PublishedAt <= filter.ToDate.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return await Task.FromResult(new NewsPagedResult
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<NewsItem?> GetNewsByIdAsync(int id)
    {
        return await Task.FromResult(_seedData.FirstOrDefault(n => n.Id == id));
    }

    public async Task BroadcastNewsAsync(NewsItem item)
    {
        // Broadcast to keyword groups
        foreach (var keyword in item.Keywords)
        {
            await _hubContext.Clients
                .Group($"keyword:{keyword.ToLower()}")
                .SendAsync("ReceiveNews", item);
        }

        // Broadcast to board group
        if (!string.IsNullOrEmpty(item.Board))
        {
            await _hubContext.Clients
                .Group($"board:{item.Board.ToUpper()}")
                .SendAsync("ReceiveNews", item);
        }

        // Broadcast to category group
        await _hubContext.Clients
            .Group($"category:{item.Category.ToLower()}")
            .SendAsync("ReceiveNews", item);

        _logger.LogInformation("Broadcasted news item {Id}: {Title}", item.Id, item.Title);
    }

    private static List<NewsItem> GenerateSeedData()
    {
        var now = DateTime.UtcNow;
        return new List<NewsItem>
        {
            new() {
                Id = 1,
                Title = "BRAC Bank Reports 18% YoY Profit Growth in Q1 2026",
                Summary = "BRAC Bank Limited has reported a net profit of BDT 485 crore for the first quarter of 2026, marking an 18% increase year-on-year driven by strong retail and SME loan growth.",
                Category = "price-sensitive",
                Board = "A",
                TradingCode = "BRACBANK",
                Source = "DSE Disclosure",
                IsPriceSensitive = true,
                PublishedAt = now.AddMinutes(-12),
                Keywords = new() { "brac bank", "profit", "q1", "banking" }
            },
            new() {
                Id = 2,
                Title = "DSE Launches New Circuit Breaker Rules Effective April 2026",
                Summary = "The Dhaka Stock Exchange has announced updated circuit breaker rules with 10% upper and lower limits for A-category stocks, effective April 1, 2026.",
                Category = "regulatory",
                Board = "ALL",
                Source = "DSE Circular",
                IsPriceSensitive = false,
                PublishedAt = now.AddMinutes(-45),
                Keywords = new() { "circuit breaker", "dse", "regulation", "trading rules" }
            },
            new() {
                Id = 3,
                Title = "Square Pharma Declares 30% Cash Dividend for FY2025",
                Summary = "Square Pharmaceuticals Ltd declared a 30% cash dividend for fiscal year 2025, with record date set for March 30, 2026.",
                Category = "price-sensitive",
                Board = "A",
                TradingCode = "SQURPHARMA",
                Source = "DSE Disclosure",
                IsPriceSensitive = true,
                PublishedAt = now.AddHours(-2),
                Keywords = new() { "square pharma", "dividend", "pharma" }
            },
            new() {
                Id = 4,
                Title = "BSEC Approves New IPO for Energypac Power Generation",
                Summary = "Bangladesh Securities and Exchange Commission has approved the IPO application of Energypac Power Generation Ltd with an issue size of BDT 150 crore.",
                Category = "corporate",
                Board = "N",
                TradingCode = "ENERGYPAC",
                Source = "BSEC Notice",
                IsPriceSensitive = false,
                PublishedAt = now.AddHours(-3),
                Keywords = new() { "ipo", "energypac", "bsec", "new listing" }
            },
            new() {
                Id = 5,
                Title = "Grameenphone Subscriber Base Crosses 85 Million Mark",
                Summary = "Grameenphone Ltd announced its subscriber base has surpassed 85 million as of February 2026, with data revenue growing 22% quarter-on-quarter.",
                Category = "general",
                Board = "A",
                TradingCode = "GP",
                Source = "Company Press Release",
                IsPriceSensitive = false,
                PublishedAt = now.AddHours(-4),
                Keywords = new() { "grameenphone", "subscriber", "telecom", "data revenue" }
            },
            new() {
                Id = 6,
                Title = "Z-Category Stocks Under Review: BSEC Issues Warning",
                Summary = "BSEC has placed 12 companies under enhanced surveillance following failure to hold AGMs and continued non-disclosure of financial results.",
                Category = "regulatory",
                Board = "Z",
                Source = "BSEC Circular",
                IsPriceSensitive = false,
                PublishedAt = now.AddHours(-6),
                Keywords = new() { "z category", "bsec", "agm", "compliance" }
            },
            new() {
                Id = 7,
                Title = "Dutch-Bangla Bank Q4 EPS Rises to BDT 4.82",
                Summary = "Dutch-Bangla Bank Limited reported earnings per share of BDT 4.82 for Q4 2025, up from BDT 3.95 in the same period last year.",
                Category = "price-sensitive",
                Board = "A",
                TradingCode = "DUTCHBANGL",
                Source = "DSE Disclosure",
                IsPriceSensitive = true,
                PublishedAt = now.AddHours(-8),
                Keywords = new() { "dutch bangla bank", "eps", "earnings", "banking" }
            },
            new() {
                Id = 8,
                Title = "SME Board Gets 3 New Listings in March 2026",
                Summary = "The SME board of DSE welcomed three new companies in March 2026: TechVision BD Ltd, AgroFresh Processing, and Coastal Maritime Services.",
                Category = "corporate",
                Board = "SME",
                Source = "DSE Announcement",
                IsPriceSensitive = false,
                PublishedAt = now.AddHours(-10),
                Keywords = new() { "sme", "listing", "new company", "dse" }
            },
        };
    }
}
