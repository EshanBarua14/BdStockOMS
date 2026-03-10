# Day 45 - Performance, Indexes & Load Testing

**Branch:** `day-45-performance-load`
**Tests:** 661 (start) -> 681 (end) | +20 tests
**Load tests:** 7 NBomber scenarios (excluded from default run)

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | ICacheService / CacheService with TTL & key constants | Done |
| 2 | Day45_PerformanceIndexes migration - 8 composite DB indexes | Done |
| 3 | CacheServiceTests - 19 unit tests | Done |
| 4 | LoadTests - 7 NBomber scenarios (100 to 5000 ramp + login stress) | Done |
| 5 | Exclude load tests via [Trait("Category","Load")] | Done |
| 6 | Build clean - 0 errors | Done |
| 7 | 681 unit+integration tests passing | Done |

---

## CacheService

**File:** `BdStockOMS.API/Services/CacheService.cs`

- ICacheService interface with GetAsync<T>, SetAsync<T>, RemoveAsync, RemoveByPrefixAsync
- CacheTtl static class: Short=5min, Standard=15min, Long=1hr, VeryLong=24hr
- CacheKeys static class: centralised key builders per domain (Orders, Users, Dashboard, etc.)
- Backed by IDistributedCache (Redis in production, in-memory for tests)
- Registered in Program.cs as AddSingleton<ICacheService, CacheService>()

---

## Database Indexes (Day45_PerformanceIndexes)

8 composite indexes covering the highest-traffic query patterns:

| Table | Index Columns | Purpose |
|-------|---------------|---------|
| Orders | (UserId, CreatedAt DESC) | Per-user order history |
| Orders | (Status, CreatedAt DESC) | Status-filtered listings |
| Orders | (BrokerId, CreatedAt DESC) | Per-broker reports |
| AuditLogs | (UserId, CreatedAt DESC) | User activity timeline |
| AuditLogs | (EntityName, EntityId) | Entity-specific audit trail |
| Notifications | (UserId, IsRead, CreatedAt DESC) | Unread notification badge |
| BosImportSessions | (UserId, CreatedAt DESC) | Import history per user |
| BosImportSessions | (Status, CreatedAt DESC) | Failed import monitoring |

---

## Load Tests (NBomber 4.1.2)

**File:** `BdStockOMS.Tests/Performance/LoadTests.cs`

All tests tagged [Trait("Category", "Load")] - excluded from default dotnet test run.

| Test | Simulation |
|------|------------|
| LoadTest_Baseline_100Users | Ramp to 100 + Inject 100/s x 15s |
| LoadTest_Ramp_500Users | Ramp to 500 + Inject 500/s x 15s |
| LoadTest_Ramp_1000Users | Ramp to 1000 + Inject 1000/s x 15s |
| LoadTest_Ramp_2000Users | Ramp to 2000 + Inject 2000/s x 15s |
| LoadTest_Ramp_3500Users | Ramp to 3500 + Inject 3500/s x 15s |
| LoadTest_FullRamp_5000Users | Full gradual ramp to 5000 + sustain |
| LoadTest_LoginStress_1000Users | Login endpoint stress 1000/s |

### Running load tests (requires API running on localhost:5000)

    # Terminal 1 - start API
    cd BdStockOMS.API && dotnet run

    # Terminal 2 - run load tests
    dotnet test --filter "Category=Load" -v n

### Default run (load tests excluded)

    dotnet test --filter "Category!=Load"

---

## Test Count Progression

| Day | Passing | Notes |
|-----|---------|-------|
| 44 | 661 | Start of Day 45 |
| 45 | 681 | +20 tests (CacheService + load traits fixed) |

---

## Next: Day 46 - VAPT

- OWASP ZAP automated scan integration
- IDOR vulnerability tests
- JWT tampering tests (alg:none, expired, wrong signature)
- Auth bypass tests (missing token, wrong role, horizontal escalation)
- Target: 705+ tests
