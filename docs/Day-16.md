# Day 16 — Fund Request Workflow

**Branch:** day-16-fund-request-workflow
**Tests:** 161 passing (was 148, +13 new tests)

## What Was Built

### FundRequestService
- CreateRequestAsync — validates amount, checks pending, creates request
- ApproveByTraderAsync — Trader approves Pending → ApprovedByTrader
- ApproveByCCDAsync — CCD approves ApprovedByTrader → ApprovedByCCD
- RejectAsync — any stage → Rejected with mandatory reason
- CompleteAsync — credits CashBalance on User model
- GetRequestsAsync — paginated, filterable by status and investor
- GetMyRequestsAsync — investor's own requests paginated

### Business Rules
- Max single request: 1 crore BDT
- Only one pending request allowed at a time
- Strict workflow: Pending → TraderApproved → CCDApproved → Completed
- Rejection requires a reason
- Cash balance credited only on completion

### FundRequestController (7 endpoints)
- POST /api/fund-requests — investor creates request
- GET /api/fund-requests/my — investor views own requests
- GET /api/fund-requests — trader/CCD/admin views all requests
- PUT /api/fund-requests/{id}/approve-trader
- PUT /api/fund-requests/{id}/approve-ccd
- PUT /api/fund-requests/{id}/reject
- PUT /api/fund-requests/{id}/complete
- GET /api/fund-requests/balance — investor cash balance

## Tests Added (FundRequestServiceTests.cs — 13 tests)
- Create: valid, zero amount, exceeds limit, pending exists
- Trader approval: pending succeeds, already approved fails
- CCD approval: trader approved succeeds, not trader approved fails
- Reject: pending succeeds, no reason fails
- Complete: CCD approved credits balance, not CCD approved fails
- Pagination: GetMyRequests returns paged result
