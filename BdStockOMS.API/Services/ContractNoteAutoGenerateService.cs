using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IContractNoteAutoGenerateService
{
    Task GenerateOnFillAsync(int orderId);
    Task GeneratePendingAsync(int brokerageHouseId);
}

public class ContractNoteAutoGenerateService : IContractNoteAutoGenerateService
{
    private readonly AppDbContext _db;
    private readonly IContractNoteService _contractNotes;
    private readonly ILogger<ContractNoteAutoGenerateService> _logger;

    public ContractNoteAutoGenerateService(
        AppDbContext db,
        IContractNoteService contractNotes,
        ILogger<ContractNoteAutoGenerateService> logger)
    {
        _db            = db;
        _contractNotes = contractNotes;
        _logger        = logger;
    }

    public async Task GenerateOnFillAsync(int orderId)
    {
        try
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return;

            if (order.Status != OrderStatus.Filled && order.Status != OrderStatus.Completed)
                return;

            var existing = await _db.ContractNotes
                .AnyAsync(c => c.OrderId == orderId && !c.IsVoid);
            if (existing) return;

            var result = await _contractNotes.GenerateContractNoteAsync(orderId);
            if (result.Success)
                _logger.LogInformation("Auto-generated contract note {Number} for order {Id}",
                    result.ContractNote?.ContractNoteNumber, orderId);
            else
                _logger.LogWarning("Failed to auto-generate contract note for order {Id}: {Msg}",
                    orderId, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-generating contract note for order {Id}", orderId);
        }
    }

    public async Task GeneratePendingAsync(int brokerageHouseId)
    {
        // Find all filled/completed orders without contract notes
        var ordersWithoutNotes = await _db.Orders
            .Where(o => o.BrokerageHouseId == brokerageHouseId
                     && (o.Status == OrderStatus.Filled || o.Status == OrderStatus.Completed))
            .Where(o => !_db.ContractNotes.Any(c => c.OrderId == o.Id && !c.IsVoid))
            .Select(o => o.Id)
            .ToListAsync();

        _logger.LogInformation("GeneratePending: {Count} orders need contract notes", ordersWithoutNotes.Count);

        foreach (var orderId in ordersWithoutNotes)
        {
            var result = await _contractNotes.GenerateContractNoteAsync(orderId);
            if (!result.Success)
                _logger.LogWarning("Failed to generate contract note for order {Id}: {Msg}",
                    orderId, result.Message);
        }
    }
}
