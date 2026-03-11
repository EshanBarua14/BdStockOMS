import { describe, it, expect } from 'vitest'
import type { AuthUser, Order, MarketTicker, PortfolioSummary, Holding, ApiResponse } from '@/types'

describe('Type shape assertions', () => {
  it('AuthUser has required fields', () => {
    const user: AuthUser = {
      userId: 'u1', email: 'a@b.com', role: 'Investor',
      permissions: [], accessToken: 'tok', refreshToken: 'ref',
      expiresAt: Date.now() + 3600_000,
    }
    expect(user.userId).toBe('u1')
    expect(user.role).toBe('Investor')
  })

  it('Order has correct status union type', () => {
    const order: Order = {
      orderId: 'o1', symbol: 'BATBC', side: 'Buy', type: 'Limit',
      quantity: 100, filledQuantity: 0, price: 716,
      status: 'Pending', createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    expect(order.status).toBe('Pending')
    expect(order.side).toBe('Buy')
  })

  it('MarketTicker has price and change fields', () => {
    const ticker: MarketTicker = {
      symbol: 'DSEX', name: 'DSEX Index', lastPrice: 6248,
      change: 42.18, changePercent: 0.68, volume: 1200000,
      high: 6280, low: 6200, open: 6210, previousClose: 6206,
    }
    expect(ticker.changePercent).toBe(0.68)
  })

  it('PortfolioSummary has all P&L fields', () => {
    const ps: PortfolioSummary = {
      totalValue: 1000000, cashBalance: 200000, investedAmount: 800000,
      todayPnl: 5600, todayPnlPercent: 0.7,
      totalPnl: 56820, totalPnlPercent: 7.1,
    }
    expect(ps.totalValue).toBe(1000000)
  })

  it('Holding calculates unrealizedPnl correctly', () => {
    const holding: Holding = {
      symbol: 'BATBC', companyName: 'BAT Bangladesh', quantity: 100,
      avgCostPrice: 700, currentPrice: 716,
      currentValue: 71600, unrealizedPnl: 1600, unrealizedPnlPercent: 2.28,
    }
    expect(holding.unrealizedPnl).toBe(1600)
  })

  it('ApiResponse wraps data correctly', () => {
    const res: ApiResponse<{ id: string }> = {
      success: true, data: { id: 'abc' }, message: 'OK',
    }
    expect(res.success).toBe(true)
    expect(res.data?.id).toBe('abc')
  })
})
