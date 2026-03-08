# Day 13 — All Models + Migration

**Branch:** day-13-all-models-migration
**Tests:** 121 passing (was 106, +15 new tests)

## What Was Built

### 14 New Models
- `CommissionRate` — system-wide buy/sell/CDBL/DSE fee rates
- `BrokerageCommissionRate` — per-brokerage commission override
- `InvestorCommissionRate` — per-investor negotiated rates with approval workflow
- `RMSLimit` — 6-level risk management (Investor/Trader/Stock/Sector/Market/Exchange)
- `SectorConfig` — 17 BD sectors with BSEC concentration limits
- `CorporateAction` — dividend, bonus share, rights, split, merger
- `FundRequest` — PPR deposit/withdrawal workflow (Pending→Trader→CCD→Complete)
- `MarketData` — OHLCV snapshots with unique index on StockId+Date+Exchange
- `NewsItem` — market/company/regulatory news with stock linking
- `Watchlist` + `WatchlistItem` — user watchlists with unique stock constraint
- `Notification` — 11 notification types with read tracking
- `SystemSetting` — key-value config store with encryption flag
- `OrderAmendment` — order edit audit trail
- `TraderReassignment` — investor-trader reassignment audit trail

### EF Core Configurations
- All decimal properties configured with HasPrecision(18, 4)
- All multi-FK entities use DeleteBehavior.Restrict to prevent cascade cycles
- Unique indexes: MarketData(StockId+Date+Exchange), WatchlistItem(WatchlistId+StockId), SystemSetting(Key)

### Migration
- Day13_AllModels applied successfully
- 14 new tables created in SQL Server

## Tests Added (ModelTests.cs — 15 tests)
- CommissionRate defaults, BrokerageCommissionRate, InvestorCommissionRate
- RMSLimit default action, RMSLevel has 6 values
- SectorConfig, CorporateAction (5 types), FundRequest (8 payment methods)
- MarketData OHLCV, NewsItem defaults, Watchlist + WatchlistItem
- Notification (11 types), SystemSetting, OrderAmendment, TraderReassignment
- TrustedDevice IsActive logic (active, revoked, expired)
