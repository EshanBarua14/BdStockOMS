# Step 1: Foundation — Design System + Layout Shell
## Installation Guide

### File Placement
Copy these files into your `E:\Projects\BdStockOMS\BdStockOMS.Client\` project:

```
src/
├── App.tsx                          ← Replace your existing App.tsx
├── styles/
│   ├── design-tokens.css            ← NEW: Design system tokens
│   └── global.css                   ← NEW: Global styles + utilities
├── services/
│   ├── signalRService.ts            ← NEW: Centralized SignalR hub manager
│   └── apiService.ts                ← NEW: REST API service
├── stores/
│   ├── MarketDataStore.tsx           ← NEW: Real-time market data context
│   └── AuthStore.tsx                 ← NEW: Auth/RBAC context
├── components/
│   ├── common/
│   │   ├── WidgetShell.tsx           ← NEW: Universal widget wrapper
│   │   └── WidgetShell.css
│   ├── AppLayout.tsx                 ← NEW: Master layout grid
│   ├── AppLayout.css
│   ├── PriceTicker.tsx               ← NEW: Scrolling price ribbon
│   ├── PriceTicker.css
│   ├── Topbar.tsx                    ← NEW: Search/Indexes/Status/Notif/News
│   ├── Topbar.css
│   ├── Sidebar.tsx                   ← NEW: RBAC side menu
│   ├── Sidebar.css
│   ├── TradingWorkspace.tsx          ← NEW: Widget grid + initial widgets
│   ├── TradingWorkspace.css
│   ├── BottomBar.tsx                 ← NEW: Activity/status strip
│   ├── BottomBar.css
│   ├── LoginPage.tsx                 ← NEW: Glass login screen
│   └── LoginPage.css
```

### Ensure Dependencies
Make sure you have `@microsoft/signalr` installed:
```bash
npm install @microsoft/signalr
```

### What This Step Delivers
1. **Design System** — 100+ CSS variables (colors, spacing, shadows, neon glows, 3D depth)
2. **Glass Panel System** — `.glass-panel`, `.glass-panel-heavy`, `.glass-panel-neon` utilities
3. **Widget Shell** — Universal wrapper every widget uses (header, exchange tabs, minimize/detach/close, loading/error states)
4. **SignalR Service** — Centralized hub connection with auto-reconnect (stock prices + notifications)
5. **Market Data Store** — React Context providing real-time stock data to ALL widgets
6. **Auth Store** — RBAC user session with `hasRole()` / `hasPermission()` helpers
7. **Full Layout** — Ticker → Topbar → Sidebar + Workspace → BottomBar
8. **Live Widgets** — Watchlist, Top Movers, Chart (placeholder), Order Console, Indexes, AI Sentiment (simulated)
9. **Login Page** — Premium glass login (pre-filled with admin@bdstockoms.com / Admin@1234)

### Backend Connection Points
- SignalR Stock Hub: `/api/hubs/stockprice` → `ReceiveStockPrices` event
- SignalR Notification Hub: `/api/hubs/notification` → `ReceiveNotification` event
- REST endpoints: `/api/auth/login`, `/api/auth/me`, `/api/marketdata/*`, `/api/orders/*`, etc.
- Vite proxy: `/api` → `http://localhost:5289` (already configured)

### After Copying
1. Start your backend: `dotnet run` in the API project
2. Start the client: `npm run dev` in BdStockOMS.Client
3. You should see the login screen → sign in → full dark glass layout with live data

### What's Coming Next (Step 2)
- Full-featured Price Ticker customization (scrolling/tile/grid modes)
- Notification drawer with categories + customization
- News drawer with sector/symbol filtering
- Theme drawer with Light/Dark/Pro/Glass/3D modes
