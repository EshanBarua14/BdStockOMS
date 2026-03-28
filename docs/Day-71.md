# Day 71 - Real DSE Market Data Scraper

**Branch:** `day-71-real-market-data`
**Tests:** 948 (start) -> 966 (end) | +18 tests
**All tests:** 966 backend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | HtmlAgilityPack installed for HTML parsing | Done |
| 2 | IDseScraperService interface + DseStockTick + DseIndexData records | Done |
| 3 | DseScraperService — scrapes dsebd.org/latest_share_price_scroll_l.php | Done |
| 4 | Parses 780 DSE stocks: code, price, change, changePct, direction | Done |
| 5 | DSE index scraper — DSEX/DSES/DS30 from dseX_share.php | Done |
| 6 | IsMarketOpen() — Sun-Thu 10:00-14:30 BST timezone check | Done |
| 7 | RealMarketDataService — replaces StockPriceUpdateService | Done |
| 8 | Real data every 30s during market hours | Done |
| 9 | Cached prices when market closed (no simulation overwrite) | Done |
| 10 | Graceful fallback on scrape failure | Done |
| 11 | HttpClient registered with DSE-compatible User-Agent headers | Done |
| 12 | StockPriceUpdateService commented out (not deleted — kept as fallback) | Done |
| 13 | isRealData flag in SignalR broadcast — frontend knows source | Done |
| 14 | Day71Tests.cs — 18 tests (scraper logic, market hours, HTML parsing) | Done |
| 15 | Build passing: 0 errors | Done |

---

## Architecture

```
DSE dsebd.org/latest_share_price_scroll_l.php
  └── HtmlAgilityPack parser
      └── 780 DseStockTick records
          └── RealMarketDataService (BackgroundService)
              └── Update Stocks table in DB
              └── SignalR BulkPriceUpdate → all connected clients
                  └── Dashboard widgets update in real time
```

## Data Source

| Source | URL | Interval | Data |
|--------|-----|----------|------|
| DSE Scroll Page | dsebd.org/latest_share_price_scroll_l.php | 30s (market open) | 780 stocks: code, LTP, change, % |
| DSE Index Page | dsebd.org/dseX_share.php | 5min | DSEX, DSES, DS30 |
| Fallback | Cached DB prices | N/A | No simulation overwrite |

## Market Schedule (DSE)

- Trading days: Sunday to Thursday
- Market hours: 10:00 — 14:30 BST (UTC+6)
- Friday & Saturday: market closed
- Scrape interval: 30s open, 5min closed

## What's Next for Real Data

- CSE (cse.com.bd) scraper — needs browser automation (Selenium/Playwright) as site requires JS
- Individual stock OHLCV from displayCompany.php pages (batch on startup)
- Volume data from DSE market summary page
- Real-time order book from DSE TREC websocket feed (requires DSE broker membership)

---

## Test Count Progression

| Day | Backend | Total | Notes |
|-----|---------|-------|-------|
| 70 | 948 | 948 | Start |
| 71 | 966 | 966 | +18: DSE scraper logic, market hours, HTML parsing |

---

## Next: Day 72 - Reports Module (Trade/Portfolio/Commission CSV+PDF)