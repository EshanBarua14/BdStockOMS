using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

public enum OrderType { Buy, Sell }
public enum OrderCategory { Market, Limit, MarketAtBest }
public enum TimeInForce { Day, IOC, FOK }
public enum ExchangeId { DSE, CSE }
public enum Board { Public, SME, ATBPublic, Government, Debt, Block, BuyIn, SPublic }
public enum ExecInstruction { None, Suspend, Release, WholeOrNone }
public enum AggressorSide { None = 0, Unknown = 0, Buy = 1, Sell = -1 }
public enum OrderStatus
{
    Pending, Queued, Submitted, Waiting,
    Open, PartiallyFilled, Filled, Completed,
    CancelRequested, EditRequested,
    Cancelled, Deleted, Replaced, Private, Rejected
}
public enum SettlementType { T2, T0 }
public enum PlacedByRole { Investor, Trader }

public class Order
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public int? TraderId { get; set; }
    public int StockId { get; set; }
    public int BrokerageHouseId { get; set; }
    public OrderType OrderType { get; set; }
    public OrderCategory OrderCategory { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? ExecutionPrice { get; set; }
    public SettlementType SettlementType { get; set; }
    public PlacedByRole PlacedBy { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(500)] public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
    public ExchangeId ExchangeId { get; set; } = ExchangeId.DSE;
    public Board Board { get; set; } = Board.Public;
    public ExecInstruction ExecInstruction { get; set; } = ExecInstruction.None;
    public int? MinQty { get; set; }
    public int? DisplayQty { get; set; }
    public bool IsPrivate { get; set; } = false;
    public int ExecutedQuantity { get; set; } = 0;
    public decimal? GrossTradeAmt { get; set; }
    public AggressorSide AggressorIndicator { get; set; } = AggressorSide.None;
    [MaxLength(50)] public string? ClOrdID { get; set; }
    [MaxLength(50)] public string? OrigClOrdID { get; set; }
    [MaxLength(50)] public string? TrdMatchID { get; set; }
    [MaxLength(8)]  public string? SettlDate { get; set; }
    public double FillProbability { get; set; } = 0.85;
    public double SlippagePercent { get; set; } = 0.001;
    [ForeignKey("InvestorId")]       public virtual User Investor { get; set; } = null!;
    [ForeignKey("TraderId")]         public virtual User? Trader { get; set; }
    [ForeignKey("StockId")]          public virtual Stock Stock { get; set; } = null!;
    [ForeignKey("BrokerageHouseId")] public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
