# Day 79 - Order Model V2

## What was built

### Models/Order.cs (updated)
Extended the Order model with all fields required for DSE FIX certification.

New enums added:
- `OrderCategory.MarketAtBest` — DSE MarketAtBest order type (third category alongside Market/Limit)
- `TimeInForce` — Day, IOC (Immediate-Or-Cancel), FOK (Fill-Or-Kill)
- `ExchangeId` — DSE, CSE (multi-exchange routing)
- `Board` — Public, SME, ATBPublic, Government, Debt, Block, BuyIn, SPublic (8 DSE boards)
- `ExecInstruction` — None, Suspend, Release, WholeOrNone
- `AggressorSide` — None=0, Unknown=0, Buy=1, Sell=-1

New OrderStatus values (8 added, 7 existing kept):
- `Queued` — in local queue before exchange submission
- `Submitted` — sent to exchange, awaiting acknowledgement
- `Waiting` — waiting for market conditions (e.g. pre-open)
- `CancelRequested` — cancel sent to exchange, awaiting confirmation
- `EditRequested` — amendment sent, awaiting confirmation
- `Deleted` — removed from exchange order book
- `Replaced` — superseded by an amended order
- `Private` — hidden/private order (IsPrivate=true)

New fields on Order class (14 fields):
- `TimeInForce` — Day/IOC/FOK, default Day
- `ExchangeId` — DSE/CSE, default DSE
- `Board` — which board the order is on, default Public
- `ExecInstruction` — suspend/release instructions, default None
- `MinQty` — minimum fill quantity (MinQty orders)
- `DisplayQty` — iceberg visible quantity
- `IsPrivate` — hidden order flag, default false
- `ExecutedQuantity` — shares filled so far, default 0
- `GrossTradeAmt` — ExecutionPrice x ExecutedQuantity
- `AggressorIndicator` — who was the aggressor (Buy/Sell/None)
- `ClOrdID` — FIX client order ID (auto-generated GUID if not supplied)
- `OrigClOrdID` — original ClOrdID before amendment
- `TrdMatchID` — exchange trade match ID (set on fill)
- `SettlDate` — settlement date in YYYYMMDD format (set on fill)
- `UpdatedAt` — last modified timestamp

### DTOs/Order/PlaceOrderDto.cs (updated)
Added all new fields: TimeInForce, ExchangeId, Board, ExecInstruction, MinQty, DisplayQty, IsPrivate, ClOrdID. Backward compatible — all new fields have defaults.

### DTOs/Order/OrderResponseDto.cs (updated)
Added all new response fields matching the Order model additions.

### Services/SimulatedOrderFillService.cs (updated)
- Queued -> Submitted -> Open lifecycle transition
- IOC/FOK: fill or cancel immediately
- MinQty: skip cycle if partial fill would not meet minimum
- Sets TrdMatchID, SettlDate, GrossTradeAmt, AggressorIndicator on fill
- Skips Private orders and Suspended orders

### Migration: Day79_OrderModelV2
EF Core migration `20260329051530_Day79_OrderModelV2` applied to DB.
17 ALTER TABLE statements on Orders table:
AggressorIndicator, Board, ClOrdID, DisplayQty, ExchangeId, ExecInstruction,
ExecutedQuantity, FillProbability, GrossTradeAmt, IsPrivate, MinQty, OrigClOrdID,
SettlDate, SlippagePercent, TimeInForce, TrdMatchID, UpdatedAt

## Tests - Day79Tests.cs - 20 tests
- OrderCategory has MarketAtBest
- TimeInForce has Day, IOC, FOK
- ExchangeId has DSE and CSE
- Board all 8 values exist (Theory x8)
- ExecInstruction has all 4 values
- OrderStatus 8 new values exist (Theory x8)
- OrderStatus 7 legacy values unchanged
- OrderStatus never has "Executed"
- AggressorSide has None, Buy, Sell
- Order defaults are correct (TimeInForce=Day, ExchangeId=DSE, Board=Public, etc.)
- Order nullable new fields are null by default
- Order private order can be set
- Order iceberg fields (DisplayQty/MinQty) work
- Order FIX fields (ClOrdID/OrigClOrdID/TrdMatchID/SettlDate/GrossTradeAmt) work
- Order UpdatedAt exists
- PlaceOrderDto new field defaults correct
- PlaceOrderDto accepts MarketAtBest + IOC + CSE + SME
- OrderResponseDto has all new fields
- Board Block/BuyIn/SPublic have correct ordinals (5/6/7)

## Test Results
- Previous: 1,057 passing
- Day 79: 1,090 passing (+33)
- Failed: 0

## Branch
day-79-order-model-v2 (from day-78-keyboard-nav)

## Next - Day 80
Multi-tenant v2: ITenantDbContextFactory, BrokerageConnections model,
per-tenant feature flags, SuperAdmin provision UI
