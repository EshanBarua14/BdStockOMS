# Day 81 - Granular RBAC

## What was built

### Authorization/Permissions.cs (new)
60+ permission constants organized by module.
Modules: orders, portfolio, kyc, reports, rms, accounts, funds, market, bos, admin, trade, compliance, ipo, tbond, notifications, tenant, system.
Permissions.All() - reflection-based enumeration of all keys.
Permissions.DefaultsForRole(roleName) - default permission set per role:
- Investor: place/cancel/amend own orders, view own portfolio, market data
- Trader: view/manage all orders, portfolio export, RMS view, trade monitor
- Admin: full order/kyc/reports/rms/accounts/bos/admin management
- CCD: accounts, funds, portfolio, BOS
- Compliance: compliance, audit, reports, freeze
- SuperAdmin: all permissions

### Authorization/RequirePermissionAttribute.cs (new)
IAsyncActionFilter attribute for granular permission checks.
Usage: [RequirePermission(Permissions.OrdersApprove)]
SuperAdmin bypasses all checks. Others checked via IUserPermissionService.
Returns 403 with {message, permission} on denial.

### Models/BOGroup.cs (new)
BOGroup: group investors for shared RMS limits.
Fields: Id, Name, Description, BrokerageHouseId, IsActive, CreatedAt, UpdatedAt.
BOGroupMember: junction table linking BOGroup to Users.

### Models/Basket.cs (new)
Basket: group stocks for bulk limit rules.
Fields: Id, Name, Description, BrokerageHouseId, IsActive, CreatedAt, UpdatedAt.
BasketStock: junction table with optional MaxOrderValue per stock.

### Controllers/UserPermissionsController.cs (new)
GET  /api/permissions/my - own permissions
GET  /api/permissions/my/check/{permission} - check single permission
GET  /api/permissions/user/{userId} - admin view user permissions
POST /api/permissions/grant - grant permission to user
POST /api/permissions/revoke - revoke permission from user
GET  /api/permissions/constants - all permission keys grouped by module
GET  /api/permissions/defaults/{roleName} - default permissions for role
POST /api/permissions/seed/{userId}/{roleName} - seed defaults for user

### Controllers/BOGroupController.cs (new)
GET/POST /api/bo-groups - list/create groups
GET /api/bo-groups/{id} - get group with members
POST /api/bo-groups/{id}/members - add member
DELETE /api/bo-groups/{id}/members/{userId} - remove member
DELETE /api/bo-groups/{id} - deactivate group

### Controllers/BasketController.cs (new)
GET/POST /api/baskets - list/create baskets
POST /api/baskets/{id}/stocks - add stock to basket
DELETE /api/baskets/{id}/stocks/{stockId} - remove stock
DELETE /api/baskets/{id} - deactivate basket

### AppDbContext (updated)
Added: BOGroups, BOGroupMembers, Baskets, BasketStocks DbSets.

### Migration: Day81_GranularRBAC
20260329094848_Day81_GranularRBAC applied. Creates BOGroups, BOGroupMembers, Baskets, BasketStocks tables.

## Tests - Day81Tests.cs - 39 tests
- Permissions.All() returns 60+ keys
- All keys are unique
- All keys have dot separator
- 11 specific permission keys verified present
- DefaultsForRole not empty for all 6 roles
- SuperAdmin gets all permissions
- Investor can place but not view all orders
- Trader can view all orders
- Admin can set RMS limits
- Unknown role returns empty
- BOGroup defaults, save/retrieve, multi-tenant isolation
- BOGroupMember add/remove
- Basket defaults, save/retrieve, deactivate
- BasketStock add/remove with MaxOrderValue
- UserPermission IsActive logic (granted, expired, revoked, future expiry)
- UserPermission grant and revoke

## Test Results
- Previous: 1,142 passing
- Day 81: 1,181 passing (+39)
- Failed: 0

## Branch
day-81-granular-rbac (from day-80-multitenant-v2)

## Next - Day 82
Multi-exchange: CSE scraper, Board enum routing, per-exchange order rules
