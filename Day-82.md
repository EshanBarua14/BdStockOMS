# Day 82 - Multi-Exchange

## What was built

### Services/ICseScraperService.cs (new)
Interface and records for CSE scraping.
Records: CseStockTick (TradingCode, LastTradePrice, Change, ChangePercent, Direction)
CseIndexData (CASPI, CSE30, CASPIChange, FetchedAt)
Interface: GetAllPricesAsync, GetIndexDataAsync, IsMarketOpen

### Services/CseScraperService.cs (new)
Scrapes https://www.cse.com.bd/market/current_price for live prices.
Scrapes https://www.cse.com.bd/market/index_chart for CASPI/CSE30.
Same market hours as DSE: Sun-Thu 10:00-14:30 BST.
Graceful error handling - returns empty list on failure.

### Services/IExchangeScraperFactory.cs (new)
ExchangeScraperFactory: DI-injected factory returning DSE or CSE scraper.
Methods: GetDseScraper(), GetCseScraper(), IsDseMarketOpen(), IsCseMarketOpen()

### Services/ExchangeOrderRouter.cs (new)
Static routing engine for exchange + board + order type combinations.
Rules:
- Block/BuyIn/SPublic boards: Limit orders only (Market/MarketAtBest rejected)
- Government/Debt/SME boards: DSE only (CSE rejected)
- ATBPublic: both exchanges, all order types
- Public: both exchanges, all order types
ExchangeRouteResult: IsAccepted, Exchange, Message
IsValidForExchange: validates stock exchange matches order exchange

### Controllers/ExchangeController.cs (new)
GET /api/exchange/status - DSE+CSE market open/closed status
GET /api/exchange/stocks/{exchange} - stocks by exchange (DSE/CSE)
GET /api/exchange/route?exchange=&board=&category= - validate order routing
GET /api/exchange/boards - board rules and availability

### Program.cs (updated)
Added HttpClient named DSE and CSE with base URLs and timeouts.
Registered: IDseScraperService, ICseScraperService, IExchangeScraperFactory.

## Exchange Routing Matrix
| Board | DSE | CSE | Market | Limit | MarketAtBest |
|-------|-----|-----|--------|-------|--------------|
| Public | Y | Y | Y | Y | Y |
| SME | Y | N | Y | Y | Y |
| ATBPublic | Y | Y | Y | Y | Y |
| Government | Y | N | Y | Y | Y |
| Debt | Y | N | Y | Y | Y |
| Block | Y | Y | N | Y | N |
| BuyIn | Y | N | N | Y | N |
| SPublic | Y | N | N | Y | N |

## Tests - Day82Tests.cs - 51 tests
- Block/BuyIn/SPublic only accept Limit (Theory x7)
- Government/Debt/SME DSE only (Theory x6)
- Public/ATBPublic all exchanges (Theory x4)
- Router accepted has exchange, rejected has null exchange
- IsValidForExchange routing (Theory x5)
- ExchangeId has DSE and CSE
- Board all 8 values exist
- Board ordinals correct (Block=5, BuyIn=6, SPublic=7)
- CseStockTick and CseIndexData records
- DseStockTick record
- Market hours weekday/weekend logic
- ExchangeRouteResult Accepted/Rejected properties
- Full routing matrix (Theory x15)

## Test Results
- Previous: 1,181 passing
- Day 82: 1,232 passing (+51)
- Failed: 0

## Branch
day-82-multi-exchange (from day-81-granular-rbac)

## Next - Day 83
RMS v2: 6-level cascade (Client/User/BOGroup/Basket/Branch/Broker), EDR calculation, margin 3-tier thresholds
