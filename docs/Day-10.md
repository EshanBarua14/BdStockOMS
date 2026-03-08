# Day 10 — Security Hardening

**Branch:** day-10-security-hardening
**Tests:** 69 passing (was 52, +17 new security tests)

## What Was Built

### New Models
- `RefreshToken` — token rotation with IsActive/IsExpired/IsRevoked computed props
- `LoginHistory` — every login attempt recorded with IP, UserAgent, success/failure

### User Model Updates
- `FailedLoginCount` — increments on each failed login
- `LockoutUntil` — datetime when lockout expires
- `ForcePasswordChange` — flag for first login / admin reset
- `PasswordChangedAt` — tracks last password change

### New Services
- `IAuditService / AuditService` — append-only audit log for all sensitive actions
- `ITokenBlacklistService / TokenBlacklistService` — Redis-backed JWT blacklist by JTI and userId prefix

### Middleware (5 new)
- `GlobalExceptionMiddleware` — catches all unhandled exceptions, logs to SystemLog, returns safe JSON error
- `SecurityHeadersMiddleware` — X-Frame-Options, X-Content-Type-Options, X-XSS-Protection, Referrer-Policy
- `RequestLoggingMiddleware` — logs method, path, userId, statusCode, durationMs
- `TokenBlacklistMiddleware` — checks JTI and userId blacklist on every authenticated request
- `IdempotencyMiddleware` — caches POST/PUT/PATCH responses in Redis for 24h via X-Idempotency-Key header

### Background Services
- `AccountUnlockService` — runs every 60s, auto-unlocks users whose LockoutUntil has passed

### Program.cs Updates
- Redis registered as `IConnectionMultiplexer` singleton
- CORS policy: localhost:5173 + *.vercel.app
- Built-in ASP.NET Core rate limiter (login: 5/15min, api: 100/1min)
- All middleware registered in correct pipeline order
- `IAuditService`, `ITokenBlacklistService`, `AccountUnlockService` registered

### Database
- Migration: `Day10_SecurityHardening`
- New tables: `RefreshTokens`, `LoginHistories`
- New columns on `Users`: FailedLoginCount, LockoutUntil, ForcePasswordChange, PasswordChangedAt

## Middleware Pipeline Order
1. GlobalExceptionMiddleware
2. SecurityHeadersMiddleware
3. RequestLoggingMiddleware
4. Swagger (dev only)
5. CORS
6. RateLimiter
7. HttpsRedirection
8. Authentication
9. TokenBlacklistMiddleware
10. Authorization
11. IdempotencyMiddleware
12. Controllers + Hubs

## Tests Added (SecurityTests.cs)
- AuditService: LogAsync creates record, stores old/new values, null entityId, multiple logs
- RefreshToken: IsActive, IsExpired, IsRevoked computed properties
- User: lockout field defaults, LockoutUntil set/read
- LoginHistory: success record, failed record with reason
- DbContext: save RefreshToken, save LoginHistory, multiple tokens per user
- Security headers: required header names
- Idempotency: key format validation
