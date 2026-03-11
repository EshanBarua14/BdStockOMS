# Day 49 - React Frontend Scaffold & Design System

**Branch:** `day-49-react-frontend`
**Tests:** 776 (start) -> 800 (end) | +24 frontend tests
**All tests:** 776 backend passing, 24 frontend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | Vite 5 + React 18 + TypeScript strict + Tailwind 4 scaffold | Done |
| 2 | Axios API client with JWT interceptors + proactive/reactive refresh | Done |
| 3 | Zustand auth store + theme store with localStorage persistence | Done |
| 4 | Login page → /api/auth/login with animated canvas background | Done |
| 5 | SignUp page → 3-step flow with role selection + password strength | Done |
| 6 | Dashboard layout: collapsible Sidebar + Topbar + MarketTickerBar | Done |
| 7 | Dashboard page: KPI cards, recent orders table, top movers | Done |
| 8 | Design System v2: 5 themes, 6 accents, 3 densities, ThemePanel | Done |
| 9 | Protected routes with role-based access guards | Done |
| 10 | SignalR hook for real-time hub connections | Done |
| 11 | Logo component (crystalline hexagon SVG) | Done |
| 12 | 24 frontend tests in src/test/Day49/ | Done |
| 13 | Build passing: tsc + vite build, 0 errors, 112 modules | Done |
| 14 | Vite proxy: /api → https://localhost:7001, /hubs → ws | Done |

---

## Frontend Analysis (Pre-Day 49)

| Check | Result |
|-------|--------|
| React + Vite scaffold | Added Day 49 |
| JWT interceptor chain | Added Day 49 — proactive + reactive, no double-refresh |
| Zustand stores | Added Day 49 — auth + theme with persist middleware |
| Design system | Added Day 49 — CSS custom properties, 5 themes, 6 accents |
| Protected routes | Added Day 49 — role-based guards |
| SignalR hook | Added Day 49 — auto-reconnect, event map |
| Backend connectivity | Vite proxy configured → https://localhost:7001 |
| Frontend tests | Added Day 49 — 24 tests, 4 files |

---

## Project Structure

**File:** `BdStockOMS.Client/src/`

### API Layer
- `api/client.ts` — Axios instance + JWT interceptors + refresh queue
- `api/auth.ts` — authApi: login / refresh / logout / me
- `api/orders.ts` — ordersApi: list / getById / place / cancel

### Stores
- `store/authStore.ts` — user, isAuthenticated, setUser, logout (persisted)
- `store/themeStore.ts` — theme, accent, density, sidebar, ticker (persisted)

### Hooks
- `hooks/useAuth.ts` — login / logout / hasRole / hasPermission
- `hooks/useSignalR.ts` — hub connection with auto-reconnect + invoke

### Components
- `components/auth/ProtectedRoute.tsx` — JWT-gated + role-gated route wrapper
- `components/layout/DashboardLayout.tsx` — Sidebar + Topbar + Outlet
- `components/layout/Sidebar.tsx` — collapsible nav, role badge, logout
- `components/layout/Topbar.tsx` — breadcrumbs, market status, clock, notifications
- `components/ui/Logo.tsx` — crystalline hexagon SVG logo
- `components/ui/Alert.tsx` — error / success / warning / info variants
- `components/ui/Spinner.tsx` — Spinner + PageSpinner
- `components/ui/ThemePanel.tsx` — visual theme/accent/density switcher
- `components/widgets/MarketTickerBar.tsx` — live scrolling price bar

### Pages
- `pages/LoginPage.tsx` — animated canvas, floating chips, glass card
- `pages/SignUpPage.tsx` — 3-step flow, role selector, password strength
- `pages/DashboardPage.tsx` — KPI cards, orders table, top movers
- `pages/PlaceholderPages.tsx` — Orders, Portfolio, Market, 403, 404

---

## JWT Interceptor Design

**File:** `BdStockOMS.Client/src/api/client.ts`

### Proactive Refresh (Request Interceptor)
- If expiresAt - now < 60s → refresh before sending request
- All concurrent requests queue behind single refresh promise
- isRefreshing flag prevents duplicate refresh calls

### Reactive Refresh (Response Interceptor)
- 401 response → refresh once (_retry flag prevents loops)
- Queued requests resolved with new token after refresh
- On refresh failure → logout() + redirect to /login

---

## Design System v2

**Files:** `src/styles/themes.css`, `src/store/themeStore.ts`

| Token | Description |
|-------|-------------|
| 5 themes | Obsidian, Midnight, Slate, Aurora, Arctic |
| 6 accents | Azure, Cyan, Emerald, Violet, Rose, Amber |
| 3 densities | Compact, Comfortable, Spacious |
| Bull color | #00D4AA — buy / profit |
| Bear color | #FF6B6B — sell / loss |
| Fonts | Outfit (UI) + Space Grotesk (display) + Space Mono (data) |

---

## Vite Proxy Config

**File:** `BdStockOMS.Client/vite.config.ts`

    proxy: {
      '/api':  { target: 'https://localhost:7001', secure: false },
      '/hubs': { target: 'https://localhost:7001', secure: false, ws: true },
    }

Both frontend and backend must run simultaneously:
- Backend: https://localhost:7001 (dotnet run)
- Frontend: http://localhost:5173 (npm run dev)

---

## New Tests (+24)

### authStore.test.ts (6 tests)
Initialises null, setUser sets authenticated, logout clears user, stores correct role, stores permissions array, logout is idempotent

### ProtectedRoute.test.tsx (5 tests)
Redirects unauthenticated to /login, renders protected content, redirects wrong role to /forbidden, allows Admin, allows SuperAdmin

### Alert.test.tsx (7 tests)
Renders children, renders title, alert role, success variant, warning variant, onDismiss called, no button without onDismiss

### types.test.ts (6 tests)
AuthUser shape, Order status union, MarketTicker fields, PortfolioSummary P&L fields, Holding unrealizedPnl, ApiResponse wrapper

---

## Test Count Progression

| Day | Backend | Frontend | Total | Notes |
|-----|---------|----------|-------|-------|
| 48 | 776 | 0 | 776 | Start of Day 49 |
| 49 | 776 | 24 | 800 | +24 frontend tests |

---

## Dev Commands

    cd BdStockOMS.Client

    # Install
    npm install

    # Dev server (proxies /api and /hubs to :7001)
    npm run dev

    # Run Day 49 tests
    npx vitest run src/test/Day49

    # Run all frontend tests
    npx vitest run

    # Build
    npm run build

---

## Next: Day 50 - Orders, Portfolio & Component Library

- OrdersPage: place/cancel order form wired to /api/orders
- PortfolioPage: holdings table + P&L breakdown
- MarketPage: SignalR streaming ticker from /hubs/stockprice
- React Query: server state caching + background refresh
- Modal, Drawer, Toast, DataTable reusable components
- MSW mock handlers for dev without backend
- Target: 800 backend + 50+ frontend tests
