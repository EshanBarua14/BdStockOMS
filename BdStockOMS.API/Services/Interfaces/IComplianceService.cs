using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services.Interfaces
{
    public interface IComplianceService
    {
        Task<List<ComplianceReport>> ScanOrderAsync(Order order, CancellationToken ct = default);
        Task<List<ComplianceReport>> ScanTradeHistoryAsync(string clientId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<ComplianceReport?> GetReportAsync(Guid reportId, CancellationToken ct = default);
        Task<PagedResult<ComplianceReport>> GetReportsAsync(ComplianceFilterDto filter, CancellationToken ct = default);
        Task<ComplianceReport> ResolveReportAsync(Guid reportId, ResolveComplianceDto dto, CancellationToken ct = default);
        Task<ComplianceReport> EscalateReportAsync(Guid reportId, string reason, string escalatedBy, CancellationToken ct = default);
        Task<ComplianceSummaryDto> GetSummaryAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
        Task<byte[]> ExportReportsAsync(ComplianceExportDto dto, CancellationToken ct = default);
    }
}
