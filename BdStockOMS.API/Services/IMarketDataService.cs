using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.Common;

namespace BdStockOMS.API.Services;

public interface IMarketDataService
{
    Task<Result<PagedResult<MarketDataResponseDto>>> GetAllAsync(MarketDataQueryDto query);
    Task<Result<MarketDataResponseDto>> GetByIdAsync(int id);
    Task<Result<List<MarketDataResponseDto>>> GetByStockAsync(int stockId, string exchange, int days = 30);
    Task<Result<MarketDataResponseDto>> CreateAsync(CreateMarketDataDto dto);
    Task<Result<BulkMarketDataResultDto>> BulkCreateAsync(BulkMarketDataDto dto);
    Task<Result<bool>> DeleteAsync(int id);
}

public class BulkMarketDataResultDto
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}
