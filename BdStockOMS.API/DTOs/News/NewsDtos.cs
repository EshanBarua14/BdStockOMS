using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [MinLength(10, ErrorMessage = "Content must be at least 10 characters.")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
    public string? Source { get; set; }

    [MaxLength(500, ErrorMessage = "ExternalUrl cannot exceed 500 characters.")]
    public string? ExternalUrl { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = "General";

    public int? RelatedStockId { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class UpdateNewsDto
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [MinLength(10, ErrorMessage = "Content must be at least 10 characters.")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Source cannot exceed 100 characters.")]
    public string? Source { get; set; }

    [MaxLength(500, ErrorMessage = "ExternalUrl cannot exceed 500 characters.")]
    public string? ExternalUrl { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = "General";

    public int? RelatedStockId { get; set; }
    public bool IsPublished { get; set; }
}

public class NewsQueryDto
{
    public string? Category { get; set; }
    public int? RelatedStockId { get; set; }
    public bool? IsPublished { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
