// @ts-nocheck
// src/test/Unit/Day54/watchlist.test.ts
// Day 54 — Enhanced Watchlist unit tests

import { describe, it, expect } from 'vitest'

// ─── Column config ────────────────────────────────────────────
const ALL_COLUMNS = [
  "tradingCode","exchange","category","lastTradePrice","change","changePercent",
  "volume","value","tradeCount","highPrice","lowPrice","closePrice","openPrice",
  "ycp","w52High","w52Low","buyPressure","sellPressure","boardLotSize","companyName","sector","isin"
]
const DEFAULT_COLS = ["tradingCode","exchange","category","lastTradePrice","change","changePercent","volume","highPrice","lowPrice"]

describe('Column configuration', () => {
  it('default columns are a subset of all columns', () => {
    DEFAULT_COLS.forEach(c => expect(ALL_COLUMNS).toContain(c))
  })
  it('has 22 available columns', () => expect(ALL_COLUMNS).toHaveLength(22))
  it('has 9 default columns',    () => expect(DEFAULT_COLS).toHaveLength(9))
  it('tradingCode is always first default', () => expect(DEFAULT_COLS[0]).toBe('tradingCode'))
  it('toggle adds a column', () => {
    let cols = [...DEFAULT_COLS]
    const add = 'companyName'
    if (!cols.includes(add)) cols = [...cols, add]
    expect(cols).toContain('companyName')
  })
  it('toggle removes a column', () => {
    let cols = [...DEFAULT_COLS]
    cols = cols.filter(c => c !== 'volume')
    expect(cols).not.toContain('volume')
  })
  it('reorder moves column correctly', () => {
    const cols = ['tradingCode', 'exchange', 'category']
    const [moved] = cols.splice(2, 1)
    cols.splice(0, 0, moved)
    expect(cols[0]).toBe('category')
  })
})

// ─── getCellValue logic ───────────────────────────────────────
const getCellValue = (stock: any, key: string): any => {
  switch(key) {
    case 'lastTradePrice': return stock.lastTradePrice ?? stock.lastPrice ?? 0
    case 'change':         return stock.change ?? 0
    case 'changePercent':  return stock.changePercent ?? 0
    case 'volume':         return stock.volume ?? 0
    case 'value':          return stock.valueInMillionTaka ?? stock.value ?? 0
    case 'highPrice':      return stock.highPrice ?? 0
    case 'lowPrice':       return stock.lowPrice ?? 0
    case 'category':       return stock.category ?? 'A'
    default:               return stock[key] ?? ''
  }
}

describe('getCellValue', () => {
  const stock = {
    tradingCode: 'GP', lastTradePrice: 310.20, change: 1.5,
    changePercent: 0.49, volume: 1500000, valueInMillionTaka: 465.3,
    highPrice: 315.0, lowPrice: 308.0, category: 'A', exchange: 'DSE',
  }

  it('returns lastTradePrice',  () => expect(getCellValue(stock, 'lastTradePrice')).toBe(310.20))
  it('returns change',          () => expect(getCellValue(stock, 'change')).toBe(1.5))
  it('returns changePercent',   () => expect(getCellValue(stock, 'changePercent')).toBe(0.49))
  it('returns volume',          () => expect(getCellValue(stock, 'volume')).toBe(1500000))
  it('returns valueInMillionTaka', () => expect(getCellValue(stock, 'value')).toBe(465.3))
  it('returns category',        () => expect(getCellValue(stock, 'category')).toBe('A'))
  it('falls back lastPrice when no lastTradePrice', () => {
    expect(getCellValue({ lastPrice: 99.9 }, 'lastTradePrice')).toBe(99.9)
  })
  it('defaults category to A', () => {
    expect(getCellValue({}, 'category')).toBe('A')
  })
  it('returns 0 for missing numeric fields', () => {
    expect(getCellValue({}, 'change')).toBe(0)
    expect(getCellValue({}, 'volume')).toBe(0)
  })
})

// ─── Filter logic ─────────────────────────────────────────────
const applyFilter = (stocks: any[], filter: any) => {
  let s = stocks
  if (filter.tradedOnly) s = s.filter(x => (x.volume ?? 0) > 0)
  if (filter.spotOnly)   s = s.filter(x => x.category === 'Spot')
  if (filter.exchange)   s = s.filter(x => x.exchange === filter.exchange)
  if (filter.category)   s = s.filter(x => x.category === filter.category)
  if (filter.symbolQ)    s = s.filter(x => x.tradingCode?.startsWith(filter.symbolQ))
  return s
}

const mockStocks = [
  { tradingCode: 'GP',     exchange: 'DSE', category: 'A',    volume: 100000 },
  { tradingCode: 'BATBC',  exchange: 'DSE', category: 'A',    volume: 0      },
  { tradingCode: 'CSE01',  exchange: 'CSE', category: 'B',    volume: 50000  },
  { tradingCode: 'SPOT1',  exchange: 'DSE', category: 'Spot', volume: 10000  },
  { tradingCode: 'GPLIFE', exchange: 'DSE', category: 'N',    volume: 5000   },
]

describe('Filter logic', () => {
  it('no filter returns all',       () => expect(applyFilter(mockStocks, {})).toHaveLength(5))
  it('tradedOnly removes zero-vol', () => expect(applyFilter(mockStocks, { tradedOnly: true })).toHaveLength(4))
  it('spotOnly keeps only Spot',    () => expect(applyFilter(mockStocks, { spotOnly: true })).toHaveLength(1))
  it('exchange=CSE filters',        () => expect(applyFilter(mockStocks, { exchange: 'CSE' })).toHaveLength(1))
  it('category=A filters',          () => expect(applyFilter(mockStocks, { category: 'A' })).toHaveLength(2))
  it('symbolQ prefix filter',       () => expect(applyFilter(mockStocks, { symbolQ: 'GP' })).toHaveLength(2))
  it('combined filter',             () => expect(applyFilter(mockStocks, { exchange: 'DSE', category: 'A', tradedOnly: true })).toHaveLength(1))
  it('symbolQ is prefix not contains', () => {
    const r = applyFilter(mockStocks, { symbolQ: 'BATBC' })
    expect(r).toHaveLength(1)
    expect(r[0].tradingCode).toBe('BATBC')
  })
})

// ─── Sort logic ───────────────────────────────────────────────
const applySort = (stocks: any[], key: string | null, dir: 'asc' | 'desc') => {
  if (!key) return stocks
  return [...stocks].sort((a, b) => {
    const av = getCellValue(a, key)
    const bv = getCellValue(b, key)
    const cmp = typeof av === 'number' ? av - bv : String(av).localeCompare(String(bv))
    return dir === 'asc' ? cmp : -cmp
  })
}

describe('Sort logic', () => {
  const stocks = [
    { tradingCode: 'C', lastTradePrice: 300, volume: 1000, changePercent: -1.0 },
    { tradingCode: 'A', lastTradePrice: 100, volume: 3000, changePercent:  2.0 },
    { tradingCode: 'B', lastTradePrice: 200, volume: 2000, changePercent:  0.5 },
  ]

  it('no sort returns original order', () => {
    expect(applySort(stocks, null, 'asc').map(s => s.tradingCode)).toEqual(['C','A','B'])
  })
  it('sort by tradingCode asc', () => {
    expect(applySort(stocks, 'tradingCode', 'asc').map(s => s.tradingCode)).toEqual(['A','B','C'])
  })
  it('sort by tradingCode desc', () => {
    expect(applySort(stocks, 'tradingCode', 'desc').map(s => s.tradingCode)).toEqual(['C','B','A'])
  })
  it('sort by price asc', () => {
    expect(applySort(stocks, 'lastTradePrice', 'asc').map(s => s.lastTradePrice)).toEqual([100,200,300])
  })
  it('sort by price desc', () => {
    expect(applySort(stocks, 'lastTradePrice', 'desc').map(s => s.lastTradePrice)).toEqual([300,200,100])
  })
  it('sort by volume desc', () => {
    expect(applySort(stocks, 'volume', 'desc').map(s => s.volume)).toEqual([3000,2000,1000])
  })
  it('sort by changePercent asc puts negatives first', () => {
    const sorted = applySort(stocks, 'changePercent', 'asc')
    expect(sorted[0].changePercent).toBe(-1.0)
  })
})

// ─── Live price merge ─────────────────────────────────────────
describe('Live price merge', () => {
  const base = [
    { stockId: 1, tradingCode: 'GP',    lastTradePrice: 305.0, changePercent: 0.1 },
    { stockId: 2, tradingCode: 'BATBC', lastTradePrice: 710.0, changePercent: 0.2 },
  ]
  const ticks = [
    { tradingCode: 'GP', lastPrice: 312.5, changePercent: 0.8, volume: 200000 },
  ]

  const merge = (base: any[], ticks: any[]) => base.map(s => {
    const live = ticks.find(t => t.tradingCode === s.tradingCode)
    return live ? { ...s, ...live, stockId: s.stockId, tradingCode: s.tradingCode } : s
  })

  it('merges live tick data onto base stock', () => {
    const merged = merge(base, ticks)
    expect(merged[0].lastPrice).toBe(312.5)
    expect(merged[0].volume).toBe(200000)
  })
  it('preserves stockId from base after merge', () => {
    const merged = merge(base, ticks)
    expect(merged[0].stockId).toBe(1)
  })
  it('preserves tradingCode from base after merge', () => {
    const merged = merge(base, ticks)
    expect(merged[0].tradingCode).toBe('GP')
  })
  it('unmatched stock keeps base data', () => {
    const merged = merge(base, ticks)
    expect(merged[1].lastTradePrice).toBe(710.0)
    expect(merged[1].volume).toBeUndefined()
  })
  it('handles empty ticks array', () => {
    const merged = merge(base, [])
    expect(merged[0].lastTradePrice).toBe(305.0)
  })
})

// ─── Format helpers ───────────────────────────────────────────
const fmtVol = (v: number) => v >= 1e6 ? `${(v/1e6).toFixed(1)}M` : v >= 1e3 ? `${(v/1e3).toFixed(0)}K` : String(v || '—')
const fmtPrice = (v: number) => v > 0 ? `৳${v.toFixed(2)}` : '—'

describe('Format helpers', () => {
  it('fmtVol formats millions', () => expect(fmtVol(1500000)).toBe('1.5M'))
  it('fmtVol formats thousands', () => expect(fmtVol(5000)).toBe('5K'))
  it('fmtVol formats small', () => expect(fmtVol(500)).toBe('500'))
  it('fmtVol handles zero', () => expect(fmtVol(0)).toBe('—'))
  it('fmtPrice formats correctly', () => expect(fmtPrice(310.2)).toBe('৳310.20'))
  it('fmtPrice handles zero', () => expect(fmtPrice(0)).toBe('—'))
})
