# Day 18 — Watchlist System

**Branch:** day-18-watchlist-system
**Tests:** 186 passing (was 172, +14 new tests)

## What Was Built

### WatchlistService
- CreateWatchlistAsync — name validation, max 10 watchlists per user
- DeleteWatchlistAsync — cannot delete default watchlist
- RenameWatchlistAsync — owner-only rename
- AddStockAsync — duplicate check, max 50 stocks, active stock validation
- RemoveStockAsync — owner-only removal
- ReorderStocksAsync — update sort order for drag-and-drop UI
- GetMyWatchlistsAsync — all watchlists with live stock prices
- GetWatchlistAsync — single watchlist with items
- EnsureDefaultWatchlistAsync — auto-creates "My Watchlist" on first login

### WatchlistWithItems DTO
- Watchlist metadata + stocks with live price, change, changePercent
- Items sorted by SortOrder

### WatchlistController (7 endpoints)
- GET /api/watchlists — all user watchlists
- GET /api/watchlists/{id} — single watchlist with stocks
- POST /api/watchlists — create new watchlist
- PUT /api/watchlists/{id}/rename — rename watchlist
- DELETE /api/watchlists/{id} — delete non-default watchlist
- POST /api/watchlists/{id}/stocks — add stock
- DELETE /api/watchlists/{id}/stocks/{stockId} — remove stock
- PUT /api/watchlists/{id}/reorder — reorder stocks

## Tests Added (WatchlistServiceTests.cs — 14 tests)
- Create: valid name, empty name, name too long, exceeds max 10
- Delete: non-default succeeds, default fails
- Add stock: valid, duplicate, invalid stock
- Remove stock: existing succeeds
- Reorder: updates sort order correctly
- Get: returns all watchlists
- EnsureDefault: creates if missing, does not duplicate
