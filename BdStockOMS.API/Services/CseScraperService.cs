using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BdStockOMS.API.Services;

public class CseScraperService : ICseScraperService
{
    private readonly HttpClient _http;
    private readonly ILogger<CseScraperService> _logger;

    private static readonly TimeZoneInfo BST = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Bangladesh Standard Time" : "Asia/Dhaka");

    public CseScraperService(IHttpClientFactory httpClientFactory,
                              ILogger<CseScraperService> logger)
    {
        _http   = httpClientFactory.CreateClient("CSE");
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

    public async Task<List<CseStockTick>> GetAllPricesAsync(CancellationToken ct = default)
    {
        var results = new List<CseStockTick>();
        try
        {
            var html = await _http.GetStringAsync(
                "https://www.cse.com.bd/market/current_price", ct);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table//tr");
            if (rows == null)
            {
                _logger.LogWarning("CSE scraper: no table rows found");
                return results;
            }

            foreach (var row in rows.Skip(1)) // skip header
            {
                try
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 5) continue;

                    var code = cells[1].InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    if (!decimal.TryParse(cells[2].InnerText.Trim().Replace(",", ""),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var price)) continue;

                    decimal.TryParse(cells[3].InnerText.Trim().Replace(",", ""),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var change);

                    var pctStr = cells[4].InnerText.Trim().Replace("%", "").Replace(",", "");
                    decimal.TryParse(pctStr,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var changePct);

                    var direction = change > 0 ? "up" : change < 0 ? "down" : "neutral";

                    if (price > 0)
                        results.Add(new CseStockTick(code, price, change, changePct, direction));
                }
                catch { }
            }

            _logger.LogInformation("CSE scraper: fetched {Count} stocks", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSE scraper failed");
        }
        return results;
    }

    public async Task<CseIndexData?> GetIndexDataAsync(CancellationToken ct = default)
    {
        try
        {
            var html = await _http.GetStringAsync(
                "https://www.cse.com.bd/market/index_chart", ct);

            var numbers = Regex.Matches(html, @"([\d,]+\.\d{2})")
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
                .Take(3)
                .ToList();

            if (numbers.Count >= 2)
                return new CseIndexData(
                    CASPI:       numbers[0],
                    CSE30:       numbers.Count > 1 ? numbers[1] : 0,
                    CASPIChange: 0,
                    FetchedAt:   DateTime.UtcNow
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSE index scraper failed");
        }
        return null;
    }
}
