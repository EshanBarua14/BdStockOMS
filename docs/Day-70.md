# Day 70 - Accounts Module UI (Deposit/IPO/T-Bond)

**Branch:** `day-70-accounts-ui`
**Tests:** 931 (start) -> 948 (end) | +17 tests
**All tests:** 948 backend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | AccountsPage.tsx — Fund request workflow UI | Done |
| 2 | Fund request table with Trader/CCD/Complete/Reject actions | Done |
| 3 | BO Accounts tab — balance and margin overview | Done |
| 4 | New Fund Request modal — amount, payment method, ref no | Done |
| 5 | Reject modal with reason input | Done |
| 6 | Summary cards — total/pending/completed/amount | Done |
| 7 | IPOPage.tsx — open IPOs grid + my applications table | Done |
| 8 | IPO application modal — lots selector, BO account, total calc | Done |
| 9 | TBondPage.tsx — government securities market + holdings | Done |
| 10 | T-Bond buy modal — units, annual interest calc | Done |
| 11 | API client — 8 fund request functions added | Done |
| 12 | Routes /accounts /ipo /tbond wired in App.tsx | Done |
| 13 | Day70Tests.cs — 17 FundRequest entity + workflow tests | Done |
| 14 | Build passing: 0 errors | Done |

---

## Pages Built

### AccountsPage (/accounts)
- Tabs: Fund Requests | BO Accounts
- Fund request workflow: Pending → ApprovedByTrader → ApprovedByCCD → Completed
- Filter by status dropdown
- Action buttons per row based on current status
- New Fund Request modal with 8 payment methods

### IPOPage (/ipo)
- Open IPOs grid — card layout with lot size, price, deadline
- Premium badge for IPOs with share premium
- My Applications table with allotment status
- Application modal — lot quantity selector + total calculation

### TBondPage (/tbond)
- Government securities table — coupon, YTM, maturity, status
- Portfolio summary cards — total value, P&L, holdings count
- My Holdings table with current value and P&L
- Buy modal — units selector + annual interest calculation

## API Client Functions Added
getFundRequests · getMyFundRequests · createFundRequest
approveFundTrader · approveFundCCD · rejectFundRequest
completeFundRequest · getMyBalance

---

## Test Count Progression

| Day | Backend | Total | Notes |
|-----|---------|-------|-------|
| 69 | 931 | 931 | Start |
| 70 | 948 | 948 | +17 FundRequest entity + workflow tests |

---

## Next: Day 71 - Reports Module (Trade/Portfolio/Commission/Audit)