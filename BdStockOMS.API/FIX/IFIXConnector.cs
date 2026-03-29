using BdStockOMS.API.Models;

namespace BdStockOMS.API.FIX;

public enum FIXSessionState { Disconnected, Connecting, Logon, Active, Logout }
public enum FIXMsgType { NewOrderSingle, OrderCancelRequest, OrderCancelReplaceRequest,
    ExecutionReport, OrderCancelReject, Heartbeat, Logon, Logout, Reject }

public class FIXOrderRequest
{
    public string  ClOrdID        { get; set; } = string.Empty;
    public string? OrigClOrdID    { get; set; }
    public int     StockId        { get; set; }
    public string  Symbol         { get; set; } = string.Empty;
    public OrderType OrderType    { get; set; }
    public OrderCategory Category { get; set; }
    public TimeInForce TimeInForce{ get; set; }
    public int     Quantity       { get; set; }
    public decimal? Price         { get; set; }
    public int     InvestorId     { get; set; }
    public int     BrokerageHouseId { get; set; }
    public ExchangeId Exchange    { get; set; }
    public Board   Board          { get; set; }
    public int?    MinQty         { get; set; }
    public int?    DisplayQty     { get; set; }
    public bool    IsPrivate      { get; set; }
}

public class FIXExecutionReport
{
    public string  ClOrdID        { get; set; } = string.Empty;
    public string? OrigClOrdID    { get; set; }
    public string  OrderId        { get; set; } = string.Empty;
    public string  ExecId         { get; set; } = string.Empty;
    public string  ExecType       { get; set; } = string.Empty;
    public string  OrdStatus      { get; set; } = string.Empty;
    public string  Symbol         { get; set; } = string.Empty;
    public int     OrderQty       { get; set; }
    public int     CumQty         { get; set; }
    public int     LeavesQty      { get; set; }
    public decimal AvgPx          { get; set; }
    public decimal LastPx         { get; set; }
    public string? Text            { get; set; }
    public string  TrdMatchID     { get; set; } = string.Empty;
    public DateTime TransactTime  { get; set; }
}

public class FIXSendResult
{
    public bool    Success         { get; set; }
    public string  Message         { get; set; } = string.Empty;
    public string? ClOrdID         { get; set; }
    public string? RawFIXMessage   { get; set; }
}

public interface IFIXConnector
{
    string ConnectorName { get; }
    bool IsSimulated     { get; }
    FIXSessionState SessionState { get; }

    Task<FIXSendResult> SendNewOrderAsync(FIXOrderRequest request, CancellationToken ct = default);
    Task<FIXSendResult> SendCancelAsync(string clOrdId, string origClOrdId, string symbol, CancellationToken ct = default);
    Task<FIXSendResult> SendAmendAsync(FIXOrderRequest request, CancellationToken ct = default);
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<string> GetSessionStatusAsync();

    event EventHandler<FIXExecutionReport>? ExecutionReportReceived;
}
