# Day 08 — Order Management APIs + Investor-Trader Assignment

## What Was Built

### Order APIs
- `POST /api/orders` — Investor or Trader places an order
- `GET /api/orders` — View orders (role scoped)
- `GET /api/orders/{id}` — View single order
- `PUT /api/orders/{id}/execute` — Trader executes a pending order
- `PUT /api/orders/{id}/cancel` — Cancel a pending order
- `GET /api/orders/portfolio/{investorId}` — View investor portfolio

### Trader-Investor Assignment APIs
- `POST /api/traders/{traderId}/investors/{investorId}` — Admin assigns investor to trader
- `DELETE /api/traders/{traderId}/investors/{investorId}` — Admin removes investor from trader
- `GET /api/traders/{traderId}/investors` — List assigned investors

### Order Validation Rules (Real DSE/CSE)
- BO account must be active
- BUY: sufficient cash or cash+margin
- BUY Z/Spot category: cash only, no margin
- SELL: must own enough shares in portfolio
- Price must be within circuit breaker range
- Quantity must meet board lot size
- Limit orders must specify LimitPrice
- Trader can only place for assigned investors

### Fund Management
- BUY order placed → funds reserved immediately
- BUY order cancelled → funds refunded automatically
- Z/Spot → T+0 settlement
- A/B/G/N → T+2 settlement

## New Files
- DTOs/Order/PlaceOrderDto.cs
- DTOs/Order/OrderResponseDto.cs
- DTOs/Order/CancelOrderDto.cs
- DTOs/Order/PortfolioResponseDto.cs
- Services/OrderService.cs
- Controllers/OrderController.cs
- Controllers/TraderInvestorController.cs
- Tests/Unit/OrderServiceTests.cs

## Tests
- Previous: 34 passing
- New: +10 OrderServiceTests
- **Total: 44 passing, 0 failing**

## Next: Day 09
- CCD APIs (BO account management, cash deposit, margin setting)
- SignalR real-time stock price updates
- Portfolio auto-update after order execution
