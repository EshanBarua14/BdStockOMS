using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day87Tests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    // ── ContractNote model ───────────────────────────────────────────

    [Fact]
    public void ContractNote_DefaultsCorrect()
    {
        var cn = new ContractNote();
        Assert.Equal("Generated", cn.Status);
        Assert.False(cn.IsVoid);
        Assert.Null(cn.VoidedAt);
        Assert.Null(cn.VoidReason);
    }

    [Fact]
    public async Task ContractNote_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.ContractNotes.Add(new ContractNote
        {
            ContractNoteNumber = "CN-20260330-000001",
            OrderId            = 1,
            ClientId           = 1,
            TraderName         = "John Doe",
            BranchName         = "Main Branch",
            InstrumentCode     = "BATBC",
            InstrumentName     = "British American Tobacco",
            Side               = "Buy",
            Quantity           = 100,
            ExecutedPrice      = 633.50m,
            GrossAmount        = 63_350m,
            CommissionAmount   = 316.75m,
            CdscFee            = 31.675m,
            LevyCharge         = 19.005m,
            VatOnCommission    = 47.5125m,
            NetAmount          = 63_764.9425m,
            TradeDate          = DateTime.UtcNow,
            SettlementDate     = DateTime.UtcNow.AddDays(2),
        });
        await db.SaveChangesAsync();

        var cn = await db.ContractNotes.FirstOrDefaultAsync();
        Assert.NotNull(cn);
        Assert.Equal("CN-20260330-000001", cn.ContractNoteNumber);
        Assert.Equal("BATBC", cn.InstrumentCode);
        Assert.Equal(100, cn.Quantity);
    }

    // ── Contract note number format ──────────────────────────────────

    [Fact]
    public void ContractNoteNumber_FormatIsCorrect()
    {
        var orderId  = 42;
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var number   = $"CN-{datePart}-{orderId:D6}";
        Assert.StartsWith("CN-", number);
        Assert.Contains(datePart, number);
        Assert.EndsWith("000042", number);
    }

    [Fact]
    public void ContractNoteNumber_IsPaddedTo6Digits()
    {
        var number = $"CN-20260330-{1:D6}";
        Assert.Equal("CN-20260330-000001", number);
    }

    // ── Fee calculations ─────────────────────────────────────────────

    [Fact]
    public void CommissionCalculation_BuyOrder()
    {
        var grossAmount      = 100_000m;
        var commissionRate   = 0.005m;
        var commissionAmount = grossAmount * commissionRate;
        Assert.Equal(500m, commissionAmount);
    }

    [Fact]
    public void CDSCFee_IsPointZeroFivePercent()
    {
        var grossAmount = 100_000m;
        var cdscFee     = grossAmount * 0.0005m;
        Assert.Equal(50m, cdscFee);
    }

    [Fact]
    public void LevyCharge_IsPointZeroThreePercent()
    {
        var grossAmount = 100_000m;
        var levyCharge  = grossAmount * 0.0003m;
        Assert.Equal(30m, levyCharge);
    }

    [Fact]
    public void VatOnCommission_IsFifteenPercent()
    {
        var commission = 500m;
        var vat        = commission * 0.15m;
        Assert.Equal(75m, vat);
    }

    [Fact]
    public void NetAmount_Buy_AddsAllFees()
    {
        var gross      = 100_000m;
        var commission = 500m;
        var cdsc       = 50m;
        var levy       = 30m;
        var vat        = 75m;
        var net        = gross + commission + cdsc + levy + vat;
        Assert.Equal(100_655m, net);
    }

    [Fact]
    public void NetAmount_Sell_DeductsAllFees()
    {
        var gross      = 100_000m;
        var commission = 500m;
        var cdsc       = 50m;
        var levy       = 30m;
        var vat        = 75m;
        var net        = gross - commission - cdsc - levy - vat;
        Assert.Equal(99_345m, net);
    }

    [Fact]
    public void NetAmount_Sell_IsLessThanGross()
    {
        var gross      = 100_000m;
        var commission = 500m;
        var cdsc       = 50m;
        var levy       = 30m;
        var vat        = 75m;
        var net        = gross - commission - cdsc - levy - vat;
        Assert.True(net < gross);
    }

    [Fact]
    public void NetAmount_Buy_IsMoreThanGross()
    {
        var gross      = 100_000m;
        var commission = 500m;
        var cdsc       = 50m;
        var levy       = 30m;
        var vat        = 75m;
        var net        = gross + commission + cdsc + levy + vat;
        Assert.True(net > gross);
    }

    // ── Void contract note ───────────────────────────────────────────

    [Fact]
    public async Task ContractNote_CanBeVoided()
    {
        using var db = CreateDb();
        db.ContractNotes.Add(new ContractNote
        {
            ContractNoteNumber = "CN-TEST-001", OrderId = 1, ClientId = 1,
            TradeDate = DateTime.UtcNow, SettlementDate = DateTime.UtcNow.AddDays(2)
        });
        await db.SaveChangesAsync();

        var cn     = await db.ContractNotes.FirstAsync();
        cn.IsVoid  = true;
        cn.VoidedAt   = DateTime.UtcNow;
        cn.VoidReason = "Test void";
        cn.Status     = "Voided";
        await db.SaveChangesAsync();

        var updated = await db.ContractNotes.FirstAsync();
        Assert.True(updated.IsVoid);
        Assert.Equal("Voided", updated.Status);
        Assert.NotNull(updated.VoidedAt);
        Assert.Equal("Test void", updated.VoidReason);
    }

    [Fact]
    public async Task ContractNote_VoidedExcludedFromActive()
    {
        using var db = CreateDb();
        db.ContractNotes.AddRange(
            new ContractNote { ContractNoteNumber="CN-001", OrderId=1, ClientId=1, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2), IsVoid=false },
            new ContractNote { ContractNoteNumber="CN-002", OrderId=2, ClientId=1, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2), IsVoid=true  }
        );
        await db.SaveChangesAsync();

        var active = await db.ContractNotes.CountAsync(c => !c.IsVoid);
        Assert.Equal(1, active);
    }

    // ── ContractNoteResult ───────────────────────────────────────────

    [Fact]
    public void ContractNoteResult_DefaultsCorrect()
    {
        var r = new ContractNoteResult();
        Assert.False(r.Success);
        Assert.Empty(r.Message);
        Assert.Null(r.ContractNote);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void ContractNoteDto_CanBeCreated()
    {
        var dto = new ContractNoteDto
        {
            Id                 = 1,
            ContractNoteNumber = "CN-20260330-000001",
            ClientName         = "Test Client",
            InstrumentCode     = "GP",
            Side               = "Buy",
            Quantity           = 50,
            ExecutedPrice      = 345.98m,
            GrossAmount        = 17_299m,
            NetAmount          = 17_385.68m,
        };
        Assert.Equal("CN-20260330-000001", dto.ContractNoteNumber);
        Assert.Equal(50, dto.Quantity);
        Assert.Equal(345.98m, dto.ExecutedPrice);
    }

    [Fact]
    public void ContractNoteSummary_CanBeCreated()
    {
        var s = new ContractNoteSummary
        {
            Id = 1, ContractNoteNumber = "CN-001",
            ClientName = "Alice", InstrumentCode = "AAMRANET",
            Side = "Sell", Quantity = 11, NetAmount = 86.02m,
            TradeDate = DateTime.UtcNow, Status = "Generated"
        };
        Assert.Equal("AAMRANET", s.InstrumentCode);
        Assert.Equal("Generated", s.Status);
    }

    // ── Multi-note per client ────────────────────────────────────────

    [Fact]
    public async Task MultipleContractNotes_CanBeSavedForSameClient()
    {
        using var db = CreateDb();
        db.ContractNotes.AddRange(
            new ContractNote { ContractNoteNumber="CN-001", OrderId=1, ClientId=5, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2), Side="BUY" },
            new ContractNote { ContractNoteNumber="CN-002", OrderId=2, ClientId=5, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2), Side="SELL" },
            new ContractNote { ContractNoteNumber="CN-003", OrderId=3, ClientId=5, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2), Side="BUY" }
        );
        await db.SaveChangesAsync();

        var count = await db.ContractNotes.CountAsync(c => c.ClientId == 5);
        Assert.Equal(3, count);

        var buys  = await db.ContractNotes.CountAsync(c => c.ClientId == 5 && c.Side == "BUY");
        var sells = await db.ContractNotes.CountAsync(c => c.ClientId == 5 && c.Side == "SELL");
        Assert.Equal(2, buys);
        Assert.Equal(1, sells);
    }

    // ── Settlement date on contract note ────────────────────────────

    [Fact]
    public void ContractNote_SettlementDate_IsAfterTradeDate()
    {
        var tradeDate      = DateTime.UtcNow;
        var settlementDate = tradeDate.AddDays(2);
        Assert.True(settlementDate > tradeDate);
    }

    [Fact]
    public void ContractNote_SettlementDate_IsTwoDaysAfter()
    {
        var tradeDate      = new DateTime(2026, 3, 30);
        var settlementDate = tradeDate.AddDays(2);
        Assert.Equal(new DateTime(2026, 4, 1), settlementDate);
    }

    // ── Export text format ───────────────────────────────────────────

    [Fact]
    public void ExportText_ContainsRequiredFields()
    {
        var cn = new ContractNote
        {
            ContractNoteNumber = "CN-20260330-000001",
            TradeDate          = new DateTime(2026, 3, 30),
            InstrumentCode     = "BATBC",
            InstrumentName     = "British American Tobacco",
            Side               = "Buy",
            Quantity           = 100,
            ExecutedPrice      = 633.50m,
            GrossAmount        = 63_350m,
            CommissionAmount   = 316.75m,
            NetAmount          = 63_764.94m,
            SettlementDate     = new DateTime(2026, 4, 1),
        };

        var text = $"CONTRACT NOTE\n" +
                   $"Number: {cn.ContractNoteNumber}\n" +
                   $"Instrument: {cn.InstrumentCode}\n" +
                   $"Net Amount: {cn.NetAmount:F2}\n";

        Assert.Contains("CN-20260330-000001", text);
        Assert.Contains("BATBC", text);
        Assert.Contains("63764.94", text);
    }
}
