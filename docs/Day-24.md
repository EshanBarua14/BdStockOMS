# Day 24 — Admin Dashboard APIs

## Branch
`day-24-admin-dashboard`

## Summary
Built a unified admin dashboard service that aggregates system-wide stats
into a single API call, plus individual section endpoints for partial refresh.

## Files Created
### DTOs
- `DTOs/Dashboard/DashboardDtos.cs`
  - UserStatsDto — user counts by role, active/locked
  - OrderStatsDto — today/month orders, pending, traded value
  - FundRequestStatsDto — pending approvals, completed today, deposited today
  - SystemStatsDto — brokerage houses, stocks, watchlists, unread notifications
  - RecentActivityDto — one activity feed item (Order or FundRequest)
  - AdminDashboardDto — all sections combined in one response

### Services
- `Services/IAdminDashboardService.cs` + `AdminDashboardService.cs`
  - GetDashboardAsync — full dashboard in one call
  - GetUserStatsAsync — user counts by role
  - GetOrderStatsAsync — today/month order activity
  - GetFundRequestStatsAsync — fund request summary
  - GetSystemStatsAsync — brokerage/stock/watchlist counts
  - GetRecentActivityAsync — combined order + fund request feed

### Controllers
- `Controllers/AdminDashboardController.cs`
  - All endpoints restricted to SuperAdmin and Admin
  - GET /api/admindashboard — full dashboard
  - GET /api/admindashboard/users
  - GET /api/admindashboard/orders
  - GET /api/admindashboard/fund-requests
  - GET /api/admindashboard/system
  - GET /api/admindashboard/activity

### Program.cs
- Registered IAdminDashboardService

### Tests
- `Tests/Unit/AdminDashboardServiceTests.cs` — 12 new tests

## Test Results
- Previous: 252 passing
- Today: 264 passing (+12)
- Failed: 0

## API Endpoints Added
| Method | Route | Auth |
|--------|-------|------|
| GET | /api/admindashboard | SuperAdmin, Admin |
| GET | /api/admindashboard/users | SuperAdmin, Admin |
| GET | /api/admindashboard/orders | SuperAdmin, Admin |
| GET | /api/admindashboard/fund-requests | SuperAdmin, Admin |
| GET | /api/admindashboard/system | SuperAdmin, Admin |
| GET | /api/admindashboard/activity | SuperAdmin, Admin |
