namespace BdStockOMS.API.Models;

public enum NewsCategory
{
    MarketUpdate,
    CompanyNews,
    RegulatoryUpdate,
    EconomicNews,
    General
}

public class NewsItem
{
    public int Id              { get; set; }
    public string Title        { get; set; } = string.Empty;
    public string Content      { get; set; } = string.Empty;
    public string? Source      { get; set; }
    public string? ExternalUrl { get; set; }
    public NewsCategory Category { get; set; } = NewsCategory.General;
    public int? RelatedStockId { get; set; }
    public bool IsPublished    { get; set; } = true;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Stock? RelatedStock { get; set; }
}
