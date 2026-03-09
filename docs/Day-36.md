# Day 36 - Portfolio Snapshots + Advanced Analytics

## Summary
Implemented daily portfolio snapshot capture and stock analytics (VWAP, 52W high/low, Beta, ROI%).

## What Was Built

### Models
- `PortfolioSnapshot` - daily capture of investor portfolio value, PnL, ROI
- `StockAnalytics` - per-stock analytics: VWAP, 52W high/low, Beta, 30D avg volume

### Service
- `IPortfolioSnapshotService` / `PortfolioSnapshotService`
  - `CaptureSnapshotAsync` - calculates and stores daily snapshot for a user
  - `CaptureAllSnapshotsAsync` - batch capture for all users with holdings
  - `GetSnapshotHistoryAsync` - date-range history for charting
  - `GetLatestSnapshotAsync` - most recent snapshot
  - `CalculateRoiAsync` - ROI% from latest snapshot
  - `UpsertStockAnalyticsAsync` - insert or update VWAP/52W/Beta data
  - `GetStockAnalyticsAsync` - fetch analytics for one stock
  - `GetAllAnalyticsAsync` - all analytics for an exchange

### Controller
- `PortfolioSnapshotController` - REST endpoints
  - POST /api/portfoliosnapshot/capture/{userId}
  - POST /api/portfoliosnapshot/capture-all
  - GET  /api/portfoliosnapshot/history/{userId}
  - GET  /api/portfoliosnapshot/latest/{userId}
  - GET  /api/portfoliosnapshot/roi/{userId}
  - POST /api/portfoliosnapshot/analytics
  - GET  /api/portfoliosnapshot/analytics/{stockId}/{exchange}
  - GET  /api/portfoliosnapshot/analytics/{exchange}

### Database
- Migration: `AddPortfolioSnapshotsAndAnalytics`
- Composite index on PortfolioSnapshots (UserId, SnapshotDate)
- Unique index on StockAnalytics (StockId, Exchange)

## Tests
- Previous: 416
- Today: 440
- Added: 24 new snapshot + analytics tests
