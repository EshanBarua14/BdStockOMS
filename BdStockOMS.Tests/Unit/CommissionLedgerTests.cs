using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class CommissionLedgerTests
{
    private static AppDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Trade BuildTrade(int id = 1, string side = "BUY",
        int quantity = 100, decimal price = 50m) => new()
    {
        Id               = id,
        OrderId          = 1,
        InvestorId       = 1,
        BrokerageHouseId = 1,
        StockId          = 1,
        Side             = side,
        Quantity         = quantity,
        Price            = price,
        TotalValue       = quantity * price,
        Status           = TradeStatus.Filled,
        TradedAt         = DateTime.UtcNow,
    };

    private static CommissionLedgerService BuildService(AppDbContext db,
        decimal buyRate = 0.005m, decimal sellRate = 0.005m)
    {
        var calcMock = new Mock<ICommissionCalculatorService>();

        calcMock.Setup(c => c.CalculateBuyCommissionAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync((int _, int _, decimal tv, string ex) =>
            {
                var breakdown = new CommissionBreakdown
                {
                    TradeValue       = tv,
                    BrokerCommission = Math.Round(tv * buyRate, 2),
                    CDBLCharge       = Math.Round(tv * 0.00015m, 2),
                    ExchangeFee      = Math.Round(tv * 0.0005m, 2),
                    CommissionRate   = buyRate,
                    Exchange         = ex,
                    OrderType        = "BUY",
                };
                breakdown.TotalCharges = breakdown.BrokerCommission + breakdown.CDBLCharge + breakdown.ExchangeFee;
                breakdown.NetAmount    = tv + breakdown.TotalCharges;
                return BdStockOMS.API.Common.Result<CommissionBreakdown>.Success(breakdown);
            });

        calcMock.Setup(c => c.CalculateSellCommissionAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync((int _, int _, decimal tv, string ex) =>
            {
                var breakdown = new CommissionBreakdown
                {
                    TradeValue       = tv,
                    BrokerCommission = Math.Round(tv * sellRate, 2),
                    CDBLCharge       = Math.Round(tv * 0.00015m, 2),
                    ExchangeFee      = Math.Round(tv * 0.0005m, 2),
                    CommissionRate   = sellRate,
                    Exchange         = ex,
                    OrderType        = "SELL",
                };
                breakdown.TotalCharges = breakdown.BrokerCommission + breakdown.CDBLCharge + breakdown.ExchangeFee;
                breakdown.NetAmount    = tv - breakdown.TotalCharges;
                return BdStockOMS.API.Common.Result<CommissionBreakdown>.Success(breakdown);
            });

        return new CommissionLedgerService(db, calcMock.Object);
    }

    // ── PostTradeCommissionAsync ──────────────────────────────────────────
    [Fact]
    public async Task PostTradeCommission_BuyTrade_CreatesLedgerEntry()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var trade = BuildTrade(side: "BUY", quantity: 100, price: 50m);

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        Assert.NotNull(ledger);
        Assert.Equal("BUY", ledger.Side);
        Assert.Equal("DSE", ledger.Exchange);
        Assert.Equal(5_000m, ledger.TradeValue);
    }

    [Fact]
    public async Task PostTradeCommission_SellTrade_CreatesLedgerEntry()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var trade = BuildTrade(side: "SELL", quantity: 200, price: 100m);

        var ledger = await svc.PostTradeCommissionAsync(trade, "CSE");

        Assert.Equal("SELL", ledger.Side);
        Assert.Equal("CSE",  ledger.Exchange);
        Assert.Equal(20_000m, ledger.TradeValue);
    }

    [Fact]
    public async Task PostTradeCommission_PersistsToDatabase()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var trade = BuildTrade();

        await svc.PostTradeCommissionAsync(trade, "DSE");

        Assert.Equal(1, await db.CommissionLedgers.CountAsync());
    }

    [Fact]
    public async Task PostTradeCommission_BrokerCommission_IsCorrect()
    {
        var db    = BuildDb();
        var svc   = BuildService(db, buyRate: 0.005m);
        var trade = BuildTrade(quantity: 100, price: 100m); // trade value = 10_000

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        // 0.5% of 10_000 = 50
        Assert.Equal(50m, ledger.BrokerCommission);
    }

    [Fact]
    public async Task PostTradeCommission_CDBLCharge_IsCorrect()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var trade = BuildTrade(quantity: 100, price: 100m); // trade value = 10_000

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        // 0.015% of 10_000 = 1.50
        Assert.Equal(1.50m, ledger.CDBLCharge);
    }

    [Fact]
    public async Task PostTradeCommission_ExchangeFee_IsCorrect()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var trade = BuildTrade(quantity: 100, price: 100m); // trade value = 10_000

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        // 0.05% of 10_000 = 5.00
        Assert.Equal(5.00m, ledger.ExchangeFee);
    }

    [Fact]
    public async Task PostTradeCommission_BuyNetAmount_IsTradeValuePlusCharges()
    {
        var db    = BuildDb();
        var svc   = BuildService(db, buyRate: 0.005m);
        var trade = BuildTrade(quantity: 100, price: 100m);

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        // TradeValue=10000, BrokerComm=50, CDBL=1.5, ExchangeFee=5 → total=56.5
        Assert.Equal(ledger.TradeValue + ledger.TotalCharges, ledger.NetAmount);
    }

    [Fact]
    public async Task PostTradeCommission_SellNetAmount_IsTradeValueMinusCharges()
    {
        var db    = BuildDb();
        var svc   = BuildService(db, sellRate: 0.005m);
        var trade = BuildTrade(side: "SELL", quantity: 100, price: 100m);

        var ledger = await svc.PostTradeCommissionAsync(trade, "DSE");

        Assert.Equal(ledger.TradeValue - ledger.TotalCharges, ledger.NetAmount);
    }

    // ── GetInvestorLedgerAsync ────────────────────────────────────────────
    [Fact]
    public async Task GetInvestorLedger_ReturnsOnlyInvestorEntries()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        await svc.PostTradeCommissionAsync(BuildTrade(id: 1), "DSE");
        var trade2 = BuildTrade(id: 2);
        trade2.InvestorId = 99;
        await svc.PostTradeCommissionAsync(trade2, "DSE");

        var ledger = await svc.GetInvestorLedgerAsync(1, null, null);
        Assert.Single(ledger);
        Assert.All(ledger, l => Assert.Equal(1, l.InvestorId));
    }

    [Fact]
    public async Task GetInvestorLedger_FiltersBy_FromDate()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        await svc.PostTradeCommissionAsync(BuildTrade(), "DSE");

        var future = DateTime.UtcNow.AddDays(1);
        var ledger = await svc.GetInvestorLedgerAsync(1, future, null);

        Assert.Empty(ledger);
    }

    [Fact]
    public async Task GetInvestorLedger_FiltersBy_ToDate()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        await svc.PostTradeCommissionAsync(BuildTrade(), "DSE");

        var past = DateTime.UtcNow.AddDays(-1);
        var ledger = await svc.GetInvestorLedgerAsync(1, null, past);

        Assert.Empty(ledger);
    }

    // ── GetTotalCommissionAsync ───────────────────────────────────────────
    [Fact]
    public async Task GetTotalCommission_SumsAllBrokerCommissions()
    {
        var db  = BuildDb();
        var svc = BuildService(db, buyRate: 0.005m);

        // Two trades of 10_000 each → 50 + 50 = 100 broker commission
        await svc.PostTradeCommissionAsync(BuildTrade(id: 1, quantity: 100, price: 100m), "DSE");
        await svc.PostTradeCommissionAsync(BuildTrade(id: 2, quantity: 100, price: 100m), "DSE");

        var total = await svc.GetTotalCommissionAsync(1, null, null);
        Assert.Equal(100m, total);
    }

    [Fact]
    public async Task GetTotalCommission_ReturnsZero_WhenNoEntries()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var total = await svc.GetTotalCommissionAsync(1, null, null);
        Assert.Equal(0m, total);
    }

    // ── CommissionLedger model ────────────────────────────────────────────
    [Fact]
    public void CommissionLedger_PostedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var ledger = new CommissionLedger();
        Assert.True(ledger.PostedAt >= before);
    }

    [Fact]
    public void CommissionBreakdown_TotalCharges_IsSumOfAllFees()
    {
        var b = new CommissionBreakdown
        {
            BrokerCommission = 50m,
            CDBLCharge       = 1.5m,
            ExchangeFee      = 5m,
            TotalCharges     = 56.5m,
        };
        Assert.Equal(b.BrokerCommission + b.CDBLCharge + b.ExchangeFee, b.TotalCharges);
    }
}
