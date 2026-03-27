using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services;

public record BrokerageHouseDto(
    int     Id,
    string  Name,
    string  LicenseNumber,
    string  Email,
    string  Phone,
    string  Address,
    bool    IsActive,
    DateTime CreatedAt,
    int     BranchCount,
    int     UserCount
);

public record CreateBrokerageHouseDto(
    string Name,
    string LicenseNumber,
    string Email,
    string Phone,
    string Address
);

public record UpdateBrokerageHouseDto(
    string? Name,
    string? Email,
    string? Phone,
    string? Address,
    bool?   IsActive
);

public record BranchOfficeDto(
    int      Id,
    int      BrokerageHouseId,
    string   BrokerageHouseName,
    string   Name,
    string   BranchCode,
    string   Address,
    string?  Phone,
    string?  Email,
    string?  ManagerName,
    bool     IsActive,
    DateTime CreatedAt,
    int      UserCount
);

public record CreateBranchOfficeDto(
    int    BrokerageHouseId,
    string Name,
    string BranchCode,
    string Address,
    string? Phone,
    string? Email,
    string? ManagerName
);

public record UpdateBranchOfficeDto(
    string? Name,
    string? Address,
    string? Phone,
    string? Email,
    string? ManagerName,
    bool?   IsActive
);

public record BOAccountListDto(
    int     UserId,
    string  FullName,
    string  Email,
    string? BONumber,
    string? AccountType,
    decimal CashBalance,
    decimal MarginLimit,
    decimal MarginUsed,
    decimal AvailableMargin,
    bool    IsActive,
    bool    IsBOAccountActive,
    int     BrokerageHouseId,
    string  BrokerageHouseName
);

public record UpdateBOAccountDto(
    string? FullName,
    string? Phone,
    bool?   IsActive,
    bool?   IsBOAccountActive,
    decimal? MarginLimit
);

public interface IBrokerManagementService
{
    // Brokerage House
    Task<IEnumerable<BrokerageHouseDto>> GetAllBrokeragesAsync();
    Task<BrokerageHouseDto?>             GetBrokerageByIdAsync(int id);
    Task<BrokerageHouseDto>              CreateBrokerageAsync(CreateBrokerageHouseDto dto);
    Task<BrokerageHouseDto?>             UpdateBrokerageAsync(int id, UpdateBrokerageHouseDto dto);
    Task<bool>                           ToggleBrokerageAsync(int id, bool active);

    // Branch Office
    Task<IEnumerable<BranchOfficeDto>>   GetAllBranchesAsync(int? brokerageHouseId = null);
    Task<BranchOfficeDto?>               GetBranchByIdAsync(int id);
    Task<BranchOfficeDto>                CreateBranchAsync(CreateBranchOfficeDto dto);
    Task<BranchOfficeDto?>               UpdateBranchAsync(int id, UpdateBranchOfficeDto dto);
    Task<bool>                           ToggleBranchAsync(int id, bool active);

    // BO Accounts
    Task<IEnumerable<BOAccountListDto>>  GetAllBOAccountsAsync(int? brokerageHouseId = null);
    Task<BOAccountListDto?>              GetBOAccountByUserIdAsync(int userId);
    Task<BOAccountListDto?>              UpdateBOAccountAsync(int userId, UpdateBOAccountDto dto);
}
