using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.FIX;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.FIX;

public class SimulatedFIXConnector : IFIXConnector
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulatedFIXConnector> _logger;
    private readonly string _exchange;
    private readonly Random _rng = new();
    private int _seqNum = 1;

    public string ConnectorName  => $"SimulatedFIX-{_exchange}";
    public bool   IsSimulated    => true;
    public FIXSessionState SessionState { get; private set; } = FIXSessionState.Disconnected;

    public event EventHandler<FIXExecutionReport>? ExecutionReportReceived;

    public SimulatedFIXConnector(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulatedFIXConnector> logger,
        string exchange = "DSE")
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _exchange     = exchange;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        SessionState = FIXSessionState.Connecting;
        await Task.Delay(100, ct);
        SessionState = FIXSessionState.Active;
        _logger.LogInformation("{Name}: Connected (simulated)", ConnectorName);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        SessionState = FIXSessionState.Disconnected;
        await Task.CompletedTask;
        _logger.LogInformation("{Name}: Disconnected", ConnectorName);
    }

    public async Task<FIXSendResult> SendNewOrderAsync(FIXOrderRequest req, CancellationToken ct = default)
    {
        var raw = BuildNewOrderSingle(req);
        await LogMessageAsync(req.BrokerageHouseId, "D", "OUT", req.ClOrdID, req.Symbol, null, raw);

        // Simulate acceptance
        await Task.Delay(50, ct);

        var execReport = new FIXExecutionReport
        {
            ClOrdID      = req.ClOrdID,
            OrderId      = $"ORD{_rng.Next(100000, 999999)}",
            ExecId       = $"EXEC{_seqNum++}",
            ExecType     = "0",  // New
            OrdStatus    = "0",  // New
            Symbol       = req.Symbol,
            OrderQty     = req.Quantity,
            CumQty       = 0,
            LeavesQty    = req.Quantity,
            AvgPx        = 0m,
            LastPx       = 0m,
            TrdMatchID   = string.Empty,
            TransactTime = DateTime.UtcNow,
        };

        await LogMessageAsync(req.BrokerageHouseId, "8", "IN", req.ClOrdID, req.Symbol, "0", raw);
        ExecutionReportReceived?.Invoke(this, execReport);

        _logger.LogInformation("{Name}: NewOrderSingle sent for {Symbol} ClOrdID={Id}", ConnectorName, req.Symbol, req.ClOrdID);

        return new FIXSendResult { Success = true, ClOrdID = req.ClOrdID, RawFIXMessage = raw,
            Message = "Order accepted by simulated exchange." };
    }

    public async Task<FIXSendResult> SendCancelAsync(string clOrdId, string origClOrdId,
        string symbol, CancellationToken ct = default)
    {
        var raw = $"8=FIX.4.4|35=F|11={clOrdId}|41={origClOrdId}|55={symbol}|10=000|";
        await LogMessageAsync(0, "F", "OUT", clOrdId, symbol, null, raw);
        await Task.Delay(30, ct);

        _logger.LogInformation("{Name}: CancelRequest sent {Id}", ConnectorName, clOrdId);
        return new FIXSendResult { Success = true, ClOrdID = clOrdId, RawFIXMessage = raw,
            Message = "Cancel request accepted." };
    }

    public async Task<FIXSendResult> SendAmendAsync(FIXOrderRequest req, CancellationToken ct = default)
    {
        var raw = BuildOrderCancelReplaceRequest(req);
        await LogMessageAsync(req.BrokerageHouseId, "G", "OUT", req.ClOrdID, req.Symbol, null, raw);
        await Task.Delay(50, ct);

        _logger.LogInformation("{Name}: AmendRequest sent {Id}", ConnectorName, req.ClOrdID);
        return new FIXSendResult { Success = true, ClOrdID = req.ClOrdID, RawFIXMessage = raw,
            Message = "Amend request accepted." };
    }

    public Task<string> GetSessionStatusAsync() => Task.FromResult(
        $"SimulatedFIX-{_exchange} | State={SessionState} | SeqNum={_seqNum}");

    private static string BuildNewOrderSingle(FIXOrderRequest req)
    {
        var side    = req.OrderType == Models.OrderType.Buy ? "1" : "2";
        var ordType = req.Category switch {
            Models.OrderCategory.Market       => "1",
            Models.OrderCategory.Limit        => "2",
            Models.OrderCategory.MarketAtBest => "P",
            _ => "2"
        };
        var tif = req.TimeInForce switch {
            Models.TimeInForce.Day => "0",
            Models.TimeInForce.IOC => "3",
            Models.TimeInForce.FOK => "4",
            _ => "0"
        };
        var msg = $"8=FIX.4.4|35=D|11={req.ClOrdID}|55={req.Symbol}|54={side}|" +
                  $"38={req.Quantity}|40={ordType}|59={tif}|";
        if (req.Price.HasValue) msg += $"44={req.Price.Value}|";
        if (req.MinQty.HasValue) msg += $"110={req.MinQty.Value}|";
        if (req.DisplayQty.HasValue) msg += $"1138={req.DisplayQty.Value}|";
        msg += "10=000|";
        return msg;
    }

    private static string BuildOrderCancelReplaceRequest(FIXOrderRequest req)
        => $"8=FIX.4.4|35=G|11={req.ClOrdID}|41={req.OrigClOrdID}|55={req.Symbol}|" +
           $"38={req.Quantity}|44={req.Price}|10=000|";

    private async Task LogMessageAsync(int brokerageHouseId, string msgType,
        string direction, string? clOrdId, string? symbol, string? status, string raw)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FIXMessageLogs.Add(new FIXMessageLog
            {
                BrokerageHouseId = brokerageHouseId,
                MsgType          = msgType,
                Direction        = direction,
                ClOrdID          = clOrdId,
                Symbol           = symbol,
                OrdStatus        = status,
                RawMessage       = raw,
                MsgSeqNum        = _seqNum,
                SentAt           = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log FIX message");
        }
    }
}
