using BdStockOMS.API.Models;

namespace BdStockOMS.API.FIX;

public class FIXValidationResult
{
    public bool IsValid            { get; set; } = true;
    public List<string> Errors     { get; set; } = new();
    public List<string> Warnings   { get; set; } = new();
    public string? FIXOrdType      { get; set; }
    public string? FIXTimeInForce  { get; set; }
    public string? FIXSide         { get; set; }
}

public static class FIXOrderTypeValidator
{
    public static FIXValidationResult Validate(FIXOrderRequest req)
    {
        var result = new FIXValidationResult
        {
            FIXSide        = req.OrderType == OrderType.Buy ? "1" : "2",
            FIXOrdType     = GetOrdType(req.Category),
            FIXTimeInForce = GetTIF(req.TimeInForce),
        };

        // Qty must be > 0
        if (req.Quantity <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Quantity must be greater than 0.");
        }

        // Limit orders require price
        if (req.Category == OrderCategory.Limit && !req.Price.HasValue)
        {
            result.IsValid = false;
            result.Errors.Add("Limit orders require a Price.");
        }

        // MarketAtBest must not have price
        if (req.Category == OrderCategory.MarketAtBest && req.Price.HasValue)
            result.Warnings.Add("MarketAtBest orders should not specify a price. Price will be ignored.");

        // FOK + Market not allowed (DSE rule)
        if (req.Category == OrderCategory.Market && req.TimeInForce == TimeInForce.FOK)
        {
            result.IsValid = false;
            result.Errors.Add("FOK is not allowed with Market orders on DSE. Use Limit+FOK.");
        }

        // DisplayQty must be <= Qty (iceberg)
        if (req.DisplayQty.HasValue && req.DisplayQty.Value > req.Quantity)
        {
            result.IsValid = false;
            result.Errors.Add($"DisplayQty ({req.DisplayQty}) cannot exceed Quantity ({req.Quantity}).");
        }

        // MinQty must be <= Qty
        if (req.MinQty.HasValue && req.MinQty.Value > req.Quantity)
        {
            result.IsValid = false;
            result.Errors.Add($"MinQty ({req.MinQty}) cannot exceed Quantity ({req.Quantity}).");
        }

        // Private + Market not allowed
        if (req.IsPrivate && req.Category == OrderCategory.Market)
        {
            result.IsValid = false;
            result.Errors.Add("Private orders must be Limit orders.");
        }

        // Block board: Limit only
        if ((req.Board == Board.Block || req.Board == Board.BuyIn || req.Board == Board.SPublic)
            && req.Category != OrderCategory.Limit)
        {
            result.IsValid = false;
            result.Errors.Add($"{req.Board} board only accepts Limit orders.");
        }

        // IOC + MarketAtBest not standard
        if (req.Category == OrderCategory.MarketAtBest && req.TimeInForce == TimeInForce.IOC)
            result.Warnings.Add("IOC with MarketAtBest is non-standard. Exchange may reject.");

        return result;
    }

    public static string GetOrdType(OrderCategory cat) => cat switch
    {
        OrderCategory.Market       => "1",
        OrderCategory.Limit        => "2",
        OrderCategory.MarketAtBest => "P",
        _ => "2"
    };

    public static string GetTIF(TimeInForce tif) => tif switch
    {
        TimeInForce.Day => "0",
        TimeInForce.IOC => "3",
        TimeInForce.FOK => "4",
        _ => "0"
    };

    public static string GetSide(OrderType side) => side == OrderType.Buy ? "1" : "2";
}
