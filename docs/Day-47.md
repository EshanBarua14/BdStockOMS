# Day 47 - MFA, Session Policy & User Permissions

**Branch:** `day-47-mfa-permissions`
**Tests:** 717 (start) -> 746 (end) | +29 tests
**All tests:** 746 passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | UserPermission model + DbSet + migration | Done |
| 2 | SessionPolicy model (configurable per role) | Done |
| 3 | ISessionPolicyService + SessionPolicyService | Done |
| 4 | IUserPermissionService + UserPermissionService | Done |
| 5 | Auto logout on inactivity (configurable per role) | Done |
| 6 | Max concurrent sessions enforcement (configurable) | Done |
| 7 | Single session mode for Admin/SuperAdmin | Done |
| 8 | MFA required flag per role (Admin/SuperAdmin default true) | Done |
| 9 | Day47_MfaPermissions migration applied | Done |
| 10 | 29 new tests, 746 total passing | Done |

---

## New Models

### UserPermission
**File:** `BdStockOMS.API/Models/UserPermission.cs`

Granular permission system per user.

| Field | Type | Description |
|-------|------|-------------|
| UserId | int | Owner of the permission |
| Permission | string | e.g. "orders.approve", "kyc.view" |
| Module | string | e.g. "Orders", "KYC", "Reports" |
| IsGranted | bool | false = explicitly denied |
| GrantedByUserId | int | Who granted this |
| GrantedAt | DateTime | When it was granted |
| ExpiresAt | DateTime? | null = never expires |
| IsActive (computed) | bool | IsGranted AND not expired |

### SessionPolicy
**File:** `BdStockOMS.API/Models/SessionPolicy.cs`

In-memory model for per-role session rules. Defaults overridable via SystemSettings at runtime.

| Field | Default (SuperAdmin) | Default (Investor) |
|-------|---------------------|-------------------|
| MaxConcurrentSessions | 1 | 5 |
| InactivityTimeoutMinutes | 20 | 60 |
| MfaRequired | true | false |
| SingleSessionOnly | true | false |

---

## New Services

### ISessionPolicyService / SessionPolicyService
**File:** `BdStockOMS.API/Services/SessionPolicyService.cs`

| Method | Description |
|--------|-------------|
| GetPolicyAsync(role) | Load policy, override from SystemSettings if present |
| EnforceSessionLimitAsync | Revoke oldest sessions when over limit |
| IsSessionActiveAsync | Check session + inactivity timeout |
| TouchSessionAsync | Update LastSeenAt on activity |
| RevokeAllSessionsAsync | Logout all sessions (except current) |
| CreateSessionAsync | Create new session with policy-based expiry |
| RevokeSessionAsync | Revoke single session token |
| PurgeExpiredSessionsAsync | Cleanup expired/revoked sessions |

#### Runtime configuration via SystemSettings keys:
    Session:{RoleName}:MaxConcurrentSessions
    Session:{RoleName}:InactivityTimeoutMinutes
    Session:{RoleName}:MfaRequired
    Session:{RoleName}:SingleSessionOnly

### IUserPermissionService / UserPermissionService
**File:** `BdStockOMS.API/Services/UserPermissionService.cs`

| Method | Description |
|--------|-------------|
| GetUserPermissionsAsync(userId) | All active permissions for a user |
| HasPermissionAsync(userId, permission) | Check single permission |
| GrantPermissionAsync | Grant (upsert) a permission, optional expiry |
| RevokePermissionAsync | Revoke a permission |
| GetModulePermissionsAsync | Filter by module |

---

## Migration

**Name:** `Day47_MfaPermissions`
- Added `UserPermissions` table
- Composite unique index on `(UserId, Permission)`
- FK to Users (cascade) and GrantedBy Users (restrict)

---

## Default Session Policies

| Role | Max Sessions | Inactivity Timeout | MFA Required | Single Session |
|------|-----------|--------------------|-------------|----------------|
| SuperAdmin | 1 | 20 min | Yes | Yes |
| Admin | 1 | 30 min | Yes | Yes |
| Compliance | 2 | 30 min | No | No |
| CCD | 2 | 30 min | No | No |
| Trader | 3 | 60 min | No | No |
| Investor | 5 | 60 min | No | No |
| BrokerageHouse | 3 | 30 min | No | No |

---

## New Tests (+29)

### UserPermissionServiceTests (11 tests)
- Grant, revoke, upsert, expiry, module filter, user isolation

### SessionPolicyTests (10 tests)
- Policy defaults per role, session active/inactive/expired, multi-session count

### MfaModelTests (8 tests)
- OTP store/retrieve, mark used, expiry, UserPermission IsActive logic

---

## Test Count Progression

| Day | Passing | Notes |
|-----|---------|-------|
| 46 | 717 | Start of Day 47 |
| 47 | 746 | +29 tests |

---

## Next: Day 48 - Final Validation & Swagger Docs

- Swagger full annotation (all controllers)
- API versioning
- Final validation pass
- React frontend begins
- Target: 760+ tests
