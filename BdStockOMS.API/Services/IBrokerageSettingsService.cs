using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class UpdateSettingsRequest
    {
        public decimal MaxSingleOrderValue { get; set; }
        public decimal MaxDailyTurnover { get; set; }
        public decimal MarginRatio { get; set; }
        public decimal MinCashBalance { get; set; }
        public bool IsMarginTradingEnabled { get; set; }
        public bool IsShortSellingEnabled { get; set; }
        public bool IsSmsAlertEnabled { get; set; }
        public bool IsEmailAlertEnabled { get; set; }
        public bool IsAutoSettlementEnabled { get; set; }
        public bool IsTwoFactorRequired { get; set; }
        public int TradingStartMinutes { get; set; }
        public int TradingEndMinutes { get; set; }
    }

    public class CreateBranchRequest
    {
        public int BrokerageHouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ManagerName { get; set; }
    }

    public interface IBrokerageSettingsService
    {
        Task<BrokerageSettings> GetOrCreateSettingsAsync(int brokerageHouseId);
        Task<BrokerageSettings> UpdateSettingsAsync(int brokerageHouseId, UpdateSettingsRequest request);
        Task<bool> IsFeatureEnabledAsync(int brokerageHouseId, string featureName);
        Task<bool> IsWithinTradingHoursAsync(int brokerageHouseId);
        Task<BranchOffice> CreateBranchAsync(CreateBranchRequest request);
        Task<BranchOffice> UpdateBranchAsync(int branchId, CreateBranchRequest request);
        Task<bool> DeactivateBranchAsync(int branchId);
        Task<IEnumerable<BranchOffice>> GetBranchesAsync(int brokerageHouseId);
        Task<BranchOffice?> GetBranchByIdAsync(int branchId);
    }
}
