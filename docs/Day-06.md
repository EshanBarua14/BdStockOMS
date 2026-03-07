# Day 06 — Stock Management APIs

## What Was Built
- `GET /api/stocks` — All logged-in users can view all active stocks
- `GET /api/stocks/{id}` — View a single stock by ID
- `GET /api/stocks/search?query=GP` — Search by TradingCode or CompanyName
- `POST /api/stocks` — CCD/Admin adds a new stock
- `PUT /api/stocks/{id}` — CCD/Admin updates stock price data
- `DELETE /api/stocks/{id}` — CCD/Admin soft deactivates a stock

## Role Permission Matrix
| Endpoint              | CCD | Admin | BrokerageHouse | Trader | Investor |
|-----------------------|-----|-------|----------------|--------|----------|
| GET /api/stocks       | ✅  | ✅    | ✅             | ✅     | ✅       |
| GET /api/stocks/{id}  | ✅  | ✅    | ✅             | ✅     | ✅       |
| GET /api/stocks/search| ✅  | ✅    | ✅             | ✅     | ✅       |
| POST /api/stocks      | ✅  | ✅    | ❌             | ❌     | ❌       |
| PUT /api/stocks/{id}  | ✅  | ✅    | ❌             | ❌     | ❌       |
| DELETE /api/stocks/{id}| ✅ | ✅    | ❌             | ❌     | ❌       |

## New Files
- DTOs/Stock/CreateStockDto.cs
- DTOs/Stock/UpdateStockDto.cs
- DTOs/Stock/StockResponseDto.cs
- Services/StockService.cs
- Controllers/StockController.cs
- Tests/Unit/StockServiceTests.cs

## Tests
- Previous: 25 passing
- New: +9 StockServiceTests
- **Total: 34 passing, 0 failing**

## Next: Day 07
- Order Management APIs
- Traders place buy/sell orders
- Order status lifecycle: Pending → Executed → Cancelled
