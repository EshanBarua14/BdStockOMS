# Day 73 - Fix SimulatedOrderFillService + Colored Status Badges

## Root Cause
All orders appeared as SELL in the Order Book. The SignalR broadcast had
 which was correct, but dead code paths
meant the value was never reliably set. Also: every pending order filled
instantly on every tick with no price logic or probability gating.

## Model Naming (critical for future days)
- OrderType     = Buy | Sell       <- this is the BUY/SELL side
- OrderCategory = Market | Limit   <- this is the order category
- Fills are atomic (no FilledQuantity on Order model)

## Fixes

### BackgroundServices/SimulatedOrderFillService.cs
- Side fix: side = order.OrderType.ToString() always resolves to Buy or Sell
- Market orders: 90% fill probability, only after 1500ms age, 0-0.2% slippage
- Limit orders: only fill when market crosses limit price, 55% probability,
  only after 3000ms age
- Slippage: buyers pay above market, sellers receive below market
- Hub stays as StockPriceHub, signal stays as TradeExecuted

### Frontend - OrderStatusBadge.tsx (new)
- OrderStatusBadge: colored pill + animated dot for live statuses
  0 Pending   -> amber pulsing dot
  1 Open      -> sky pulsing dot
  2 Partial   -> blue pulsing dot
  3 Filled    -> emerald solid dot
  4 Settled   -> teal solid dot
  5 Cancelled -> gray solid dot
  6 Rejected  -> red solid dot
- OrderSideBadge: green BUY / red SELL (accepts numeric 0/1 or string)

## Tests
BdStockOMS.Tests/Unit/Day73Tests.cs - 12 tests, 0 failures

## Next - Day 74
BOS XML reconciliation dashboard
