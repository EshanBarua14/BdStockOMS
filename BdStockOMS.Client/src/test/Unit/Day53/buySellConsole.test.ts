// @ts-nocheck
// src/test/Unit/Day53/buySellConsole.test.ts
// Day 53 — BuySellConsole unit tests

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { BuySellConsoleEvents } from '@/components/trading/BuySellConsole'

// ─── Event bus tests ──────────────────────────────────────────
describe('BuySellConsoleEvents', () => {
  it('exports open and close functions', () => {
    expect(typeof BuySellConsoleEvents.open).toBe('function')
    expect(typeof BuySellConsoleEvents.close).toBe('function')
  })

  it('open() accepts BUY side', () => {
    expect(() => BuySellConsoleEvents.open('BUY')).not.toThrow()
  })

  it('open() accepts SELL side', () => {
    expect(() => BuySellConsoleEvents.open('SELL')).not.toThrow()
  })

  it('open() accepts optional symbol', () => {
    expect(() => BuySellConsoleEvents.open('BUY', 'GP')).not.toThrow()
    expect(() => BuySellConsoleEvents.open('SELL', 'BATBC')).not.toThrow()
  })

  it('close() does not throw', () => {
    expect(() => BuySellConsoleEvents.close()).not.toThrow()
  })

  it('open() then close() sequence works', () => {
    expect(() => {
      BuySellConsoleEvents.open('BUY', 'SQURPHARMA')
      BuySellConsoleEvents.close()
    }).not.toThrow()
  })

  it('multiple open() calls do not throw', () => {
    expect(() => {
      BuySellConsoleEvents.open('BUY')
      BuySellConsoleEvents.open('SELL')
      BuySellConsoleEvents.open('BUY', 'GP')
    }).not.toThrow()
  })
})

// ─── Order value calculation ──────────────────────────────────
describe('Order value calculation logic', () => {
  const calcValue = (qty: number, price: number) => qty * price

  it('calculates correct value for whole numbers', () => {
    expect(calcValue(100, 50)).toBe(5000)
  })

  it('calculates correct value for decimal prices', () => {
    expect(calcValue(200, 312.50)).toBe(62500)
  })

  it('returns 0 for zero quantity', () => {
    expect(calcValue(0, 100)).toBe(0)
  })

  it('returns 0 for zero price', () => {
    expect(calcValue(100, 0)).toBe(0)
  })

  it('handles large BD stock values correctly', () => {
    // RENATA ~1420 BDT
    expect(calcValue(500, 1420)).toBe(710000)
  })

  it('handles minimum tick size values', () => {
    expect(calcValue(1000, 0.10)).toBeCloseTo(100, 2)
  })
})

// ─── Order validation logic ───────────────────────────────────
describe('Order validation logic', () => {
  const validate = (symbol: string, qty: number, orderType: string, price: number) => {
    if (!symbol || symbol.trim() === '') return { valid: false, msg: 'Symbol required' }
    if (!qty || qty <= 0) return { valid: false, msg: 'Quantity must be positive' }
    if (orderType !== 'Market' && (!price || price <= 0)) return { valid: false, msg: 'Price required' }
    return { valid: true, msg: null }
  }

  it('rejects empty symbol', () => {
    const r = validate('', 100, 'Limit', 50)
    expect(r.valid).toBe(false)
    expect(r.msg).toContain('Symbol')
  })

  it('rejects zero quantity', () => {
    const r = validate('GP', 0, 'Limit', 50)
    expect(r.valid).toBe(false)
    expect(r.msg).toContain('Quantity')
  })

  it('rejects negative quantity', () => {
    const r = validate('GP', -10, 'Limit', 50)
    expect(r.valid).toBe(false)
  })

  it('rejects Limit order with no price', () => {
    const r = validate('GP', 100, 'Limit', 0)
    expect(r.valid).toBe(false)
    expect(r.msg).toContain('Price')
  })

  it('rejects StopLoss order with no price', () => {
    const r = validate('GP', 100, 'StopLoss', 0)
    expect(r.valid).toBe(false)
  })

  it('accepts Market order with no price', () => {
    const r = validate('GP', 100, 'Market', 0)
    expect(r.valid).toBe(true)
  })

  it('accepts valid Limit order', () => {
    const r = validate('BATBC', 200, 'Limit', 716.50)
    expect(r.valid).toBe(true)
    expect(r.msg).toBeNull()
  })

  it('accepts valid Market order', () => {
    const r = validate('RENATA', 50, 'Market', 0)
    expect(r.valid).toBe(true)
  })
})

// ─── Order type mapping ───────────────────────────────────────
describe('Order type to API category mapping', () => {
  const mapOrderType = (type: string) => {
    if (type === 'Market')   return 0
    if (type === 'Limit')    return 1
    if (type === 'StopLoss') return 2
    return -1
  }

  const mapSide = (side: string) => side === 'BUY' ? 0 : 1

  it('maps Market to category 0', () => expect(mapOrderType('Market')).toBe(0))
  it('maps Limit to category 1',  () => expect(mapOrderType('Limit')).toBe(1))
  it('maps StopLoss to category 2', () => expect(mapOrderType('StopLoss')).toBe(2))
  it('maps unknown type to -1',   () => expect(mapOrderType('FOK')).toBe(-1))
  it('maps BUY side to 0',  () => expect(mapSide('BUY')).toBe(0))
  it('maps SELL side to 1', () => expect(mapSide('SELL')).toBe(1))
})

// ─── Symbol search filter ─────────────────────────────────────
describe('Symbol search filter logic', () => {
  const stocks = [
    { tradingCode: 'GP',         stockName: 'Grameenphone Ltd',    lastPrice: 310.20 },
    { tradingCode: 'BATBC',      stockName: 'BAT Bangladesh',       lastPrice: 716.00 },
    { tradingCode: 'SQURPHARMA', stockName: 'Square Pharmaceuticals', lastPrice: 312.50 },
    { tradingCode: 'RENATA',     stockName: 'Renata Ltd',           lastPrice: 1420.00 },
    { tradingCode: 'BRACBANK',   stockName: 'BRAC Bank Limited',    lastPrice: 51.20  },
  ]

  const filter = (q: string) => q.length < 1 ? [] : stocks.filter(s =>
    s.tradingCode.toUpperCase().startsWith(q.toUpperCase()) ||
    s.stockName.toUpperCase().includes(q.toUpperCase())
  ).slice(0, 8)

  it('returns empty array for empty query', () => {
    expect(filter('')).toHaveLength(0)
  })

  it('finds GP by exact code', () => {
    const r = filter('GP')
    expect(r.some(s => s.tradingCode === 'GP')).toBe(true)
  })

  it('finds by prefix match', () => {
    const r = filter('BAT')
    expect(r.some(s => s.tradingCode === 'BATBC')).toBe(true)
  })

  it('finds by company name substring', () => {
    const r = filter('Renata')
    expect(r.some(s => s.tradingCode === 'RENATA')).toBe(true)
  })

  it('is case-insensitive', () => {
    const r = filter('squr')
    expect(r.some(s => s.tradingCode === 'SQURPHARMA')).toBe(true)
  })

  it('returns max 8 results', () => {
    // All 5 match empty prefix — but we need at least 1 char
    const r = filter('A')
    expect(r.length).toBeLessThanOrEqual(8)
  })

  it('returns nothing for unmatched query', () => {
    expect(filter('ZZZZZ')).toHaveLength(0)
  })
})

// ─── Keyboard shortcut guard ──────────────────────────────────
describe('Keyboard shortcut input guard', () => {
  // Guard: skip if event target is INPUT or TEXTAREA
  const shouldHandleKey = (tagName: string) =>
    tagName !== 'INPUT' && tagName !== 'TEXTAREA'

  it('handles F1 outside inputs', () => expect(shouldHandleKey('DIV')).toBe(true))
  it('handles F1 on BODY',       () => expect(shouldHandleKey('BODY')).toBe(true))
  it('skips F1 inside INPUT',    () => expect(shouldHandleKey('INPUT')).toBe(false))
  it('skips F1 inside TEXTAREA', () => expect(shouldHandleKey('TEXTAREA')).toBe(false))
  it('handles F2 on BUTTON',     () => expect(shouldHandleKey('BUTTON')).toBe(true))
})
