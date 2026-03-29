using BdStockOMS.API.FIX;
using Microsoft.Extensions.DependencyInjection;
using BdStockOMS.API.Models;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day84Tests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    // ── FIXSessionState enum ─────────────────────────────────────────

    [Fact]
    public void FIXSessionState_HasAllValues()
    {
        Assert.True(Enum.IsDefined(typeof(FIXSessionState), FIXSessionState.Disconnected));
        Assert.True(Enum.IsDefined(typeof(FIXSessionState), FIXSessionState.Connecting));
        Assert.True(Enum.IsDefined(typeof(FIXSessionState), FIXSessionState.Logon));
        Assert.True(Enum.IsDefined(typeof(FIXSessionState), FIXSessionState.Active));
        Assert.True(Enum.IsDefined(typeof(FIXSessionState), FIXSessionState.Logout));
    }

    [Fact]
    public void FIXMsgType_HasAllValues()
    {
        Assert.True(Enum.IsDefined(typeof(FIXMsgType), FIXMsgType.NewOrderSingle));
        Assert.True(Enum.IsDefined(typeof(FIXMsgType), FIXMsgType.OrderCancelRequest));
        Assert.True(Enum.IsDefined(typeof(FIXMsgType), FIXMsgType.ExecutionReport));
        Assert.True(Enum.IsDefined(typeof(FIXMsgType), FIXMsgType.Heartbeat));
        Assert.True(Enum.IsDefined(typeof(FIXMsgType), FIXMsgType.Logon));
    }

    // ── FIXOrderRequest ───────────────────────────────────────────────

    [Fact]
    public void FIXOrderRequest_CanBeCreated()
    {
        var req = new FIXOrderRequest
        {
            ClOrdID    = "ORD001",
            Symbol     = "BATBC",
            OrderType  = OrderType.Buy,
            Category   = OrderCategory.Limit,
            TimeInForce = TimeInForce.Day,
            Quantity   = 100,
            Price      = 633.50m,
            Exchange   = ExchangeId.DSE,
            Board      = Board.Public,
        };
        Assert.Equal("ORD001",  req.ClOrdID);
        Assert.Equal("BATBC",   req.Symbol);
        Assert.Equal(100,       req.Quantity);
        Assert.Equal(633.50m,   req.Price);
    }

    [Fact]
    public void FIXOrderRequest_IcebergFields()
    {
        var req = new FIXOrderRequest { MinQty = 50, DisplayQty = 100, IsPrivate = true };
        Assert.Equal(50,   req.MinQty);
        Assert.Equal(100,  req.DisplayQty);
        Assert.True(req.IsPrivate);
    }

    // ── FIXExecutionReport ────────────────────────────────────────────

    [Fact]
    public void FIXExecutionReport_CanBeCreated()
    {
        var rpt = new FIXExecutionReport
        {
            ClOrdID      = "ORD001",
            OrderId      = "EXCH123",
            ExecId       = "EXEC001",
            ExecType     = "0",
            OrdStatus    = "0",
            Symbol       = "GP",
            OrderQty     = 50,
            CumQty       = 0,
            LeavesQty    = 50,
            AvgPx        = 0m,
            LastPx       = 0m,
            TrdMatchID   = string.Empty,
            TransactTime = DateTime.UtcNow,
        };
        Assert.Equal("ORD001",  rpt.ClOrdID);
        Assert.Equal("0",       rpt.ExecType);
        Assert.Equal(50,        rpt.LeavesQty);
    }

    // ── FIXSendResult ─────────────────────────────────────────────────

    [Fact]
    public void FIXSendResult_SuccessCase()
    {
        var r = new FIXSendResult { Success = true, ClOrdID = "ORD001", Message = "OK" };
        Assert.True(r.Success);
        Assert.Equal("ORD001", r.ClOrdID);
    }

    [Fact]
    public void FIXSendResult_FailureCase()
    {
        var r = new FIXSendResult { Success = false, Message = "Order rejected" };
        Assert.False(r.Success);
        Assert.Null(r.ClOrdID);
    }

    // ── SimulatedFIXConnector ────────────────────────────────────────

    [Fact]
    public void SimulatedFIXConnector_IsSimulatedTrue()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "DSE");
        Assert.True(connector.IsSimulated);
        Assert.Equal("SimulatedFIX-DSE", connector.ConnectorName);
        Assert.Equal(FIXSessionState.Disconnected, connector.SessionState);
    }

    [Fact]
    public async Task SimulatedFIXConnector_ConnectSetsActive()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "DSE");
        await connector.ConnectAsync();
        Assert.Equal(FIXSessionState.Active, connector.SessionState);
    }

    [Fact]
    public async Task SimulatedFIXConnector_SendNewOrder_ReturnsSuccess()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "DSE");
        await connector.ConnectAsync();

        var req = new FIXOrderRequest
        {
            ClOrdID = "TEST001", Symbol = "BATBC",
            OrderType = OrderType.Buy, Category = OrderCategory.Limit,
            TimeInForce = TimeInForce.Day, Quantity = 10, Price = 100m,
            BrokerageHouseId = 0, Exchange = ExchangeId.DSE, Board = Board.Public
        };
        var result = await connector.SendNewOrderAsync(req);
        Assert.True(result.Success);
        Assert.Equal("TEST001", result.ClOrdID);
        Assert.NotEmpty(result.RawFIXMessage!);
    }

    [Fact]
    public async Task SimulatedFIXConnector_SendCancel_ReturnsSuccess()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "CSE");
        await connector.ConnectAsync();

        var result = await connector.SendCancelAsync("CXL001", "ORD001", "GP");
        Assert.True(result.Success);
        Assert.Equal("CXL001", result.ClOrdID);
    }

    [Fact]
    public async Task SimulatedFIXConnector_Disconnect_SetsDisconnected()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "DSE");
        await connector.ConnectAsync();
        await connector.DisconnectAsync();
        Assert.Equal(FIXSessionState.Disconnected, connector.SessionState);
    }

    [Fact]
    public async Task SimulatedFIXConnector_FiresExecutionReport()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        var scopeFactory = new FakeScopeFactory(db);
        var connector = new SimulatedFIXConnector(scopeFactory, logger, "DSE");
        await connector.ConnectAsync();

        FIXExecutionReport? received = null;
        connector.ExecutionReportReceived += (_, rpt) => received = rpt;

        var req = new FIXOrderRequest
        {
            ClOrdID = "EVT001", Symbol = "GP",
            OrderType = OrderType.Sell, Category = OrderCategory.Market,
            TimeInForce = TimeInForce.IOC, Quantity = 5,
            BrokerageHouseId = 0, Exchange = ExchangeId.DSE, Board = Board.Public
        };
        await connector.SendNewOrderAsync(req);
        Assert.NotNull(received);
        Assert.Equal("EVT001", received!.ClOrdID);
    }

    // ── FIXMessageLog model ───────────────────────────────────────────

    [Fact]
    public async Task FIXMessageLog_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.FIXMessageLogs.Add(new FIXMessageLog
        {
            BrokerageHouseId = 1,
            MsgType   = "D",
            Direction = "OUT",
            ClOrdID   = "ORD001",
            Symbol    = "BATBC",
            RawMessage = "8=FIX.4.4|35=D|11=ORD001|",
            MsgSeqNum = 1,
        });
        await db.SaveChangesAsync();

        var msg = await db.FIXMessageLogs.FirstOrDefaultAsync();
        Assert.NotNull(msg);
        Assert.Equal("D",     msg.MsgType);
        Assert.Equal("OUT",   msg.Direction);
        Assert.Equal("ORD001",msg.ClOrdID);
    }

    [Fact]
    public async Task FIXMessageLog_MultipleMessages()
    {
        using var db = CreateDb();
        db.FIXMessageLogs.AddRange(
            new FIXMessageLog { BrokerageHouseId=1, MsgType="D",  Direction="OUT", RawMessage="msg1", MsgSeqNum=1 },
            new FIXMessageLog { BrokerageHouseId=1, MsgType="8",  Direction="IN",  RawMessage="msg2", MsgSeqNum=2 },
            new FIXMessageLog { BrokerageHouseId=1, MsgType="F",  Direction="OUT", RawMessage="msg3", MsgSeqNum=3 }
        );
        await db.SaveChangesAsync();

        var outbound = await db.FIXMessageLogs.CountAsync(m => m.Direction == "OUT");
        var inbound  = await db.FIXMessageLogs.CountAsync(m => m.Direction == "IN");
        Assert.Equal(2, outbound);
        Assert.Equal(1, inbound);
    }

    // ── FIX message format ────────────────────────────────────────────

    [Fact]
    public void FIXRawMessage_ContainsSOH()
    {
        var raw = "8=FIX.4.4|35=D|11=ORD001|10=000|";
        var withSOH = raw.Replace("|", "");
        Assert.Contains("", withSOH);
        Assert.StartsWith("8=FIX.4.4", withSOH);
    }

    [Fact]
    public void FIXMsgType_NewOrderSingle_IsD()
    {
        // FIX 4.4 standard
        Assert.Equal("D", GetFixMsgTypeTag(FIXMsgType.NewOrderSingle));
        Assert.Equal("F", GetFixMsgTypeTag(FIXMsgType.OrderCancelRequest));
        Assert.Equal("G", GetFixMsgTypeTag(FIXMsgType.OrderCancelReplaceRequest));
        Assert.Equal("8", GetFixMsgTypeTag(FIXMsgType.ExecutionReport));
    }

    private static string GetFixMsgTypeTag(FIXMsgType t) => t switch
    {
        FIXMsgType.NewOrderSingle            => "D",
        FIXMsgType.OrderCancelRequest        => "F",
        FIXMsgType.OrderCancelReplaceRequest => "G",
        FIXMsgType.ExecutionReport           => "8",
        FIXMsgType.Heartbeat                 => "0",
        FIXMsgType.Logon                     => "A",
        FIXMsgType.Logout                    => "5",
        FIXMsgType.Reject                    => "3",
        _ => "?"
    };
}

// ── Test helpers ──────────────────────────────────────────────────────

public class FakeScopeFactory : IServiceScopeFactory
{
    private readonly AppDbContext _db;
    public FakeScopeFactory(AppDbContext db) => _db = db;
    public IServiceScope CreateScope() => new FakeScope(_db);
}

public class FakeScope : IServiceScope
{
    public IServiceProvider ServiceProvider { get; }
    public FakeScope(AppDbContext db) => ServiceProvider = new FakeServiceProvider(db);
    public void Dispose() { }
}

public class FakeServiceProvider : IServiceProvider
{
    private readonly AppDbContext _db;
    public FakeServiceProvider(AppDbContext db) => _db = db;
    public object? GetService(Type serviceType)
        => serviceType == typeof(AppDbContext) ? _db : null;
}
