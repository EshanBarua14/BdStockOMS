# Day 44 — Integration Tests + Concurrency Tests

**Branch:** day-44-integration-concurrency
**Tests:** 661 passing, 0 failing (was 631)
**New Tests:** 30
**New Tables:** 0
**New Services:** 0

## What Was Built

Two new test suites — integration tests using WebApplicationFactory against
the real ASP.NET Core pipeline, and concurrency tests validating thread-safe
database operations under parallel load.

## Files Created

| File | Purpose |
|------|---------|
| Tests/Integration/BdStockOmsFactory.cs | WebApplicationFactory with InMemory DB override |
| Tests/Integration/AuthIntegrationTests.cs | 13 integration tests — auth, RBAC, protected routes |
| Tests/Unit/ConcurrencyTests.cs | 17+ concurrency tests — parallel writes, reads, race conditions |

## Infrastructure Changes

- Added `public partial class Program { }` to Program.cs for WebApplicationFactory access
- Added `InternalsVisibleTo` to BdStockOMS.API.csproj for test project access
- Added `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.SqlServer` packages to test project

## Integration Tests Cover

- Unauthenticated requests to protected endpoints return 401
- Invalid JWT token returns 401
- Login with wrong credentials returns 401
- All major protected routes: stocks, portfolio, watchlists, kyc, fund-requests, bos
- App startup and swagger availability

## Concurrency Tests Cover

- 10-20 simultaneous order writes — all saved, all unique IDs
- 15 parallel portfolio reads — no exceptions, consistent data
- 25 concurrent audit log writes — all saved
- 20 parallel RMS limit reads — consistent values across threads
- 10 concurrent BOS session writes — all saved
- Simultaneous order + audit writes — neither blocks the other
- Isolated databases — no cross-contamination between test DBs
- Concurrent stock, user, notification, watchlist, market data, fund request operations
- Deadlock detection — two threads writing different entities complete within timeout
