namespace BdStockOMS.API.Exchange;

public record MarketTickDto(
    string   TradingCode,
    decimal  LastTradePrice,
    decimal  Change,
    decimal  ChangePercent,
    long     Volume,
    DateTime Timestamp);

public record MarketDepthDto(
    string                TradingCode,
    List<DepthLevelDto>   Bids,
    List<DepthLevelDto>   Asks);

public record DepthLevelDto(decimal Price, long Quantity);

public record OhlcDto(
    DateTime Date,
    decimal  Open,
    decimal  High,
    decimal  Low,
    decimal  Close,
    long     Volume);

public record ExchangeOrderRequest(
    string  ExchangeOrderId,
    string  TradingCode,
    string  Side,
    string  OrderType,
    decimal Price,
    int     Quantity,
    int     BrokerageHouseId);

public record ExchangeOrderResult(
    bool    Success,
    string  ExchangeOrderId,
    string  Status,
    string? ErrorMessage = null);

public record ExchangeOrderStatus(
    string  ExchangeOrderId,
    string  Status,
    int     FilledQuantity,
    decimal AveragePrice);

public interface IExchangeConnector
{
    string ExchangeCode { get; }
    bool   IsConnected  { get; }

    Task<bool>                ConnectAsync();
    Task                      DisconnectAsync();
    Task<MarketTickDto>        GetLatestPriceAsync(string tradingCode);
    Task<MarketDepthDto>       GetMarketDepthAsync(string tradingCode);
    Task<List<OhlcDto>>        GetHistoricalDataAsync(string tradingCode, DateTime from, DateTime to);
    Task<ExchangeOrderResult>  SendOrderAsync(ExchangeOrderRequest order);
    Task<ExchangeOrderResult>  CancelOrderAsync(string exchangeOrderId);
    Task<ExchangeOrderStatus>  GetOrderStatusAsync(string exchangeOrderId);
}
