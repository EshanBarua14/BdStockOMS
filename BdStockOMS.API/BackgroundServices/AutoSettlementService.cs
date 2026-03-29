using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

public class AutoSettlementService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoSettlementService> _logger;

    public AutoSettlementService(IServiceScopeFactory scopeFactory,
        ILogger<AutoSettlementService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AutoSettlementService started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessDueBatchesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoSettlementService error.");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }

    private async Task ProcessDueBatchesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var settlement  = scope.ServiceProvider.GetRequiredService<ISettlementService>();

        var today = DateTime.UtcNow.Date;

        // Find all pending batches whose settlement date has arrived
        var dueBatches = await db.SettlementBatches
            .Where(b => b.Status == SettlementBatchStatus.Pending
                     && b.SettlementDate.Date <= today)
            .ToListAsync(ct);

        if (!dueBatches.Any()) return;

        _logger.LogInformation("AutoSettlement: {Count} batches due for processing.", dueBatches.Count);

        foreach (var batch in dueBatches)
        {
            try
            {
                _logger.LogInformation("AutoSettlement: Processing batch {Id} for {Exchange} TradeDate={Date}",
                    batch.Id, batch.Exchange, batch.TradeDate.ToString("yyyy-MM-dd"));

                var result = await settlement.ProcessBatchAsync(batch.Id);

                _logger.LogInformation("AutoSettlement: Batch {Id} {Status}. Items={Count}",
                    batch.Id, result.Status, result.Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoSettlement: Failed to process batch {Id}", batch.Id);
            }
        }
    }
}
