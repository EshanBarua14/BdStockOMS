# Day 30 — Trade Model + Order State Machine

## Branch
`day-30-trade-model-order-statemachine`

## Status
✅ Complete — 325 tests passing, 0 failures (up from 302)

---

## What Was Built

### New Models

| Model | Table | Purpose |
|-------|-------|---------|
| `Trade` | `Trades` | Individual fill per order — quantity, price, total value, exchange trade ID |
| `OrderEvent` | `OrderEvents` | Full audit trail — every status transition recorded with from/to/reason/triggeredBy |

### Trade Model
- Fields: `OrderId`, `StockId`, `InvestorId`, `BrokerageHouseId`, `Side`, `Quantity`, `Price`, `TotalValue`, `ExchangeTradeId`, `Status`, `TradedAt`
- `TradeStatus` enum: `Filled`, `PartialFill`, `Cancelled`
- Navigation: Order, Stock, User (Investor), BrokerageHouse

### OrderEvent Model
- Fields: `OrderId`, `FromStatus`, `ToStatus`, `Reason`, `TriggeredBy`, `OccurredAt`
- Navigation: Order
- `TriggeredBy` = UserId string or `"System"`

### IOrderStateMachine + OrderStateMachine
- `CanTransition(from, to)` — validates against allowed transition map
- `GetAllowedTransitions(current)` — returns valid next states
- `TransitionAsync(order, to, reason, triggeredBy)` — applies transition, writes `OrderEvent`, updates timestamps

### State Transition Map
```
Pending  → Executed, Cancelled, Rejected
Executed → Completed, Cancelled
Completed → (terminal)
Cancelled → (terminal)
Rejected  → (terminal)
```

### Timestamp Rules
| Transition to | Timestamp set |
|---------------|---------------|
| Executed | `ExecutedAt` |
| Completed | `CompletedAt` |
| Cancelled | `CancelledAt` |
| Rejected | `CancelledAt` + `RejectionReason` |

### Migration
`Day30_TradeModelOrderStateMachine` — adds `Trades` and `OrderEvents` tables

### DbContext
- `DbSet<Trade> Trades`
- `DbSet<OrderEvent> OrderEvents`

### Program.cs
- `builder.Services.AddScoped<IOrderStateMachine, OrderStateMachine>()`

---

## Tests
| File | Tests Added |
|------|-------------|
| `OrderStateMachineTests.cs` | 23 |
| **Total new** | **23** |

- Previous: 302 → Current: **325**

---

## Next: Day 31
- Real RMS Engine — cash balance, margin, sector concentration, daily value limit, single order value limit
- RMS breach → `TradeAlert` + SignalR notification
- Target: 355+ tests
