using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.TraderReassignment;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class TraderReassignmentService : ITraderReassignmentService
{
    private readonly AppDbContext _context;

    public TraderReassignmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TraderReassignmentResponseDto>> ReassignTraderAsync(int reassignedByUserId, CreateTraderReassignmentDto dto)
    {
        var investor = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Id == dto.InvestorId);

        if (investor == null)
            return Result<TraderReassignmentResponseDto>.Failure("Investor not found.");

        if (investor.Role.Name != "Investor")
            return Result<TraderReassignmentResponseDto>.Failure("Target user is not an investor.");

        var newTrader = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.NewTraderId);

        if (newTrader == null)
            return Result<TraderReassignmentResponseDto>.Failure("New trader not found.");

        if (newTrader.Role.Name != "Trader")
            return Result<TraderReassignmentResponseDto>.Failure("Target user is not a trader.");

        if (newTrader.BrokerageHouseId != investor.BrokerageHouseId)
            return Result<TraderReassignmentResponseDto>.Failure("New trader must belong to the same brokerage house as the investor.");

        if (investor.AssignedTraderId == dto.NewTraderId)
            return Result<TraderReassignmentResponseDto>.Failure("Investor is already assigned to this trader.");

        var reassignedBy = await _context.Users.FindAsync(reassignedByUserId);
        if (reassignedBy == null)
            return Result<TraderReassignmentResponseDto>.Failure("Reassigning user not found.");

        User? oldTrader = null;
        if (investor.AssignedTraderId.HasValue)
            oldTrader = await _context.Users.FindAsync(investor.AssignedTraderId.Value);

        var record = new TraderReassignment
        {
            InvestorId = dto.InvestorId,
            OldTraderId = investor.AssignedTraderId,
            NewTraderId = dto.NewTraderId,
            ReassignedByUserId = reassignedByUserId,
            BrokerageHouseId = investor.BrokerageHouseId,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        investor.AssignedTraderId = dto.NewTraderId;

        _context.TraderReassignments.Add(record);
        await _context.SaveChangesAsync();

        record.Investor = investor;
        record.OldTrader = oldTrader;
        record.NewTrader = newTrader;
        record.ReassignedBy = reassignedBy;
        record.BrokerageHouse = investor.BrokerageHouse;

        return Result<TraderReassignmentResponseDto>.Success(ToDto(record));
    }

    public async Task<Result<List<TraderReassignmentResponseDto>>> GetByInvestorAsync(int investorId)
    {
        var investor = await _context.Users.FindAsync(investorId);
        if (investor == null)
            return Result<List<TraderReassignmentResponseDto>>.Failure("Investor not found.");

        var items = await _context.TraderReassignments
            .Include(r => r.Investor)
            .Include(r => r.OldTrader)
            .Include(r => r.NewTrader)
            .Include(r => r.ReassignedBy)
            .Include(r => r.BrokerageHouse)
            .Where(r => r.InvestorId == investorId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Result<List<TraderReassignmentResponseDto>>.Success(items);
    }

    public async Task<Result<List<TraderReassignmentResponseDto>>> GetByBrokerageHouseAsync(int brokerageHouseId)
    {
        var items = await _context.TraderReassignments
            .Include(r => r.Investor)
            .Include(r => r.OldTrader)
            .Include(r => r.NewTrader)
            .Include(r => r.ReassignedBy)
            .Include(r => r.BrokerageHouse)
            .Where(r => r.BrokerageHouseId == brokerageHouseId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Result<List<TraderReassignmentResponseDto>>.Success(items);
    }

    private static TraderReassignmentResponseDto ToDto(TraderReassignment r) => new()
    {
        Id = r.Id,
        InvestorId = r.InvestorId,
        InvestorName = r.Investor?.FullName ?? string.Empty,
        OldTraderId = r.OldTraderId,
        OldTraderName = r.OldTrader?.FullName,
        NewTraderId = r.NewTraderId,
        NewTraderName = r.NewTrader?.FullName ?? string.Empty,
        ReassignedByUserId = r.ReassignedByUserId,
        ReassignedByName = r.ReassignedBy?.FullName ?? string.Empty,
        BrokerageHouseId = r.BrokerageHouseId,
        BrokerageHouseName = r.BrokerageHouse?.Name ?? string.Empty,
        Reason = r.Reason,
        CreatedAt = r.CreatedAt
    };
}
