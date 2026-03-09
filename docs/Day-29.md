# Day 29 — Multi-Tenant Isolation + IExchangeConnector

## Branch
`day-29-tenant-isolation-exchange-connector`

## Status
✅ Complete — 302 tests passing, 0 failures (up from 281)

---

## What Was Built

### ITenantContext + TenantContext
- `ITenantContext` — interface exposing `BrokerageHouseId`, `UserId`, `Role`, `IsSuperAdmin`
- `TenantContext` — reads JWT claims from `IHttpContextAccessor`
- `BrokerageHouseId` extracted from custom claim `"BrokerageHouseId"`
- `IsSuperAdmin` computed property — true when Role == "SuperAdmin"
- Registered as `AddScoped<ITenantContext, TenantContext>()` in Program.cs
- All services can now inject `ITenantContext` to scope queries to the caller's brokerage

### IExchangeConnector (Plug-and-Play Core)
- `IExchangeConnector` interface with full contract:
  - `ConnectAsync` / `DisconnectAsync` / `IsConnected`
  - `GetLatestPriceAsync` — single stock tick
  - `GetMarketDepthAsync` — 5-level bid/ask
  - `GetHistoricalDataAsync` — OHLCV by date range
  - `SendOrderAsync` / `CancelOrderAsync` / `GetOrderStatusAsync`
- DTOs: `MarketTickDto`, `MarketDepthDto`, `DepthLevelDto`, `OhlcDto`, `ExchangeOrderRequest`, `ExchangeOrderResult`, `ExchangeOrderStatus`

### SimulatedExchangeConnector
- Full in-memory implementation of `IExchangeConnector`
- Simulated price store for 5 stocks (GRAMEEN, BEXIMCO, SQPHARMA, BRAC, RENATA)
- Random price drift ±0.5% per tick
- 5-level market depth (bids below, asks above base price)
- Historical OHLCV generation — skips weekends (Saturday/Sunday)
- 95% order acceptance rate simulation
- Registered for both `"DSE"` and `"CSE"` keyed DI keys

### ExchangeConnectorFactory
- `IExchangeConnectorFactory.GetConnector(exchangeCode)` — resolves keyed service
- Throws `InvalidOperationException` for unknown exchange codes
- Swap to real connector with ONE line change in Program.cs

### Program.cs Registrations
```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddKeyedScoped<IExchangeConnector, SimulatedExchangeConnector>("DSE", ...);
builder.Services.AddKeyedScoped<IExchangeConnector, SimulatedExchangeConnector>("CSE", ...);
builder.Services.AddSingleton<IExchangeConnectorFactory, ExchangeConnectorFactory>();
```

---

## Tests
| File | Tests Added |
|------|-------------|
| `TenantContextTests.cs` | 5 |
| `SimulatedExchangeConnectorTests.cs` | 14 |
| **Total new** | **21** |

- Previous: 281 → Current: **302**

---

## Next: Day 30
- `Trades` table — individual fills per order
- `OrderEvents` table — full audit trail
- Order state machine: Created → Validated → RmsApproved → Queued → Sent → Acknowledged → PartialFill → Filled → Cancelled → Settled
- Target: 325+ tests
