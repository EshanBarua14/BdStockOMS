# Day 11 — Architecture Hardening

**Branch:** day-11-architecture-hardening
**Tests:** 91 passing (was 69, +22 new tests)

## What Was Built

### Repository Pattern (4 repositories)
- `IRepository<T>` — generic base interface with CRUD + FindAsync
- `BaseRepository<T>` — EF Core implementation
- `IUserRepository / UserRepository` — GetByEmailAsync, GetByIdWithRoleAsync, GetPagedAsync
- `IOrderRepository / OrderRepository` — GetPagedAsync with filters, GetPendingOrdersAsync
- `IStockRepository / StockRepository` — GetByTradingCodeAsync, GetPagedAsync with search/filter
- `IRefreshTokenRepository / RefreshTokenRepository` — GetActiveTokenAsync, RevokeAllUserTokensAsync

### Common Patterns
- `Result<T>` — IsSuccess, Value, Error, ErrorCode — replaces throw-on-error
- `Result` — non-generic version for void operations
- `PagedResult<T>` — Items, TotalCount, Page, PageSize, TotalPages, HasNextPage, HasPreviousPage

### AuthService — Full Rewrite
- Constructor now takes: IRefreshTokenRepository, IAuditService, ITokenBlacklistService
- LoginAsync returns Result<AuthResponseDto> with IP address
- Real refresh token issued on every login
- Account lockout after 5 failed attempts (30 min)
- LoginHistory recorded on every attempt (success + failure)
- AuditService.LogAsync on LOGIN_SUCCESS, LOGIN_FAILED, LOGOUT
- RefreshTokenAsync — validates, revokes old, issues new pair
- LogoutAsync — blacklists JWT JTI + revokes refresh token

### AuthController — Full Rewrite
- POST /api/auth/login — passes IP, sets httpOnly cookie, returns Result
- POST /api/auth/refresh — reads cookie, rotates token
- POST /api/auth/logout — blacklists token, clears cookie
- GET /api/auth/me — unchanged

### ITSupportController (new)
- POST /api/itsupport/unlock/{userId} — unlocks account, audit logs
- GET /api/itsupport/locked-accounts — lists all locked users
- Roles: ITSupport, SuperAdmin

### Pagination Added
- GET /api/users — page, pageSize, roleId query params
- GET /api/stocks — page, pageSize, exchange, category, search
- GET /api/orders — page, pageSize, status filter

### Health Checks
- GET /health — JSON response with sqlserver + redis status
- AddDbContextCheck<AppDbContext> for SQL Server
- AddRedis for Redis

### NuGet Packages Added
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 8.0.0
- AspNetCore.HealthChecks.Redis 8.0.1
- Moq (test project)

## Tests Added
- ArchitectureTests.cs — Result<T>, PagedResult, UserRepository, StockRepository, RefreshTokenRepository (20 tests)
- AuthServiceTests.cs — rewritten with login history, refresh token, lockout (9 tests replacing 5)
