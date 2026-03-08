using BdStockOMS.API.DTOs.News;
using BdStockOMS.API.Common;

namespace BdStockOMS.API.Services;

public interface INewsService
{
    Task<Result<PagedResult<NewsResponseDto>>> GetAllAsync(NewsQueryDto query);
    Task<Result<NewsResponseDto>> GetByIdAsync(int id);
    Task<Result<NewsResponseDto>> CreateAsync(CreateNewsDto dto);
    Task<Result<NewsResponseDto>> UpdateAsync(int id, UpdateNewsDto dto);
    Task<Result<bool>> PublishAsync(int id);
    Task<Result<bool>> UnpublishAsync(int id);
    Task<Result<bool>> DeleteAsync(int id);
}
