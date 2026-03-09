using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class BrokerageSettingsService : IBrokerageSettingsService
    {
        private readonly AppDbContext _db;

        public BrokerageSettingsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<BrokerageSettings> GetOrCreateSettingsAsync(int brokerageHouseId)
        {
            var settings = await _db.BrokerageSettings
                .FirstOrDefaultAsync(s => s.BrokerageHouseId == brokerageHouseId);

            if (settings != null) return settings;

            // Auto-create with defaults
            settings = new BrokerageSettings
            {
                BrokerageHouseId = brokerageHouseId,
                CreatedAt        = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow
            };
            _db.BrokerageSettings.Add(settings);
            await _db.SaveChangesAsync();
            return settings;
        }

        public async Task<BrokerageSettings> UpdateSettingsAsync(int brokerageHouseId, UpdateSettingsRequest request)
        {
            var settings = await GetOrCreateSettingsAsync(brokerageHouseId);

            settings.MaxSingleOrderValue      = request.MaxSingleOrderValue;
            settings.MaxDailyTurnover         = request.MaxDailyTurnover;
            settings.MarginRatio              = request.MarginRatio;
            settings.MinCashBalance           = request.MinCashBalance;
            settings.IsMarginTradingEnabled   = request.IsMarginTradingEnabled;
            settings.IsShortSellingEnabled    = request.IsShortSellingEnabled;
            settings.IsSmsAlertEnabled        = request.IsSmsAlertEnabled;
            settings.IsEmailAlertEnabled      = request.IsEmailAlertEnabled;
            settings.IsAutoSettlementEnabled  = request.IsAutoSettlementEnabled;
            settings.IsTwoFactorRequired      = request.IsTwoFactorRequired;
            settings.TradingStartMinutes      = request.TradingStartMinutes;
            settings.TradingEndMinutes        = request.TradingEndMinutes;
            settings.UpdatedAt                = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return settings;
        }

        public async Task<bool> IsFeatureEnabledAsync(int brokerageHouseId, string featureName)
        {
            var settings = await GetOrCreateSettingsAsync(brokerageHouseId);

            return featureName switch
            {
                "MarginTrading"    => settings.IsMarginTradingEnabled,
                "ShortSelling"     => settings.IsShortSellingEnabled,
                "SmsAlert"         => settings.IsSmsAlertEnabled,
                "EmailAlert"       => settings.IsEmailAlertEnabled,
                "AutoSettlement"   => settings.IsAutoSettlementEnabled,
                "TwoFactor"        => settings.IsTwoFactorRequired,
                _ => false
            };
        }

        public async Task<bool> IsWithinTradingHoursAsync(int brokerageHouseId)
        {
            var settings = await GetOrCreateSettingsAsync(brokerageHouseId);
            var now = DateTime.UtcNow.AddHours(6); // BST = UTC+6
            var currentMinutes = now.Hour * 60 + now.Minute;
            return currentMinutes >= settings.TradingStartMinutes &&
                   currentMinutes <= settings.TradingEndMinutes;
        }

        public async Task<BranchOffice> CreateBranchAsync(CreateBranchRequest request)
        {
            var existing = await _db.BranchOffices
                .FirstOrDefaultAsync(b => b.BrokerageHouseId == request.BrokerageHouseId &&
                                          b.BranchCode == request.BranchCode);
            if (existing != null)
                throw new InvalidOperationException($"Branch code {request.BranchCode} already exists.");

            var branch = new BranchOffice
            {
                BrokerageHouseId = request.BrokerageHouseId,
                Name             = request.Name,
                BranchCode       = request.BranchCode,
                Address          = request.Address,
                Phone            = request.Phone,
                Email            = request.Email,
                ManagerName      = request.ManagerName,
                IsActive         = true,
                CreatedAt        = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow
            };

            _db.BranchOffices.Add(branch);
            await _db.SaveChangesAsync();
            return branch;
        }

        public async Task<BranchOffice> UpdateBranchAsync(int branchId, CreateBranchRequest request)
        {
            var branch = await _db.BranchOffices.FindAsync(branchId)
                         ?? throw new KeyNotFoundException($"Branch {branchId} not found.");

            branch.Name        = request.Name;
            branch.Address     = request.Address;
            branch.Phone       = request.Phone;
            branch.Email       = request.Email;
            branch.ManagerName = request.ManagerName;
            branch.UpdatedAt   = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return branch;
        }

        public async Task<bool> DeactivateBranchAsync(int branchId)
        {
            var branch = await _db.BranchOffices.FindAsync(branchId);
            if (branch == null) return false;

            branch.IsActive  = false;
            branch.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<BranchOffice>> GetBranchesAsync(int brokerageHouseId)
        {
            return await _db.BranchOffices
                .Where(b => b.BrokerageHouseId == brokerageHouseId)
                .OrderBy(b => b.BranchCode)
                .ToListAsync();
        }

        public async Task<BranchOffice?> GetBranchByIdAsync(int branchId)
        {
            return await _db.BranchOffices
                .Include(b => b.BrokerageHouse)
                .FirstOrDefaultAsync(b => b.Id == branchId);
        }
    }
}
