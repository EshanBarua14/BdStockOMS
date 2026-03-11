# Day 49 — React Frontend Scaffold

**Branch:** `day-49-react-frontend`  
**Date:** 2025 (Day 49 of BD Stock OMS development)

---

## Goals Achieved

| Goal | Status |
|------|--------|
| Vite + React 18 + TypeScript scaffold in `/BdStockOMS.Client` | ✅ |
| Tailwind CSS with custom dark theme | ✅ |
| Axios API client with JWT interceptors + auto-refresh | ✅ |
| Zustand auth store with persistence | ✅ |
| Login page → `/api/auth/login` | ✅ |
| Dashboard layout (collapsible sidebar + topbar) | ✅ |
| Dashboard page with KPI cards + recent orders | ✅ |
| Protected routes (redirect to `/login` if unauthenticated) | ✅ |
| Role-based route guards | ✅ |
| SignalR hook for real-time streaming | ✅ |
| Frontend tests (22 tests) | ✅ |
| Backend: 776 tests still passing | ✅ |

---

## Project Structure

```
BdStockOMS.Client/
├── index.html
├── vite.config.ts              # Proxy: /api → :7001, /hubs → ws
├── tailwind.config.js          # Custom brand palette + animations
├── tsconfig.json               # Strict mode
├── package.json                # React 18, Vite 5, Axios, Zustand, MSW
└── src/
    ├── main.tsx
    ├── App.tsx                 # Router with protected/admin routes
    ├── index.css               # Tailwind + global component classes
    ├── api/
    │   ├── client.ts           # Axios instance + JWT interceptors + refresh
    │   ├── auth.ts             # authApi (login/refresh/logout/me)
    │   └── orders.ts           # ordersApi (list/get/place/cancel)
    ├── store/
    │   └── authStore.ts        # Zustand store with localStorage persist
    ├── hooks/
    │   ├── useAuth.ts          # login/logout/hasRole/hasPermission
    │   └── useSignalR.ts       # SignalR hub connection hook
    ├── types/
    │   └── index.ts            # All shared TypeScript types
    ├── components/
    │   ├── auth/
    │   │   └── ProtectedRoute.tsx   # JWT-gated + role-gated route wrapper
    │   ├── layout/
    │   │   ├── DashboardLayout.tsx  # Sidebar + Topbar + <Outlet>
    │   │   ├── Sidebar.tsx          # Collapsible nav + user info + logout
    │   │   └── Topbar.tsx           # Breadcrumbs + market status + bell
    │   └── ui/
    │       ├── Spinner.tsx     # Spinner + PageSpinner
    │       └── Alert.tsx       # Error/success/warning/info alerts
    ├── pages/
    │   ├── LoginPage.tsx       # Full login UI → /api/auth/login
    │   ├── DashboardPage.tsx   # KPI cards, recent orders, quick actions
    │   └── PlaceholderPages.tsx # Orders, Portfolio, Market, 403, 404
    └── test/
        ├── setup.ts
        ├── authStore.test.ts      # 6 tests
        ├── ProtectedRoute.test.tsx # 5 tests
        ├── LoginPage.test.tsx     # 7 tests
        ├── Alert.test.tsx         # 7 tests
        └── types.test.ts          # 6 tests (type shape tests)
```

---

## JWT Interceptor Design

### Proactive Refresh (Request Interceptor)
- If `expiresAt - now < 60s` → refresh **before** sending request
- All concurrent requests queue behind a single refresh promise
- No duplicate refresh calls via `isRefreshing` flag

### Reactive Refresh (Response Interceptor)
- 401 response → refresh once (`_retry` flag prevents loops)
- Queued requests resolved with new token after refresh
- On refresh failure → `logout()` + redirect to `/login`

---

## Auth Store

```typescript
// Zustand slice
{
  user: AuthUser | null      // null = unauthenticated
  isAuthenticated: boolean
  setUser(user: AuthUser): void
  logout(): void
}

// Persisted to localStorage key: "bd_oms_auth"
// (In production, use httpOnly cookies for tokens)
```

---

## Protected Route Logic

```
/any-protected-path
  → ProtectedRoute
      ├── !isAuthenticated → <Navigate to="/login" />
      ├── role not in allowedRoles → <Navigate to="/forbidden" />
      └── ✅ render <Outlet />
```

---

## Tailwind Design System

| Token | Value |
|-------|-------|
| `surface.DEFAULT` | `#0f1117` — page background |
| `surface.card` | `#161b27` — card/sidebar background |
| `surface.border` | `#1e2535` — borders |
| `brand.600` | `#1464f5` — primary CTA |
| `success` | `#22c55e` — buy / profit |
| `danger` | `#ef4444` — sell / loss |
| Font | DM Sans (UI) + JetBrains Mono (numbers) |

---

## Vite Proxy Config

```typescript
proxy: {
  '/api':  { target: 'https://localhost:7001', secure: false },
  '/hubs': { target: 'https://localhost:7001', secure: false, ws: true },
}
```

---

## Frontend Tests (22 total)

```
src/test/authStore.test.ts         6 tests  — store init, setUser, logout, idempotency
src/test/ProtectedRoute.test.tsx   5 tests  — unauth redirect, role guard, SuperAdmin
src/test/LoginPage.test.tsx        7 tests  — render, disabled state, API call, error
src/test/Alert.test.tsx            7 tests  — variants, dismiss, title, role
src/test/types.test.ts             6 tests  — runtime type shape assertions
```

Run: `cd BdStockOMS.Client && npm test`

---

## Dev Commands

```bash
cd BdStockOMS.Client

# Install dependencies
npm install

# Dev server (proxies API to :7001)
npm run dev

# Type check
npx tsc --noEmit

# Lint
npm run lint

# Run frontend tests
npm test

# Build for production
npm run build
```

---

## Day 50 Plan

- Orders page: place/cancel order form with validation
- Portfolio page: holdings table + P&L breakdown
- Market watch: SignalR streaming ticker
- React Query / SWR for server state caching
- MSW mocks for all API endpoints
- E2E test setup (Playwright)
