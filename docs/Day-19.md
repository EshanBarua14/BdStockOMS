# Day 19 — Market Data + Corporate Actions + News

## Branch
`day-19-market-data-corporate-actions`

## Summary
Built full CRUD APIs for MarketData, CorporateAction, and NewsItem — three models
that were already migrated but had no service or controller layer.

## Files Created
### DTOs
- `DTOs/MarketData/MarketDataDtos.cs` — Request/response shapes + bulk + query
- `DTOs/CorporateAction/CorporateActionDtos.cs` — Create/update/response
- `DTOs/News/NewsDtos.cs` — Create/update/response + query with filters

### Services
- `Services/IMarketDataService.cs` + `MarketDataService.cs`
  - GetAll (paginated, filterable by stock/exchange/date range)
  - GetById, GetByStock (last N days)
  - Create (duplicate guard per stock+exchange+date)
  - BulkCreate (skips duplicates, returns created/skipped counts)
  - Delete
- `Services/ICorporateActionService.cs` + `CorporateActionService.cs`
  - GetAll (filterable by stockId + isProcessed)
  - GetById, GetByStock
  - Create (validates CorporateActionType enum + value > 0)
  - Update (blocked if already processed)
  - MarkProcessed, Delete (blocked if already processed)
- `Services/INewsService.cs` + `NewsService.cs`
  - GetAll (paginated, filterable by category/stock/published)
  - GetById, Create, Update
  - Publish, Unpublish, Delete

### Controllers
- `Controllers/MarketDataController.cs` — GET/POST/DELETE + bulk endpoint
- `Controllers/CorporateActionController.cs` — Full CRUD + mark-processed
- `Controllers/NewsController.cs` — Full CRUD + publish/unpublish (GET endpoints AllowAnonymous)

### Program.cs
- Registered `IMarketDataService`, `ICorporateActionService`, `INewsService`

### Tests
- `Tests/Unit/MarketDataServiceTests.cs` — 14 new tests

## Test Results
- Previous: 186 passing
- Today: 200 passing (+14)
- Failed: 0

## API Endpoints Added
| Method | Route | Auth |
|--------|-------|------|
| GET | /api/marketdata | Any |
| GET | /api/marketdata/{id} | Any |
| GET | /api/marketdata/stock/{stockId}/{exchange} | Any |
| POST | /api/marketdata | SuperAdmin,Admin,ITSupport |
| POST | /api/marketdata/bulk | SuperAdmin,Admin,ITSupport |
| DELETE | /api/marketdata/{id} | SuperAdmin,Admin |
| GET | /api/corporateaction | Any |
| GET | /api/corporateaction/{id} | Any |
| GET | /api/corporateaction/stock/{stockId} | Any |
| POST | /api/corporateaction | SuperAdmin,Admin |
| PUT | /api/corporateaction/{id} | SuperAdmin,Admin |
| POST | /api/corporateaction/{id}/mark-processed | SuperAdmin,Admin |
| DELETE | /api/corporateaction/{id} | SuperAdmin,Admin |
| GET | /api/news | Anonymous |
| GET | /api/news/{id} | Anonymous |
| POST | /api/news | SuperAdmin,Admin |
| PUT | /api/news/{id} | SuperAdmin,Admin |
| POST | /api/news/{id}/publish | SuperAdmin,Admin |
| POST | /api/news/{id}/unpublish | SuperAdmin,Admin |
| DELETE | /api/news/{id} | SuperAdmin,Admin |
