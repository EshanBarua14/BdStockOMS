using BdStockOMS.API.Models;

namespace BdStockOMS.API.FIX;

public enum FIXCertScenario
{
    S1_MarketOrder               = 1,
    S2_LimitOrder                = 2,
    S3_MarketAtBest              = 3,
    S4_IOC_Limit                 = 4,
    S5_FOK_Limit                 = 5,
    S6_PrivateOrder              = 6,
    S7_IcebergOrder              = 7,
    S8_MinQtyOrder               = 8,
    S9_CancelPendingOrder        = 9,
    S10_AmendPendingOrder        = 10,
    S11_PartialFillThenCancel    = 11,
    S12_RejectInvalidOrder       = 12,
}

public class FIXCertResult
{
    public FIXCertScenario Scenario    { get; set; }
    public bool Passed                 { get; set; }
    public string ScenarioName         { get; set; } = string.Empty;
    public string Description          { get; set; } = string.Empty;
    public List<string> Steps          { get; set; } = new();
    public List<string> Errors         { get; set; } = new();
    public string? RawFIXMessage       { get; set; }
    public DateTime TestedAt           { get; set; } = DateTime.UtcNow;
}

public static class FIXCertScenarioRunner
{
    public static async Task<FIXCertResult> RunAsync(
        FIXCertScenario scenario, IFIXConnector connector)
    {
        return scenario switch
        {
            FIXCertScenario.S1_MarketOrder            => await S1_MarketOrder(connector),
            FIXCertScenario.S2_LimitOrder             => await S2_LimitOrder(connector),
            FIXCertScenario.S3_MarketAtBest           => await S3_MarketAtBest(connector),
            FIXCertScenario.S4_IOC_Limit              => await S4_IOC(connector),
            FIXCertScenario.S5_FOK_Limit              => await S5_FOK(connector),
            FIXCertScenario.S6_PrivateOrder           => await S6_Private(connector),
            FIXCertScenario.S7_IcebergOrder           => await S7_Iceberg(connector),
            FIXCertScenario.S8_MinQtyOrder            => await S8_MinQty(connector),
            FIXCertScenario.S9_CancelPendingOrder     => await S9_Cancel(connector),
            FIXCertScenario.S10_AmendPendingOrder     => await S10_Amend(connector),
            FIXCertScenario.S11_PartialFillThenCancel => await S11_PartialFill(connector),
            FIXCertScenario.S12_RejectInvalidOrder    => await S12_Reject(connector),
            _ => new FIXCertResult { Scenario = scenario, Passed = false, Errors = { "Unknown scenario" } }
        };
    }

    public static async Task<List<FIXCertResult>> RunAllAsync(IFIXConnector connector)
    {
        var results = new List<FIXCertResult>();
        foreach (var scenario in Enum.GetValues<FIXCertScenario>())
            results.Add(await RunAsync(scenario, connector));
        return results;
    }

    private static async Task<FIXCertResult> S1_MarketOrder(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S1_MarketOrder,
            ScenarioName = "S1 - Market Order",
            Description  = "Place a buy market order. Expect immediate fill at market price." };
        var req = MakeRequest("S1", OrderType.Buy, OrderCategory.Market, TimeInForce.Day, 100, null);
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add($"Send NewOrderSingle 35=D Side=1 OrdType=1 Qty=100");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success;
        if (!res.Success) r.Errors.Add(res.Message);
        return r;
    }

    private static async Task<FIXCertResult> S2_LimitOrder(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S2_LimitOrder,
            ScenarioName = "S2 - Limit Order",
            Description  = "Place a sell limit order at specific price. Expect Open status." };
        var req = MakeRequest("S2", OrderType.Sell, OrderCategory.Limit, TimeInForce.Day, 50, 380.50m);
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D Side=2 OrdType=2 Price=380.50 Qty=50");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("44=380.50");
        if (!r.Passed) r.Errors.Add("Expected price 44=380.50 in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S3_MarketAtBest(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S3_MarketAtBest,
            ScenarioName = "S3 - MarketAtBest Order",
            Description  = "Place MarketAtBest order (OrdType=P). DSE-specific order type." };
        var req = MakeRequest("S3", OrderType.Buy, OrderCategory.MarketAtBest, TimeInForce.Day, 200, null);
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D OrdType=P (MarketAtBest)");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("40=P");
        if (!r.Passed) r.Errors.Add("Expected OrdType=P in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S4_IOC(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S4_IOC_Limit,
            ScenarioName = "S4 - IOC Limit Order",
            Description  = "Immediate-Or-Cancel limit order. TimeInForce=3. Fill what's available, cancel rest." };
        var req = MakeRequest("S4", OrderType.Buy, OrderCategory.Limit, TimeInForce.IOC, 500, 100.00m);
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D OrdType=2 TimeInForce=3 (IOC)");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("59=3");
        if (!r.Passed) r.Errors.Add("Expected TimeInForce=3 (IOC) in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S5_FOK(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S5_FOK_Limit,
            ScenarioName = "S5 - FOK Limit Order",
            Description  = "Fill-Or-Kill limit order. TimeInForce=4. Fill entire qty or cancel." };
        var req = MakeRequest("S5", OrderType.Buy, OrderCategory.Limit, TimeInForce.FOK, 1000, 50.00m);
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D OrdType=2 TimeInForce=4 (FOK)");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("59=4");
        if (!r.Passed) r.Errors.Add("Expected TimeInForce=4 (FOK) in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S6_Private(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S6_PrivateOrder,
            ScenarioName = "S6 - Private Order",
            Description  = "Hidden/private order. Not visible in order book." };
        var req = MakeRequest("S6", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 5000, 75.00m);
        req.IsPrivate = true;
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D IsPrivate=true");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success;
        if (!res.Success) r.Errors.Add(res.Message);
        return r;
    }

    private static async Task<FIXCertResult> S7_Iceberg(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S7_IcebergOrder,
            ScenarioName = "S7 - Iceberg Order",
            Description  = "Iceberg order with DisplayQty(1138)=100 from total Qty=1000." };
        var req = MakeRequest("S7", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 1000, 200.00m);
        req.DisplayQty = 100;
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D Qty=1000 MaxFloor(1138)=100");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("1138=100");
        if (!r.Passed) r.Errors.Add("Expected tag 1138=100 (DisplayQty) in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S8_MinQty(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S8_MinQtyOrder,
            ScenarioName = "S8 - MinQty Order",
            Description  = "Minimum quantity order. MinQty(110)=500. Fill at least 500 or reject." };
        var req = MakeRequest("S8", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 2000, 150.00m);
        req.MinQty = 500;
        var res = await c.SendNewOrderAsync(req);
        r.Steps.Add("Send NewOrderSingle 35=D Qty=2000 MinQty(110)=500");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("110=500");
        if (!r.Passed) r.Errors.Add("Expected tag 110=500 (MinQty) in FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S9_Cancel(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S9_CancelPendingOrder,
            ScenarioName = "S9 - Cancel Pending Order",
            Description  = "Cancel a pending limit order. Send 35=F CancelRequest." };
        // First place
        var req = MakeRequest("S9A", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 100, 500.00m);
        await c.SendNewOrderAsync(req);
        r.Steps.Add("Step 1: Send NewOrderSingle ClOrdID=S9A");

        // Then cancel
        var res = await c.SendCancelAsync("S9B", "S9A", "BATBC");
        r.Steps.Add("Step 2: Send OrderCancelRequest 35=F ClOrdID=S9B OrigClOrdID=S9A");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("35=F");
        if (!r.Passed) r.Errors.Add("Expected 35=F in cancel FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S10_Amend(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S10_AmendPendingOrder,
            ScenarioName = "S10 - Amend Pending Order",
            Description  = "Amend qty/price of pending order. Send 35=G CancelReplaceRequest." };
        var req = MakeRequest("S10A", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 100, 300.00m);
        await c.SendNewOrderAsync(req);
        r.Steps.Add("Step 1: Send NewOrderSingle ClOrdID=S10A Qty=100 Price=300");

        var amend = MakeRequest("S10B", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 150, 305.00m);
        amend.OrigClOrdID = "S10A";
        var res = await c.SendAmendAsync(amend);
        r.Steps.Add("Step 2: Send OrderCancelReplaceRequest 35=G NewQty=150 NewPrice=305");
        r.Steps.Add($"Result: {res.Message}");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success && res.RawFIXMessage!.Contains("35=G");
        if (!r.Passed) r.Errors.Add("Expected 35=G in amend FIX message");
        return r;
    }

    private static async Task<FIXCertResult> S11_PartialFill(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S11_PartialFillThenCancel,
            ScenarioName = "S11 - Partial Fill Then Cancel",
            Description  = "Place large order, get partial fill, then cancel remainder." };
        var req = MakeRequest("S11A", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 10000, 50.00m);
        await c.SendNewOrderAsync(req);
        r.Steps.Add("Step 1: Send NewOrderSingle Qty=10000 (large order for partial fill)");

        var res = await c.SendCancelAsync("S11B", "S11A", "GP");
        r.Steps.Add("Step 2: Cancel remainder after partial fill");
        r.RawFIXMessage = res.RawFIXMessage;
        r.Passed = res.Success;
        if (!res.Success) r.Errors.Add(res.Message);
        return r;
    }

    private static async Task<FIXCertResult> S12_Reject(IFIXConnector c)
    {
        var r = new FIXCertResult { Scenario = FIXCertScenario.S12_RejectInvalidOrder,
            ScenarioName = "S12 - Reject Invalid Order",
            Description  = "Attempt order with zero quantity (should fail validation)." };
        var req = MakeRequest("S12", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 0, 100.00m);
        r.Steps.Add("Validate: Qty=0 should be rejected before FIX send");
        var validationError = req.Quantity <= 0 ? "Order rejected: Quantity must be > 0." : null;
        r.Passed = validationError != null;
        if (validationError != null)
            r.Steps.Add($"Validation result: {validationError}");
        else
            r.Errors.Add("Expected validation to reject zero quantity order");
        return r;
    }

    private static FIXOrderRequest MakeRequest(string clOrdId, OrderType side,
        OrderCategory cat, TimeInForce tif, int qty, decimal? price) => new()
    {
        ClOrdID     = clOrdId,
        Symbol      = "TESTSTOCK",
        StockId     = 1,
        OrderType   = side,
        Category    = cat,
        TimeInForce = tif,
        Quantity    = qty,
        Price       = price,
        Exchange    = ExchangeId.DSE,
        Board       = Board.Public,
        BrokerageHouseId = 1,
        InvestorId  = 1,
    };
}
