# Day 50 — BD Stock OMS Dev Log
Date: 2026-03-12

## Fixed
- auth.ts: added getBrokerages, registerBrokerage exports
- client.ts: fixed token() to read bd_oms_auth_v2 from Zustand persist store
- useMarketData.ts: removed broken /api/Helth fetch, replaced with local BST time inference
- All widgets: null-safe destructuring (stocks ?? [], orders ?? [])
- vite.config.ts: proxy target changed to localhost (IPv6 fix)
- index.html CSP: added connect-src, worker-src blob:, unsafe-eval
- Program.cs: added NotificationHub mapping at /hubs/notification
- GridLayout import fix in DashboardPage
- WidgetErrorBoundary added to DashboardPage

## Known Issues
- Vite 7 proxy unreliable on Windows for /api/* routes
- /hubs/notification still 401 (JWT query param auth needed)
- Only news + index widgets showing data — others need re-login + 401 investigation

## Next Session (Day 51)
- Premium Bloomberg/Refinitiv-style UI redesign
- Glassmorphism, 3D depth, neon accents
- DSE+CSE exchange dropdown on every widget
- Full widget suite with AI sentiment, scanner, alerts
- Tests for all new components

## Status
- npm run build ✅ 0 errors
- Backend running on localhost:5289 ✅
- 16 stocks updating every 2s ✅
