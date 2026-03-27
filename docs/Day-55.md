# Day 55 — Full Widget System Overhaul

**Branch:** `day-55-widget-system`
**Tests:** 177 passed (13 files)
**Build:** Clean

## What Was Built

### src/store/useTemplateStore.ts — REWRITTEN
- Multi-instance widget system: addWidgetInstance(), removeWidgetInstance()
- Each instance has unique instanceId (e.g. watchlist, watchlist-2, watchlist-3)
- addWidgetInstance: finds free grid position, injects layout entry + instance
- removeWidgetInstance: removes from layout + instances array
- setWidgetVisible: now calls addWidgetInstance/removeWidgetInstance (legacy compat)
- setWidgetColor: targets instanceId not widgetId
- Migration: old pages with widgets[] auto-migrate to instances[] on hydrate
- Page CRUD: addPage, deletePage, renamePage, setPageIcon, reorderPages all intact
- Export/Import: handles both old and new format

### src/pages/DashboardPage.tsx — REWRITTEN
- Widget Drawer: right-side slide-in panel, searchable, grouped by category
- Drag from drawer to grid: onDragStart sets dataTransfer widgetId, grid onDrop calls addWidgetInstance
- Click to add: adds widget at bottom of current layout
- Multi-instance: same widget can be added unlimited times
- Page tabs inline: double-click to rename, right-click for menu (rename/icon/delete)
- Page icons: emoji picker in right-click menu
- Add page: ＋ button creates new Trading preset page
- Preset buttons (Trading/Research/Portfolio) in toolbar
- Empty state: shows preset buttons + "Open Widget Drawer" CTA
- Grid: instanceId as key, resolves widgetId via instances array
- Close widget (×): calls removeWidgetInstance

### Backend — StockResponseDto + StockService
- StockResponseDto: +Category, +CircuitBreakerHigh, +CircuitBreakerLow, +BoardLotSize
- StockService.MapToDto: maps Category.ToString(), CircuitBreaker fields, BoardLotSize
- Now available in market data API response for BuySellConsole circuit breaker display

### src/test/Unit/Day52/templateStore.test.ts — UPDATED
- setWidgetVisible test: updated for new add/remove instance behavior
- setWidgetColor test: updated to use instanceId

## Architecture Notes
- Widget instances: { instanceId, widgetId, colorGroup }
- Layout key (l.i) === instanceId
- widgetId resolved: instance.widgetId ?? l.i.replace(/-\d+$/, '')
- WIDGET_REGISTRY keyed by widgetId
- DashboardPage: { id, name, icon, layout[], instances[] }
