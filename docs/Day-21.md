# Day 21 — Portfolio P&L Reporting

## Branch
`day-21-portfolio-pnl-reporting`

## Summary
Built a Portfolio P&L reporting service that calculates unrealized profit/loss
for each investor holding using current market prices and historical MarketData.

## P&L Formulas Used
- CostBasis      = Quantity × AverageBuyPrice
- CurrentValue   = Quantity × Stock.LastTradePrice
- UnrealizedPnL  = CurrentValue - CostBasis
- PnLPercent     = (UnrealizedPnL / CostBasis) × 100
- TotalPortfolio = CashBalance + TotalCurrentValue

## Files Created
### DTOs
- `DTOs/Portfolio/PortfolioDtos.cs`
  - PortfolioHoldingDto — one stock with full P&L breakdown
  - PortfolioSummaryDto — all holdings + totals + cash balance
  - PortfolioHistoryItemDto — one day's portfolio value (for charts)

### Services
- `Services/IPortfolioPnlService.cs` + `PortfolioPnlService.cs`
  - GetPortfolioSummaryAsync — full P&L summary for one investor
  - GetHoldingAsync — P&L for one specific stock holding
  - GetPortfolioHistoryAsync — day-by-day value using MarketData close prices

### Controllers
- `Controllers/PortfolioController.cs`
  - Investors can only view their own portfolio (role-based guard)
  - Traders/CCD/Admin can view any investor's portfolio

### Program.cs
- Registered IPortfolioPnlService

### Tests
- `Tests/Unit/PortfolioPnlServiceTests.cs` — 13 new tests

## Test Results
- Previous: 212 passing
- Today: 225 passing (+13)
- Failed: 0

## API Endpoints Added
| Method | Route | Auth |
|--------|-------|------|
| GET | /api/portfolio/{investorId}/summary | Authenticated (own only for Investor) |
| GET | /api/portfolio/{investorId}/holding/{stockId} | Authenticated (own only for Investor) |
| GET | /api/portfolio/{investorId}/history?days=30 | Authenticated (own only for Investor) |
