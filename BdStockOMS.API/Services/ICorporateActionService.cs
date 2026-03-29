using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.Common;

namespace BdStockOMS.API.Services;

public interface ICorporateActionService
{
    Task<Result<List<CorporateActionResponseDto>>> GetAllAsync(int? stockId, bool? isProcessed);
    Task<Result<CorporateActionResponseDto>> GetByIdAsync(int id);
    Task<Result<List<CorporateActionResponseDto>>> GetByStockAsync(int stockId);
    Task<Result<CorporateActionResponseDto>> CreateAsync(CreateCorporateActionDto dto);
    Task<Result<CorporateActionResponseDto>> UpdateAsync(int id, UpdateCorporateActionDto dto);
    Task<Result<bool>> MarkProcessedAsync(int id);
    Task<Result<bool>> DeleteAsync(int id);
    Task<Result<ProcessCorporateActionResultDto>> ProcessAsync(int id);
    Task<Result<List<CorporateActionLedgerEntryDto>>> GetLedgerAsync(int corporateActionId);
}
