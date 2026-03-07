// Services/CCDService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.CCD;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface ICCDService
{
    Task<(BOAccountResponseDto? Account, string? Error)> OpenBOAccountAsync(OpenBOAccountDto dto);
    Task<(BOAccountResponseDto? Account, string? Error)> DepositCashAsync(DepositCashDto dto);
    Task<(BOAccountResponseDto? Account, string? Error)> SetMarginLimitAsync(SetMarginLimitDto dto);
    Task<(BOAccountResponseDto? Account, string? Error)> ToggleBOAccountAsync(int userId, bool activate);
    Task<List<BOAccountResponseDto>> GetBOAccountsAsync(int brokerageHouseId);
    Task<(OrderResponseDto? Order, string? Error)> SettleOrderAsync(int orderId);
}

public class CCDService : ICCDService
{
    private readonly AppDbContext _db;

    public CCDService(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────
    // OPEN BO ACCOUNT — CCD opens account for an investor
    // ─────────────────────────────────────────────────────────────────
    public async Task<(BOAccountResponseDto? Account, string? Error)> OpenBOAccountAsync(
        OpenBOAccountDto dto)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.IsActive);

        if (user == null)
            return (null, "User not found.");

        if (user.Role.Name != "Investor")
            return (null, "BO accounts can only be opened for Investors.");

        if (user.IsBOAccountActive)
            return (null, "Investor already has an active BO account.");

        // Check BO number is unique
        if (await _db.Users.AnyAsync(u => u.BONumber == dto.BONumber))
            return (null, $"BO Number '{dto.BONumber}' is already in use.");

        // Margin account must have margin limit set
        if (dto.AccountType == AccountType.Margin && dto.MarginLimit <= 0)
            return (null, "Margin accounts must have a MarginLimit greater than 0.");

        user.BONumber = dto.BONumber;
        user.AccountType = dto.AccountType;
        user.CashBalance = dto.InitialCashBalance;
        user.MarginLimit = dto.AccountType == AccountType.Margin ? dto.MarginLimit : 0;
        user.MarginUsed = 0;
        user.IsBOAccountActive = true;

        await _db.SaveChangesAsync();
        return (MapToDto(user), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // DEPOSIT CASH — CCD adds cash to investor account
    // ─────────────────────────────────────────────────────────────────
    public async Task<(BOAccountResponseDto? Account, string? Error)> DepositCashAsync(
        DepositCashDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.IsActive);

        if (user == null)
            return (null, "User not found.");

        if (!user.IsBOAccountActive)
            return (null, "BO account is not active.");

        user.CashBalance += dto.Amount;
        await _db.SaveChangesAsync();
        return (MapToDto(user), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // SET MARGIN LIMIT — CCD sets how much broker will lend
    // ─────────────────────────────────────────────────────────────────
    public async Task<(BOAccountResponseDto? Account, string? Error)> SetMarginLimitAsync(
        SetMarginLimitDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.IsActive);

        if (user == null)
            return (null, "User not found.");

        if (!user.IsBOAccountActive)
            return (null, "BO account is not active.");

        if (user.AccountType != AccountType.Margin)
            return (null, "Margin limit can only be set for Margin accounts.");

        // Cannot reduce margin below what's currently used
        if (dto.MarginLimit < user.MarginUsed)
            return (null, $"Cannot reduce margin limit below current margin in use ({user.MarginUsed:F2}).");

        user.MarginLimit = dto.MarginLimit;
        await _db.SaveChangesAsync();
        return (MapToDto(user), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // TOGGLE BO ACCOUNT — CCD activates or freezes account
    // ─────────────────────────────────────────────────────────────────
    public async Task<(BOAccountResponseDto? Account, string? Error)> ToggleBOAccountAsync(
        int userId, bool activate)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
            return (null, "User not found.");

        if (user.BONumber == null)
            return (null, "User does not have a BO account yet.");

        user.IsBOAccountActive = activate;
        await _db.SaveChangesAsync();
        return (MapToDto(user), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET ALL BO ACCOUNTS — CCD views all investor accounts
    // ─────────────────────────────────────────────────────────────────
    public async Task<List<BOAccountResponseDto>> GetBOAccountsAsync(int brokerageHouseId)
    {
        return await _db.Users
            .Include(u => u.Role)
            .Where(u =>
                u.BrokerageHouseId == brokerageHouseId &&
                u.Role.Name == "Investor" &&
                u.IsActive)
            .Select(u => new BOAccountResponseDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                BONumber = u.BONumber,
                AccountType = u.AccountType.ToString(),
                CashBalance = u.CashBalance,
                MarginLimit = u.MarginLimit,
                MarginUsed = u.MarginUsed,
                AvailableMargin = u.MarginLimit - u.MarginUsed,
                IsBOAccountActive = u.IsBOAccountActive
            })
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // SETTLE ORDER — CCD marks Executed order as Completed
    //               and updates investor portfolio
    // ─────────────────────────────────────────────────────────────────
    public async Task<(OrderResponseDto? Order, string? Error)> SettleOrderAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return (null, "Order not found.");

        if (order.Status != OrderStatus.Executed)
            return (null, $"Only Executed orders can be settled. Current status: {order.Status}.");

        var investor = await _db.Users.FindAsync(order.InvestorId);
        if (investor == null)
            return (null, "Investor not found.");

        decimal executionPrice = order.ExecutionPrice ?? order.PriceAtOrder;

        if (order.OrderType == OrderType.Buy)
        {
            // Update or create portfolio entry
            var portfolio = await _db.Portfolios
                .FirstOrDefaultAsync(p =>
                    p.InvestorId == order.InvestorId &&
                    p.StockId == order.StockId);

            if (portfolio == null)
            {
                // First time buying this stock
                portfolio = new Portfolio
                {
                    InvestorId = order.InvestorId,
                    StockId = order.StockId,
                    BrokerageHouseId = order.BrokerageHouseId,
                    Quantity = order.Quantity,
                    AverageBuyPrice = executionPrice,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _db.Portfolios.Add(portfolio);
            }
            else
            {
                // Already owns this stock — recalculate average
                // Formula: (oldQty × oldAvg + newQty × newPrice) ÷ (oldQty + newQty)
                decimal totalCost = (portfolio.Quantity * portfolio.AverageBuyPrice)
                                  + (order.Quantity * executionPrice);
                portfolio.Quantity += order.Quantity;
                portfolio.AverageBuyPrice = totalCost / portfolio.Quantity;
                portfolio.LastUpdatedAt = DateTime.UtcNow;
            }
        }
        else // Sell order
        {
            var portfolio = await _db.Portfolios
                .FirstOrDefaultAsync(p =>
                    p.InvestorId == order.InvestorId &&
                    p.StockId == order.StockId);

            if (portfolio != null)
            {
                portfolio.Quantity -= order.Quantity;
                portfolio.LastUpdatedAt = DateTime.UtcNow;

                // Add cash proceeds to investor account
                investor.CashBalance += executionPrice * order.Quantity;
            }
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (new OrderResponseDto
        {
            Id = order.Id,
            InvestorId = order.InvestorId,
            InvestorName = order.Investor.FullName,
            StockId = order.StockId,
            TradingCode = order.Stock.TradingCode,
            CompanyName = order.Stock.CompanyName,
            Exchange = order.Stock.Exchange,
            OrderType = order.OrderType,
            OrderCategory = order.OrderCategory,
            Quantity = order.Quantity,
            PriceAtOrder = order.PriceAtOrder,
            ExecutionPrice = order.ExecutionPrice,
            SettlementType = order.SettlementType,
            PlacedBy = order.PlacedBy,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            ExecutedAt = order.ExecutedAt,
            CompletedAt = order.CompletedAt,
            TotalValue = executionPrice * order.Quantity
        }, null);
    }

    private static BOAccountResponseDto MapToDto(User u) => new()
    {
        UserId = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        BONumber = u.BONumber,
        AccountType = u.AccountType?.ToString(),
        CashBalance = u.CashBalance,
        MarginLimit = u.MarginLimit,
        MarginUsed = u.MarginUsed,
        AvailableMargin = u.MarginLimit - u.MarginUsed,
        IsBOAccountActive = u.IsBOAccountActive
    };
}
