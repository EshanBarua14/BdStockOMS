using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services;

public static class ExchangeOrderRouter
{
    public static ExchangeRouteResult Route(ExchangeId exchange, Board board, OrderCategory category)
    {
        // Block board: Limit orders only, no Market or MarketAtBest
        if (board == Board.Block)
        {
            if (category == OrderCategory.Market || category == OrderCategory.MarketAtBest)
                return ExchangeRouteResult.Rejected("Block board only accepts Limit orders.");
            return ExchangeRouteResult.Accepted(exchange, "Block board — Limit order accepted.");
        }

        // BuyIn board: Limit orders only
        if (board == Board.BuyIn)
        {
            if (category != OrderCategory.Limit)
                return ExchangeRouteResult.Rejected("BuyIn board only accepts Limit orders.");
            return ExchangeRouteResult.Accepted(exchange, "BuyIn board — Limit order accepted.");
        }

        // SPublic board: Limit orders only
        if (board == Board.SPublic)
        {
            if (category != OrderCategory.Limit)
                return ExchangeRouteResult.Rejected("SPublic board only accepts Limit orders.");
            return ExchangeRouteResult.Accepted(exchange, "SPublic board — Limit order accepted.");
        }

        // Government/Debt board: DSE only
        if (board == Board.Government || board == Board.Debt)
        {
            if (exchange == ExchangeId.CSE)
                return ExchangeRouteResult.Rejected($"{board} board is only available on DSE.");
            return ExchangeRouteResult.Accepted(exchange, $"{board} board — DSE order accepted.");
        }

        // SME board: DSE only
        if (board == Board.SME)
        {
            if (exchange == ExchangeId.CSE)
                return ExchangeRouteResult.Rejected("SME board is only available on DSE.");
            return ExchangeRouteResult.Accepted(exchange, "SME board — DSE order accepted.");
        }

        // ATBPublic: both exchanges
        if (board == Board.ATBPublic)
            return ExchangeRouteResult.Accepted(exchange, "ATBPublic board accepted.");

        // Public board: all order types, both exchanges
        return ExchangeRouteResult.Accepted(exchange, $"{exchange} Public board — order accepted.");
    }

    public static bool IsValidForExchange(string stockExchange, ExchangeId orderExchange)
    {
        var stockEx = stockExchange?.ToUpperInvariant();
        return orderExchange switch
        {
            ExchangeId.DSE => stockEx == "DSE" || string.IsNullOrEmpty(stockEx),
            ExchangeId.CSE => stockEx == "CSE",
            _ => false
        };
    }
}

public class ExchangeRouteResult
{
    public bool IsAccepted { get; private set; }
    public ExchangeId? Exchange { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public static ExchangeRouteResult Accepted(ExchangeId exchange, string message)
        => new() { IsAccepted = true, Exchange = exchange, Message = message };

    public static ExchangeRouteResult Rejected(string message)
        => new() { IsAccepted = false, Message = message };
}
