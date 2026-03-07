# Day 05 — Role-Based Authorization + User Management

## What Was Built
- JWT now embeds `ClaimTypes.Role` + custom `BrokerageHouseId` claim
- `GET /api/auth/me` — returns current user profile from JWT
- `POST /api/users` — BrokerageHouse creates Admin/Trader/Investor users
- `GET /api/users` — BrokerageHouse/Admin lists their brokerage's users
- `GET /api/users/{id}` — role-scoped lookup (Traders/Investors see only themselves)
- `DELETE /api/users/{id}` — soft delete (IsActive = false)

## Role Permission Matrix
| Endpoint              | BrokerageHouse | Admin | Trader    | Investor  |
|-----------------------|---------------|-------|-----------|-----------|
| POST /api/users       | ✅            | ❌    | ❌        | ❌        |
| GET /api/users        | ✅            | ✅    | ❌        | ❌        |
| GET /api/users/{id}   | ✅            | ✅    | self only | self only |
| DELETE /api/users/{id}| ✅            | ❌    | ❌        | ❌        |

## Bug Fixes
- Removed duplicate `BdStockOMS.API.*` namespaces from AuthService
- Fixed JWT config key mismatch (`Jwt:Key` → `JwtSettings:SecretKey`)
- Fixed model field mismatches (`BrokerageName` → `FirmName`, etc.)
- Updated 3 old tests from ThrowsException → ReturnsNull pattern

## Tests
- Previous: 19 passing
- New: +6 UserServiceTests
- **Total: 25 passing, 0 failing**

## Next: Day 06
- Stock Management APIs (CRUD)
- `[Authorize(Roles = "Admin,CCD")]` on stock endpoints
- Stock search and filter
