# BD Stock OMS — Master Analysis & Implementation Blueprint
## Deep Analysis of XFL OMS (Xpert Trading) Reference + Feature Gap + Enhancement Plan

---

## PART 1: REFERENCE SYSTEM ANALYSIS (XFL OMS / Xpert Trading)

### 1.1 Role-Based Architecture (3 Handbooks)

| Role | Sidebar Modules | Key Capabilities |
|------|----------------|------------------|
| **IT Admin / Broker Admin** | Dashboard, Broker, Trade Monitor, User, Branch, Back Office, BO Account, Accounts, Risk Management, User Activities, Reports, App Settings, Settings, About Us, Logout | Full system control, user provisioning, BO import/export, risk limits (6 entity levels), all reports |
| **Trader / Dealer** | Dashboard (24 widgets), Broker, Trade Monitor, User, BO Account, Accounts, Reports, Settings, About Us, Logout | Trade execution (F1/F2), POD trade, all 24 dashboard widgets, layout management, client management |
| **Client / Investor** | Dashboard (23 widgets — no Price Scroll), Broker, User, BO Account, Accounts, Settings, About Us, Logout | Self-service trading, portfolio view, deposit/withdrawal/IPO requests, limited reports |

### 1.2 Dashboard Widgets (Reference — 24 Total)

| # | Widget | Key Columns/Features |
|---|--------|---------------------|
| 1 | **Buy/Sell Console (F1/F2)** | BO Code, Exchange (DSE/CSE), Market (Public/SME/ATB/GOV), Symbol, Qty, Price Type (Limit/Market/Market at Best), Display Qty, Time in Force (Day/IOC/FOK), Min Qty, Private Order |
| 2 | **POD Trade** | Percentage-of-Daily trade, % filter, auto-calculate qty from purchase power |
| 3 | **Market Depth** | Bid/Ask volumes, prices, Time, Split, Cum Qty, Yield, column customization, row management, symbol linking |
| 4 | **Portfolio** | Trading Code, Cat, Board, LTP, Change, Change%, BO Qty, Position, Matured, Salable, Avg Rate, Total Purchase Price, Profit, Profit%, Sell Value, filter by BO Account |
| 5 | **Order List** | 50+ columns, Status, Edit, Cancel, Order Rejection Reason, all order types, filter by Order ID/BO Code/BO Group/Status/Type/Board/Market/Trading Code |
| 6 | **Execution Report** | 40+ columns, filter Executed Only, Exec Type, Trade Match ID, Aggressor Indicator |
| 7 | **Order Summary** | Side-wise (Total/Buy/Sell/Net), Order Count, Order Qty, Executed Value/Qty, Execution Value% |
| 8 | **Share Price / Watchlist** | 30+ columns, Custom Watchlists (create/manage/add symbols), Right-click context menu (Buy/Sell/Active Orders/Market Depth/Time & Sales/Company Info/TA Chart/Minute Chart/News) |
| 9 | **Score Board** | Sector-wise: Gainer, Loser, Unchanged, Not Traded, Value, Volume, Trade |
| 10 | **Top Gainer** | Top 20, sortable by Change%/Value/Volume/Trade, 20+ columns |
| 11 | **Top Loser** | Top 20, same columns as Top Gainer |
| 12 | **Most Active** | Parameter-based (Change%/Value/Volume/Trade) |
| 13 | **Time and Sales** | Chronological trade log, Buy/Sell Pressure indicators |
| 14 | **News** | Live feed, filter by keyword/Board/Trading Code/Sector/Category |
| 15 | **TA Chart** | Multi-timeframe (1min to 6mo), symbol selection, screenshot, module linking |
| 16 | **Fundamental** | Company Info, EPS, Profit, Shareholding (current+history), P/E History, NAV History, Dividend History, Quarter data |
| 17 | **Minute Chart** | Short-term charting, symbol/board/market selection |
| 18 | **Price History** | Date/Time, OHLC, YTP, LTP, Change, Change%, Trade, Volume, Value, timeframe selection |
| 19 | **Market Trade Info** | Aggregated market-wide trading statistics |
| 20 | **Top Sectors by Gainer** | Sector heatmap by gain%, export SVG/PNG/CSV |
| 21 | **Top Sectors by Value/Category** | Capital-weighted sector view |
| 22 | **Index Summary** | Index, Value, Change, Change%, Trade, Volume |
| 23 | **Market Map** | Visual heatmap of entire equity market, color-coded tiles, filter by Board/Market/Value/% Change |
| 24 | **Price Scroll (Ticker)** | Scrolling ticker tape, alphabetical or last trade order, watchlist filter, scroll direction (L→R / R→L), pause, zoom |

### 1.3 Dashboard Layout Features (Reference)

- **Multi-tab sheets** — Create/rename/delete/reorder sheets with custom names + icons
- **Edit Mode** — Toggle edit mode to add/remove/rearrange modules
- **Save Sheet** — Persist layout
- **Export/Import** — Dashboard configuration as JSON
- **Trade Settings** — Default buy/sell behaviors
- **Notification Settings** — Choose which alerts to receive
- **Dashboard Settings** — Save preferences, export/import configs

### 1.4 Back Office Module (Admin Only)

- **Import**: BO Account Info (XML), Portfolio Data (XML)
- **Export**: DSE EOD Ticker (XML), Broker Trade (XML)
- **XW Trade Data Import**: XW Trade File (XML)
- **File History Table**: View, File Name, Uploaded By, Date, Status, Processing Mode, Duration, Records, Success/Wrong Count, Affected Accounts

### 1.5 Risk Management Module (Admin Only — 6 Entity Levels)

Each entity has 8 sub-tabs:
1. Credit Limit (Cash Limit, Purchase Power, Market-specific: Main/ATB/SC)
2. Board Limit (Market Segments × Order Types × Transaction Types)
3. Trading Code Limit (per-code Buy/Sell/Total/Net with consumed/remaining)
4. Trading Code Allowed (Buy/Sell toggles per code)
5. Sector Limit
6. Sector Allowed
7. Category Limit
8. Category Allowed

**Entities**: Client, User, BO Group, Basket, Branch, Broker

### 1.6 Reports Module

| Report | Filters | Key Columns |
|--------|---------|-------------|
| Client Detailed Execution | Date, Trader, Trading Code, BO Account | Client ID, Ticker, Initial Position, Ordered Qty, Avg Price, Order ID, Exec State, Resulting Position |
| Client Executions by Ticker | Same | Client ID, Ticker, Initial Position, Net Ordered Qty, Net Executed Qty |
| Client Limit & Executions | Same | Client ID, Name, Currency, Cash Initial Limit, Bought, Sold, Total |
| Client Trading | Same | Client ID, Buy/Sell Amt/Qty, Total, Net |
| Dealer Executions by Client | Same | Dealer ID, Client ID, Ticker, Market, Time, Qty, Price, Order ID, Exec ID |
| Dealer Executions by Instrument | Same | Same as above |
| Dealer Trading | Same | Dealer ID, Buy/Sell Amt/Qty, Total, Net |
| Ticker Detailed Executions | Same | Ticker, Client ID, Positions, Qty, Price, State |
| Ticker Trading | Same | Ticker, Buy/Sell Amt/Qty, Total, Net |
| Daily Trade Summary | Same | Date, Trader/Client/Total: Amt, Qty, Trades |

### 1.7 Accounts Module (All Roles)

| Sub-Module | Key Features |
|-----------|-------------|
| **Deposit** | Submit → Process → Approve workflow, Amount, Approved Amount, Status, Slip, Transaction ID, Customer/Review/Admin Notes |
| **Withdrawal** | Same workflow, Admin can adjust approved amount |
| **Purchase Power** | Adjust Medium (Cash/Cheque/Online Transfer/Sell Share) |
| **IPO** | Submit → Process → Approve, IPO Name, Lot |
| **Forthcoming IPO** | CRUD, Activate/Deactivate, Opening/Closing dates, Multiple Lot, Summary/Prospect URLs |
| **Treasury Bond** | Submit → Process → Approve, Tenure, ISIN |
| **Forthcoming T-Bond** | CRUD, Activate/Deactivate, same as Forthcoming IPO pattern |

---

## PART 2: COMPARISON FILE ANALYSIS (Xpert Trading vs qTrader)

Key requirements from the comparison spreadsheet:

1. **Multi-Market** (DSE + CSE) — ✅ We have this via SignalR
2. **Multi-Currency** — Need to support BDT + cross-currency
3. **Market Watch Lists** — Multiple exchange, currency, asset class, Board/Category/PE/NAV/VWAP, Buy/Sell Pressure, Free Float Shares
4. **Shortcut Key Setup** — F1/F2 Buy/Sell, customizable shortcuts
5. **Client Info** — ID, Name, Share Position, Immature/Mature Balance, IPO Status, Dividend Status, ED Ratio, Deposit/Withdrawal
6. **Conditional Orders** — Basket, Conditional, OCA, Spreads, Volatility
7. **Block Transaction Info** — % of Turnover, filtering by Stock/Sector/Date
8. **Index Contribution & Correlation** — Date range, Stock, Sector
9. **Market Radar** — Sector, Paid-up Capital Range, Market Cap Range, Free-float Market Cap, Category, Share Price, Shareholding
10. **International Market Index** — NYSE, NASDAQ, KOSPI
11. **Sector Index** — Standard setup
12. **BD Equity Factor** — Industry Details (Large/Mid/Small/Value/Growth)
13. **What-If Calculator** — Scenario analysis
14. **Charting** — Average Cost Calculator, Bond Calculator, Yield Calculator
15. **Margin/Custodian Limits** — Upload via template/portal, confirmation emails
16. **Real-Time Risk Exposure** — Margin, DVP, client/advisor/security wise
17. **Algorithmic & Conditional Orders** — Advanced Order Management, Basket Orders
18. **Research Reports** — Dashboard for economic indicators, Money Market, Commodity, Currency
19. **Mutual Fund Dashboard** — NAV, Dividend, Fund/Manager Performance, Trend
20. **Research Data Feed** — API (DSE/CSE), Research DB, Websites, Excel

---

## PART 3: WHAT WE HAVE vs WHAT WE NEED

### ✅ Already Implemented (Day 50-51)
- Sidebar with RBAC nav, glass aesthetic, neon accents
- Topbar with DSE+CSE indexes, BD market sessions, SignalR status, BDT clock, search (Ctrl+K)
- ThemeMenu (14 themes, 4-tab drawer)
- Login/SignUp pages with theme integration
- DashboardPage with glass top bar, preset pills, widget picker
- WidgetPanel glass panels
- 16 registered widgets in registry.tsx
- SignalR for stock prices (16 stocks) and notifications
- Zustand persist for themes

### ❌ Missing / Needs Implementation

#### A. Dashboard Infrastructure (Priority 1 — Day 52-54)
1. **Multi-page dashboard tabs** (create/rename/delete/reorder sheets with icons)
2. **Widget toolbar** (drag-and-drop widget bar below topbar)
3. **Template system** (save/load/export/import multiple named templates via Zustand persist)
4. **Edit mode toggle** (add/remove/rearrange widgets)
5. **Price Scroll/Ticker** (DSE+CSE, customizable, placed under topbar)

#### B. Core Trading Widgets (Priority 2 — Day 55-60)
1. **Buy/Sell Console** (F1/F2 shortcut, hover effect, full order form)
2. **POD Trade** (percentage-based order placement)
3. **Market Depth** (real-time bid/ask book)
4. **Portfolio** (full column set with profit/loss)
5. **Order List** (50+ columns, edit/cancel, full filtering)
6. **Execution Report** (40+ columns)
7. **Order Summary** (aggregated metrics)
8. **Share Price / Watchlist** (30+ columns, custom watchlists, right-click context menu)

#### C. Market Analytics Widgets (Priority 3 — Day 61-65)
1. **Score Board** (sector-wise breakdown)
2. **Top Gainer / Top Loser / Most Active** (parameter-based ranking)
3. **Time and Sales** (trade log with pressure indicators)
4. **News** (live feed with filters)
5. **TA Chart** (multi-timeframe technical analysis)
6. **Fundamental** (company financials)
7. **Minute Chart** (intraday charting)
8. **Price History** (OHLC data table)
9. **Market Trade Info** (aggregated stats)
10. **Top Sectors by Gainer / Value** (sector analytics)
11. **Index Summary** (multi-index view)
12. **Market Map** (heatmap visualization)

#### D. Admin Modules (Priority 4 — Day 66-72)
1. **Back Office** (BO import/export, file history)
2. **Risk Management** (6 entities × 8 limit types = 48 config screens)
3. **User Management** (full CRUD, role assignment, BO linking)
4. **Branch Management** (CRUD)
5. **Broker Management** (CRUD)
6. **User Activities / Audit Trail** (full logging)

#### E. Accounts Module (Priority 5 — Day 73-76)
1. **Deposit** (submit → process → approve workflow)
2. **Withdrawal** (same workflow)
3. **Purchase Power** (adjust medium)
4. **IPO / Forthcoming IPO** (CRUD + workflow)
5. **Treasury Bond / Forthcoming T-Bond** (CRUD + workflow)

#### F. Reports Module (Priority 6 — Day 77-80)
1. All 10 report types with filters and PDF/Excel export
2. Client, Dealer, Ticker, and Daily Summary reports

#### G. Advanced Features (Priority 7 — Day 81-90)
1. **Conditional/Algorithmic Orders** (Basket, OCA, Spreads)
2. **What-If Calculator**
3. **Bond/Yield Calculator**
4. **Market Radar** (multi-parameter screening)
5. **International Market Index**
6. **Research Dashboard** (economic indicators)
7. **Mutual Fund Dashboard**
8. **Notification Settings** (granular control)
9. **Trade Settings** (default order behaviors)

---

## PART 4: OUR ENHANCEMENTS OVER REFERENCE

### What makes BD Stock OMS *better* than XFL Xpert Trading:

1. **14-Theme System** with custom accent/buy/sell colors (they have basic light/dark toggle)
2. **Glass morphism UI** with neon accents (they have standard corporate UI)
3. **SignalR real-time** everywhere (they use polling with 10-sec refresh)
4. **Multi-page dashboard with template save/load/export/import** (they have basic sheet management)
5. **Ctrl+K command palette** for power users (they have basic menu)
6. **CSS variable theming** end-to-end (they have limited theme support)
7. **Advanced widget customization** — drag resize, column reorder, persistent state per widget
8. **Real-time notifications via SignalR NotificationHub** (they use basic pop-ups)
9. **Price Scroll with full DSE+CSE customization** including speed, direction, watchlist filter, pause
10. **Responsive design** — desktop + tablet + mobile (they appear desktop-only)
11. **Better search** — global search across all entities, not just per-module
12. **Export/Import shareable templates** — share dashboard layouts between users
13. **Widget linking** — click stock in watchlist → auto-updates Market Depth, Chart, Fundamental, etc.
14. **Keyboard-first UX** — F1/F2 for buy/sell, Ctrl+K search, Tab navigation, Esc to close

---

## PART 5: DAY 52 IMPLEMENTATION PLAN

### Day 52 Focus: Dashboard Template System + Widget Bar + Multi-Page Tabs + Price Ticker

This is the foundation day. We build:

1. **`useTemplateStore.ts`** — Zustand persist store for multiple named templates
2. **`WidgetBar.tsx`** — Horizontal widget toolbar (drag/select widgets)
3. **`DashboardTabs.tsx`** — Multi-page tab system (create/rename/delete/reorder)
4. **`PriceTicker.tsx`** — Scrolling price ticker under topbar (DSE+CSE)
5. **`TemplateManager.tsx`** — Save/Load/Export/Import UI in settings drawer
6. **Enhanced `DashboardPage.tsx`** — Integrate all above components
7. **Update `registry.tsx`** — Add new widget entries for all 24 reference widgets

### Files to Create/Modify:
```
src/
  stores/
    useTemplateStore.ts          ← NEW: Template persistence
  components/
    dashboard/
      WidgetBar.tsx              ← NEW: Widget toolbar
      DashboardTabs.tsx          ← NEW: Multi-page tabs
      PriceTicker.tsx            ← NEW: Price scroll
      TemplateManager.tsx        ← NEW: Template CRUD UI
  pages/
    DashboardPage.tsx            ← MODIFY: Integrate new components
  config/
    registry.tsx                 ← MODIFY: Add all 24 widget types
```

---

## PART 6: SIDEBAR MENU STRUCTURE (Admin View — Complete)

Based on reference + our enhancements:

```
📊 Dashboard
🧾 Broker
📈 Trade Monitor
👤 User Management
🏢 Branch Management
🗂️ Back Office
  ├── BO Account Info (Import)
  ├── Portfolio Data (Import)
  ├── DSE EOD Ticker (Export)
  ├── Broker Trade (Export)
  └── XW Trade Data (Import)
🗃️ BO Account
💼 Accounts
  ├── Deposit
  ├── Withdrawal
  ├── Purchase Power
  ├── IPO
  ├── Forthcoming IPO
  ├── Treasury Bond
  └── Forthcoming T-Bond
🛡️ Risk Management
  ├── Client
  ├── User
  ├── BO Group
  ├── Basket
  ├── Branch
  └── Broker
📋 User Activities
📑 Reports
  ├── Client Reports
  ├── Dealer Reports
  ├── System Reports
  └── Market Reports
🛠️ App Settings
⚙️ Settings
🏢 About Us
🚪 Logout
```
