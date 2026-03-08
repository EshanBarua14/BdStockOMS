using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.News;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class NewsService : INewsService
{
    private readonly AppDbContext _context;

    public NewsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<NewsResponseDto>>> GetAllAsync(NewsQueryDto query)
    {
        var q = _context.NewsItems
            .Include(n => n.RelatedStock)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            if (!Enum.TryParse<NewsCategory>(query.Category, true, out var cat))
                return Result<PagedResult<NewsResponseDto>>.Failure($"Invalid category: {query.Category}");
            q = q.Where(n => n.Category == cat);
        }

        if (query.RelatedStockId.HasValue)
            q = q.Where(n => n.RelatedStockId == query.RelatedStockId.Value);

        if (query.IsPublished.HasValue)
            q = q.Where(n => n.IsPublished == query.IsPublished.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(n => n.PublishedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => ToDto(n))
            .ToListAsync();

        return Result<PagedResult<NewsResponseDto>>.Success(new PagedResult<NewsResponseDto>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<Result<NewsResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.NewsItems
            .Include(n => n.RelatedStock)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (entity == null)
            return Result<NewsResponseDto>.Failure("News item not found.");

        return Result<NewsResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<NewsResponseDto>> CreateAsync(CreateNewsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<NewsResponseDto>.Failure("Title is required.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<NewsResponseDto>.Failure("Content is required.");

        if (!Enum.TryParse<NewsCategory>(dto.Category, true, out var category))
            return Result<NewsResponseDto>.Failure($"Invalid category: {dto.Category}");

        if (dto.RelatedStockId.HasValue)
        {
            var stock = await _context.Stocks.FindAsync(dto.RelatedStockId.Value);
            if (stock == null)
                return Result<NewsResponseDto>.Failure("Related stock not found.");
        }

        var entity = new NewsItem
        {
            Title = dto.Title,
            Content = dto.Content,
            Source = dto.Source,
            ExternalUrl = dto.ExternalUrl,
            Category = category,
            RelatedStockId = dto.RelatedStockId,
            IsPublished = dto.IsPublished,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.NewsItems.Add(entity);
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(n => n.RelatedStock).LoadAsync();
        return Result<NewsResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<NewsResponseDto>> UpdateAsync(int id, UpdateNewsDto dto)
    {
        var entity = await _context.NewsItems
            .Include(n => n.RelatedStock)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (entity == null)
            return Result<NewsResponseDto>.Failure("News item not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result<NewsResponseDto>.Failure("Title is required.");

        if (string.IsNullOrWhiteSpace(dto.Content))
            return Result<NewsResponseDto>.Failure("Content is required.");

        if (!Enum.TryParse<NewsCategory>(dto.Category, true, out var category))
            return Result<NewsResponseDto>.Failure($"Invalid category: {dto.Category}");

        if (dto.RelatedStockId.HasValue)
        {
            var stock = await _context.Stocks.FindAsync(dto.RelatedStockId.Value);
            if (stock == null)
                return Result<NewsResponseDto>.Failure("Related stock not found.");
        }

        entity.Title = dto.Title;
        entity.Content = dto.Content;
        entity.Source = dto.Source;
        entity.ExternalUrl = dto.ExternalUrl;
        entity.Category = category;
        entity.RelatedStockId = dto.RelatedStockId;
        entity.IsPublished = dto.IsPublished;

        await _context.SaveChangesAsync();
        return Result<NewsResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<bool>> PublishAsync(int id)
    {
        var entity = await _context.NewsItems.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("News item not found.");

        if (entity.IsPublished)
            return Result<bool>.Failure("News item is already published.");

        entity.IsPublished = true;
        entity.PublishedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UnpublishAsync(int id)
    {
        var entity = await _context.NewsItems.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("News item not found.");

        if (!entity.IsPublished)
            return Result<bool>.Failure("News item is already unpublished.");

        entity.IsPublished = false;
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var entity = await _context.NewsItems.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("News item not found.");

        _context.NewsItems.Remove(entity);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static NewsResponseDto ToDto(NewsItem n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Content = n.Content,
        Source = n.Source,
        ExternalUrl = n.ExternalUrl,
        Category = n.Category.ToString(),
        RelatedStockId = n.RelatedStockId,
        RelatedTradingCode = n.RelatedStock?.TradingCode,
        IsPublished = n.IsPublished,
        PublishedAt = n.PublishedAt,
        CreatedAt = n.CreatedAt
    };
}
