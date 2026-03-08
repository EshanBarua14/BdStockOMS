namespace BdStockOMS.API.DTOs.News;

public class NewsResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ExternalUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? RelatedStockId { get; set; }
    public string? RelatedTradingCode { get; set; }
    public bool IsPublished { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ExternalUrl { get; set; }
    public string Category { get; set; } = "General";
    public int? RelatedStockId { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class UpdateNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ExternalUrl { get; set; }
    public string Category { get; set; } = "General";
    public int? RelatedStockId { get; set; }
    public bool IsPublished { get; set; }
}

public class NewsQueryDto
{
    public string? Category { get; set; }
    public int? RelatedStockId { get; set; }
    public bool? IsPublished { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
