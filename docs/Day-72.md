# Day 72 - Reports Module (Trade/Commission/Investors/Fund Requests)

**Branch:** `day-72-reports`
**Tests:** 971 (start) -> 987 (end) | +16 tests
**All tests:** 987 backend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | ReportsPage.tsx — 4-tab reports UI | Done |
| 2 | Trade Orders tab — summary cards + detailed table | Done |
| 3 | Commission tab — breakdown by component (brokerage/CDBL/SECD/VAT) | Done |
| 4 | Top Investors tab — ranked table with gold/silver/bronze badges | Done |
| 5 | Fund Requests tab — deposit summary cards + table | Done |
| 6 | Brokerage selector + date range filters | Done |
| 7 | CSV export for all 4 report types | Done |
| 8 | Print/PDF via window.print() for Trade Orders | Done |
| 9 | 5 new API client functions (report endpoints) | Done |
| 10 | /reports route wired in App.tsx | Done |
| 11 | Day72Tests.cs — 16 BrokerageReportService tests | Done |
| 12 | Build passing: 0 errors | Done |

---

## Report Types

### Trade Orders Report (/api/brokeragereport/{id}/orders)
- Total/Buy/Sell/Executed/Pending/Cancelled/Rejected counts
- Total order value in BDT
- CSV export + Print/PDF

### Commission Report (/api/brokeragereport/{id}/commission)
- Brokerage commission (0.40%)
- CDBL fee (0.015%)
- SECD fee (0.015%)
- VAT on commission (15%)
- Total estimated commission

### Top Investors (/api/brokeragereport/{id}/top-investors)
- Ranked by total traded value
- Gold/silver/bronze rank badges
- Configurable top N (5/10/20/50)

### Fund Requests (/api/brokeragereport/{id}/fund-requests)
- Total/Pending/Completed/Rejected counts
- Total deposited amount

## Export Options
- **CSV**: Downloads directly to browser — all 4 report types
- **Print/PDF**: Opens print dialog — formatted HTML report with BdStockOMS branding

---

## Test Count Progression

| Day | Backend | Total | Notes |
|-----|---------|-------|-------|
| 71 | 971 | 971 | Start |
| 72 | 987 | 987 | +16: BrokerageReportService tests |

---

## Next: Day 73 - Back Office UI (BOS XML + Reconciliation Dashboard)