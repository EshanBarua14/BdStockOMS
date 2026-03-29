using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.IPO;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface IIPOService
    {
        Task<Result<IPOResponseDto>>              CreateIPOAsync(CreateIPODto dto, CancellationToken ct = default);
        Task<Result<IPOResponseDto>>              GetIPOAsync(int id, CancellationToken ct = default);
        Task<Result<List<IPOResponseDto>>>        GetAllIPOsAsync(string? status, CancellationToken ct = default);
        Task<Result<IPOApplicationResponseDto>>   ApplyAsync(ApplyIPODto dto, CancellationToken ct = default);
        Task<Result<IPOAllocationResultDto>>      AllocateAsync(int ipoId, CancellationToken ct = default);
        Task<Result<int>>                         ProcessRefundsAsync(int ipoId, CancellationToken ct = default);
        Task<Result<List<IPOApplicationResponseDto>>> GetApplicationsAsync(int ipoId, CancellationToken ct = default);
        Task<Result<IPOApplicationResponseDto>>   GetApplicationAsync(int applicationId, CancellationToken ct = default);
        Task<Result<bool>>                        CloseIPOAsync(int ipoId, CancellationToken ct = default);
    }
}
