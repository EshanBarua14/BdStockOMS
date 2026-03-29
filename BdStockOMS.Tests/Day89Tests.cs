using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests
{
    public class Day89CorporateActionTests
    {
        private AppDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        private CorporateActionService CreateSvc(AppDbContext db) => new(db);

        private Stock MakeStock(AppDbContext db, int id = 1)
        {
            var s = new Stock { Id = id, TradingCode = "BRAC" + id, CompanyName = "BRAC Bank " + id };
            db.Stocks.Add(s);
            return s;
        }

        private Portfolio MakeHolding(AppDbContext db, int investorId, int stockId, int qty, decimal avgPrice = 100m, int brokerageHouseId = 1)
        {
            var p = new Portfolio { InvestorId = investorId, StockId = stockId, Quantity = qty, AverageBuyPrice = avgPrice, BrokerageHouseId = brokerageHouseId };
            db.Portfolios.Add(p);
            return p;
        }

        private CorporateAction MakeAction(AppDbContext db, int stockId, CorporateActionType type, decimal value, bool processed = false)
        {
            var a = new CorporateAction { StockId = stockId, Type = type, Value = value, RecordDate = DateTime.UtcNow, IsProcessed = processed, CreatedAt = DateTime.UtcNow };
            db.CorporateActions.Add(a);
            return a;
        }

        // ── Create ───────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidDividend_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); await db.SaveChangesAsync();
            var dto = new CreateCorporateActionDto { StockId = 1, Type = "Dividend", Value = 5m, RecordDate = DateTime.UtcNow };
            var result = await svc.CreateAsync(dto);
            Assert.True(result.IsSuccess);
            Assert.Equal("Dividend", result.Value!.Type);
        }

        [Fact]
        public async Task Create_InvalidStock_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = new CreateCorporateActionDto { StockId = 999, Type = "Dividend", Value = 5m, RecordDate = DateTime.UtcNow };
            var result = await svc.CreateAsync(dto);
            Assert.False(result.IsSuccess);
            Assert.Contains("Stock not found", result.Error);
        }

        [Fact]
        public async Task Create_InvalidType_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); await db.SaveChangesAsync();
            var dto = new CreateCorporateActionDto { StockId = 1, Type = "INVALID", Value = 5m, RecordDate = DateTime.UtcNow };
            var result = await svc.CreateAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Create_ZeroValue_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); await db.SaveChangesAsync();
            var dto = new CreateCorporateActionDto { StockId = 1, Type = "Dividend", Value = 0m, RecordDate = DateTime.UtcNow };
            var result = await svc.CreateAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Create_IsProcessedFalseByDefault()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); await db.SaveChangesAsync();
            var dto = new CreateCorporateActionDto { StockId = 1, Type = "BonusShare", Value = 0.10m, RecordDate = DateTime.UtcNow };
            var result = await svc.CreateAsync(dto);
            Assert.False(result.Value!.IsProcessed);
        }

        // ── GetById ──────────────────────────────────────────────────

        [Fact]
        public async Task GetById_Exists_ReturnsDto()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var stock = MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 10m);
            await db.SaveChangesAsync();
            var result = await svc.GetByIdAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(action.Id, result.Value!.Id);
        }

        [Fact]
        public async Task GetById_NotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.GetByIdAsync(9999);
            Assert.False(result.IsSuccess);
        }

        // ── Update ───────────────────────────────────────────────────

        [Fact]
        public async Task Update_Unprocessed_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();
            var dto = new UpdateCorporateActionDto { Value = 8m, RecordDate = DateTime.UtcNow };
            var result = await svc.UpdateAsync(action.Id, dto);
            Assert.True(result.IsSuccess);
            Assert.Equal(8m, result.Value!.Value);
        }

        [Fact]
        public async Task Update_AlreadyProcessed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m, processed: true);
            await db.SaveChangesAsync();
            var dto = new UpdateCorporateActionDto { Value = 8m, RecordDate = DateTime.UtcNow };
            var result = await svc.UpdateAsync(action.Id, dto);
            Assert.False(result.IsSuccess);
        }

        // ── Delete ───────────────────────────────────────────────────

        [Fact]
        public async Task Delete_Unprocessed_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();
            var result = await svc.DeleteAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.False(await db.CorporateActions.AnyAsync(a => a.Id == action.Id));
        }

        [Fact]
        public async Task Delete_AlreadyProcessed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m, processed: true);
            await db.SaveChangesAsync();
            var result = await svc.DeleteAsync(action.Id);
            Assert.False(result.IsSuccess);
        }

        // ── MarkProcessed ────────────────────────────────────────────

        [Fact]
        public async Task MarkProcessed_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();
            var result = await svc.MarkProcessedAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.True((await db.CorporateActions.FindAsync(action.Id))!.IsProcessed);
        }

        [Fact]
        public async Task MarkProcessed_AlreadyProcessed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m, processed: true);
            await db.SaveChangesAsync();
            var result = await svc.MarkProcessedAsync(action.Id);
            Assert.False(result.IsSuccess);
        }

        // ── ProcessAsync — Dividend ───────────────────────────────────

        [Fact]
        public async Task Process_Dividend_CashAmountCorrect()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 200);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(1000m, result.Value!.TotalCashDistributed); // 200 * 5
            Assert.Equal(0, result.Value.TotalSharesAwarded);
        }

        [Fact]
        public async Task Process_Dividend_MultipleHolders_SumsCorrectly()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            MakeHolding(db, investorId: 2, stockId: 1, qty: 300);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 10m);
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(4000m, result.Value!.TotalCashDistributed); // (100+300)*10
            Assert.Equal(2, result.Value.AffectedHoldings);
        }

        [Fact]
        public async Task Process_Dividend_LedgerEntryType_IsDividendCash()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var ledger = await db.CorporateActionLedgers.FirstAsync();
            Assert.Equal(CorporateActionLedgerType.DividendCash, ledger.EntryType);
        }

        [Fact]
        public async Task Process_Dividend_PortfolioQuantityUnchanged()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var holding = MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var updated = await db.Portfolios.FindAsync(holding.Id);
            Assert.Equal(100, updated!.Quantity); // unchanged
        }

        // ── ProcessAsync — Bonus share ────────────────────────────────

        [Fact]
        public async Task Process_BonusShare_SharesAwardedCorrect()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 200);
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.10m); // 10% bonus
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(20, result.Value!.TotalSharesAwarded); // 200 * 0.10
            Assert.Equal(0m, result.Value.TotalCashDistributed);
        }

        [Fact]
        public async Task Process_BonusShare_PortfolioQuantityIncreased()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var holding = MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.20m); // 20% bonus
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var updated = await db.Portfolios.FindAsync(holding.Id);
            Assert.Equal(120, updated!.Quantity); // 100 + 20
        }

        [Fact]
        public async Task Process_BonusShare_LedgerEntryType_IsBonusShareCredit()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.10m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var ledger = await db.CorporateActionLedgers.FirstAsync();
            Assert.Equal(CorporateActionLedgerType.BonusShareCredit, ledger.EntryType);
        }

        [Fact]
        public async Task Process_BonusShare_FractionalFlooredToInt()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 3); // 3 * 0.10 = 0.3 -> floors to 0
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.10m);
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.Equal(0, result.Value!.TotalSharesAwarded);
        }

        [Fact]
        public async Task Process_BonusShare_MultipleHolders_AllGet_Shares()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            MakeHolding(db, investorId: 2, stockId: 1, qty: 200);
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.10m);
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.Equal(30, result.Value!.TotalSharesAwarded); // 10+20
            Assert.Equal(2, result.Value.AffectedHoldings);
        }

        // ── ProcessAsync — Rights ─────────────────────────────────────

        [Fact]
        public async Task Process_RightShare_EntitlementCorrect()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 400);
            var action = MakeAction(db, 1, CorporateActionType.RightShare, 0.25m); // 1:4
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(100, result.Value!.TotalSharesAwarded); // 400 * 0.25
        }

        [Fact]
        public async Task Process_RightShare_LedgerEntryType_IsRightsEntitlement()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.RightShare, 0.25m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var ledger = await db.CorporateActionLedgers.FirstAsync();
            Assert.Equal(CorporateActionLedgerType.RightsEntitlement, ledger.EntryType);
        }

        // ── ProcessAsync — guard conditions ──────────────────────────

        [Fact]
        public async Task Process_NotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.ProcessAsync(9999);
            Assert.False(result.IsSuccess);
            Assert.Contains("not found", result.Error);
        }

        [Fact]
        public async Task Process_AlreadyProcessed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m, processed: true);
            await db.SaveChangesAsync();
            var result = await svc.ProcessAsync(action.Id);
            Assert.False(result.IsSuccess);
            Assert.Contains("already processed", result.Error);
        }

        [Fact]
        public async Task Process_NoHoldings_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();
            var result = await svc.ProcessAsync(action.Id);
            Assert.False(result.IsSuccess);
            Assert.Contains("No holdings", result.Error);
        }

        [Fact]
        public async Task Process_SetsIsProcessedTrue()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var updated = await db.CorporateActions.FindAsync(action.Id);
            Assert.True(updated!.IsProcessed);
        }

        [Fact]
        public async Task Process_LedgerRowsPersisted()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            MakeHolding(db, investorId: 2, stockId: 1, qty: 200);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            Assert.Equal(2, await db.CorporateActionLedgers.CountAsync());
        }

        // ── GetLedger ─────────────────────────────────────────────────

        [Fact]
        public async Task GetLedger_ReturnsEntries()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();
            await svc.ProcessAsync(action.Id);

            var result = await svc.GetLedgerAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task GetLedger_EmptyForUnprocessed_ReturnsEmpty()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            await db.SaveChangesAsync();

            var result = await svc.GetLedgerAsync(action.Id);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        // ── GetAll / GetByStock filters ───────────────────────────────

        [Fact]
        public async Task GetAll_FilterByProcessed_ReturnsCorrectSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeAction(db, 1, CorporateActionType.Dividend, 5m, processed: false);
            MakeAction(db, 1, CorporateActionType.BonusShare, 0.1m, processed: true);
            await db.SaveChangesAsync();

            var result = await svc.GetAllAsync(null, false);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
            Assert.All(result.Value!, r => Assert.False(r.IsProcessed));
        }

        [Fact]
        public async Task GetAll_FilterByStockId_ReturnsCorrectSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); MakeStock(db, 2);
            MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            MakeAction(db, 2, CorporateActionType.Dividend, 3m);
            await db.SaveChangesAsync();

            var result = await svc.GetAllAsync(stockId: 1, null);
            Assert.Single(result.Value!);
            Assert.All(result.Value!, r => Assert.Equal(1, r.StockId));
        }

        [Fact]
        public async Task GetByStock_ReturnsOnlyThatStock()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1); MakeStock(db, 2);
            MakeAction(db, 1, CorporateActionType.Dividend, 5m);
            MakeAction(db, 2, CorporateActionType.BonusShare, 0.1m);
            await db.SaveChangesAsync();

            var result = await svc.GetByStockAsync(1);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!);
        }

        // ── Result DTO fields ─────────────────────────────────────────

        [Fact]
        public async Task Process_ResultDto_ActionTypeSet()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 1, stockId: 1, qty: 100);
            var action = MakeAction(db, 1, CorporateActionType.BonusShare, 0.10m);
            await db.SaveChangesAsync();

            var result = await svc.ProcessAsync(action.Id);
            Assert.Equal("BonusShare", result.Value!.ActionType);
        }

        [Fact]
        public async Task Process_LedgerEntry_HoldingQtyAtRecord_Correct()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeStock(db, 1);
            MakeHolding(db, investorId: 5, stockId: 1, qty: 750);
            var action = MakeAction(db, 1, CorporateActionType.Dividend, 2m);
            await db.SaveChangesAsync();

            await svc.ProcessAsync(action.Id);
            var ledger = await db.CorporateActionLedgers.FirstAsync();
            Assert.Equal(750, ledger.HoldingQtyAtRecord);
        }
    }
}
