# Day 80 - Multi-Tenant V2

## What was built

### Models/TenantFeatureFlag.cs (new)
Per-tenant feature flag model. Fields: BrokerageHouseId, FeatureKey, IsEnabled, Value, Description, SetByUserId, CreatedAt, UpdatedAt.

### Services/Interfaces/ITenantDbContextFactory.cs (new)
Future-ready interface: CreateForTenantAsync(int), IsPerTenantDbEnabled.
When false (current): returns shared AppDbContext.
When true (future): creates scoped DbContext per tenant connection string.

### Services/SharedTenantDbContextFactory.cs (new)
Implements ITenantDbContextFactory. IsPerTenantDbEnabled=false always.
Returns shared AppDbContext. Zero-code-change path to per-tenant DBs.
Registered: AddScoped<ITenantDbContextFactory, SharedTenantDbContextFactory>()

### Services/ITenantContext.cs (updated)
Added IsFeatureEnabled(string featureKey) method.
SuperAdmin always returns true. BrokerageHouseId=0 returns false.

### Services/TenantContext.cs (updated)
Constructor now takes AppDbContext as second parameter.
Implements IsFeatureEnabled via AsNoTracking DB query.
Handles both BrokerageHouseId and brokerageHouseId claim variants.

### Controllers/TenantFeatureFlagsController.cs (new)
SuperAdmin-only. Endpoints:
- GET  /api/tenant-features/{brokerageHouseId}
- POST /api/tenant-features/{brokerageHouseId}
- DELETE /api/tenant-features/{brokerageHouseId}/{featureKey}
- GET  /api/tenant-features/all-flags
- POST /api/tenant-features/bulk/{brokerageHouseId}

### Migration: Day80_MultiTenantV2
20260329061057_Day80_MultiTenantV2 applied. Creates TenantFeatureFlags table.

## Known Feature Keys
ENABLE_CSE_TRADING, BLOCK_BOARD_TRADING, MARGIN_TRADING,
ICEBERG_ORDERS, PRIVATE_ORDERS, FOK_ORDERS

## Tests - Day80Tests.cs - 16 tests
- TenantFeatureFlag defaults, fields, save/retrieve, isolation, update, delete, bulk
- BrokerageConnection defaults and save/retrieve
- SharedTenantDbContextFactory IsPerTenantDbEnabled=false, returns shared DB
- TenantProvisioningService sanitizes DB name and removes special chars
- ITenantContext has IsFeatureEnabled with correct signature
- ITenantContext has all required members
- ITenantDbContextFactory has correct members

## Test Results
- Previous: 1,090 passing
- Day 80: 1,106 passing (+16)
- Failed: 0

## Branch
day-80-multitenant-v2 (from day-79-order-model-v2)

## Next - Day 81
Granular RBAC: UserPermissions table, BO Group, Basket, 60+ permission constants, Permission middleware
