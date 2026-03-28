const fs = require("fs");

// ── IDseScraperService interface ─────────────────────────────
fs.writeFileSync("BdStockOMS.API/Services/IDseScraperService.cs", `namespace BdStockOMS.API.Services;

public record DseStockTick(
    string  TradingCode,
    decimal LastTradePrice,
    decimal Change,
    decimal ChangePercent,
    string  Direction  // "up" | "down" | "neutral"
);

public record DseIndexData(
    decimal DSEX,
    decimal DSES,
    decimal DS30,
    decimal DSEXChange,
    DateTime FetchedAt
);

public interface IDseScraperService
{
    Task<List<DseStockTick>> GetAllPricesAsync(CancellationToken ct = default);
    Task<DseIndexData?>      GetIndexDataAsync(CancellationToken ct = default);
    bool IsMarketOpen();
}
`);
console.log("IDseScraperService.cs written");

// ── DseScraperService implementation ────────────────────────
fs.writeFileSync("BdStockOMS.API/Services/DseScraperService.cs", `using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BdStockOMS.API.Services;

public class DseScraperService : IDseScraperService
{
    private readonly HttpClient _http;
    private readonly ILogger<DseScraperService> _logger;

    // DSE trading hours: Sun-Thu 10:00-14:30 BST (UTC+6)
    private static readonly TimeZoneInfo BST = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Bangladesh Standard Time" : "Asia/Dhaka");

    public DseScraperService(IHttpClientFactory httpClientFactory,
                              ILogger<DseScraperService> logger)
    {
        _http   = httpClientFactory.CreateClient("DSE");
        _logger = logger;
    }

    public bool IsMarketOpen()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BST);
        if (now.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) return false;
        var open  = new TimeSpan(10, 0, 0);
        var close = new TimeSpan(14, 30, 0);
        return now.TimeOfDay >= open && now.TimeOfDay <= close;
    }

    public async Task<List<DseStockTick>> GetAllPricesAsync(CancellationToken ct = default)
    {
        var results = new List<DseStockTick>();
        try
        {
            var html = await _http.GetStringAsync(
                "https://dsebd.org/latest_share_price_scroll_l.php", ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Each stock is in an anchor with class 'abhead'
            var anchors = doc.DocumentNode
                .SelectNodes("//a[@class='abhead']");

            if (anchors == null)
            {
                _logger.LogWarning("DSE scraper: no abhead anchors found");
                return results;
            }

            foreach (var anchor in anchors)
            {
                try
                {
                    // Inner text looks like: "GP 380.00  [img]  \\n  2.50    0.66%"
                    // After decoding nbsp; we get the code and numbers
                    var text = anchor.InnerText
                        .Replace("\\u00a0", " ")
                        .Replace("&nbsp;", " ")
                        .Trim();

                    // Also look at the raw inner text with multiple spaces cleaned
                    var cleaned = Regex.Replace(text, @"\\s+", " ").Trim();
                    var parts   = cleaned.Split(' ',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

                    if (parts.Length < 3) continue;

                    var code  = parts[0].Trim();
                    if (!decimal.TryParse(parts[1],
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var price)) continue;

                    decimal change = 0;
                    decimal changePct = 0;

                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2],
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out change);

                    if (parts.Length >= 4)
                    {
                        var pctStr = parts[3].Replace("%", "").Trim();
                        decimal.TryParse(pctStr,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out changePct);
                    }

                    // Determine direction from img src (tkup/tkdown/tkupdown)
                    var imgSrc    = anchor.SelectSingleNode(".//img")?.GetAttributeValue("src", "") ?? "";
                    var direction = imgSrc.Contains("tkup") && !imgSrc.Contains("tkdown")
                        ? "up" : imgSrc.Contains("tkdown") ? "down" : "neutral";

                    if (!string.IsNullOrWhiteSpace(code) && price > 0)
                    {
                        results.Add(new DseStockTick(code, price, change, changePct, direction));
                    }
                }
                catch { /* skip malformed entries */ }
            }

            _logger.LogInformation("DSE scraper: fetched {Count} stocks", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DSE scraper failed");
        }
        return results;
    }

    public async Task<DseIndexData?> GetIndexDataAsync(CancellationToken ct = default)
    {
        try
        {
            var html = await _http.GetStringAsync(
                "https://dsebd.org/dseX_share.php", ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract all decimal numbers from the page
            var numbers = Regex.Matches(html, @"(?<![\\d])([\\d,]+\\.\\d{2})(?![\\d])")
                .Cast<Match>()
                .Select(m => {
                    decimal.TryParse(m.Value.Replace(",", ""),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var v);
                    return v;
                })
                .Where(v => v > 100)
                .Distinct()
                .OrderByDescending(v => v)
                .Take(5)
                .ToList();

            if (numbers.Count >= 3)
            {
                return new DseIndexData(
                    DSEX:       numbers[0],
                    DSES:       numbers.Count > 1 ? numbers[1] : 0,
                    DS30:       numbers.Count > 2 ? numbers[2] : 0,
                    DSEXChange: 0,
                    FetchedAt:  DateTime.UtcNow
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DSE index scraper failed");
        }
        return null;
    }
}
`);
console.log("DseScraperService.cs written");
