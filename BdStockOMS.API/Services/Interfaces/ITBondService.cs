using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.TBond;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface ITBondService
    {
        Task<Result<TBondResponseDto>>            CreateAsync(CreateTBondDto dto, CancellationToken ct = default);
        Task<Result<TBondResponseDto>>            GetAsync(int id, CancellationToken ct = default);
        Task<Result<List<TBondResponseDto>>>      GetAllAsync(string? status, CancellationToken ct = default);
        Task<Result<TBondOrderResponseDto>>       PlaceOrderAsync(PlaceTBondOrderDto dto, CancellationToken ct = default);
        Task<Result<TBondOrderResponseDto>>       ExecuteOrderAsync(int orderId, CancellationToken ct = default);
        Task<Result<TBondOrderResponseDto>>       SettleOrderAsync(int orderId, CancellationToken ct = default);
        Task<Result<TBondOrderResponseDto>>       CancelOrderAsync(int orderId, CancellationToken ct = default);
        Task<Result<List<TBondOrderResponseDto>>> GetOrdersAsync(int? investorId, int? tbondId, CancellationToken ct = default);
        Task<Result<List<CouponPaymentResponseDto>>> GenerateCouponsAsync(int tbondId, CancellationToken ct = default);
        Task<Result<int>>                         PayCouponsAsync(int tbondId, DateTime upTo, CancellationToken ct = default);
        Task<Result<List<CouponPaymentResponseDto>>> GetCouponsAsync(int tbondId, CancellationToken ct = default);
        Task<Result<MaturityProcessResultDto>>    ProcessMaturityAsync(int tbondId, CancellationToken ct = default);
        Task<Result<List<TBondHoldingResponseDto>>> GetHoldingsAsync(int investorId, CancellationToken ct = default);
    }
}
