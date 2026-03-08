using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.TraderReassignment;

namespace BdStockOMS.API.Services;

public interface ITraderReassignmentService
{
    Task<Result<TraderReassignmentResponseDto>> ReassignTraderAsync(int reassignedByUserId, CreateTraderReassignmentDto dto);
    Task<Result<List<TraderReassignmentResponseDto>>> GetByInvestorAsync(int investorId);
    Task<Result<List<TraderReassignmentResponseDto>>> GetByBrokerageHouseAsync(int brokerageHouseId);
}
