// src/hooks/useMarketData.ts
// Fixed: live BulkPriceUpdate merge, market status from /api/helth, BST fallback

import { useState, useEffect, useRef, useCallback } from "react"
import { getMarketData } from "../api/market"
import { subscribeMarket } from "./useSignalR"

export interface StockTick {
  stockId: number
  tradingCode: string
  stockName?: string
  companyName?: string
  lastPrice: number
  change: number
  changePercent: number
  volume: number
  turnover?: number
  openPrice?: number
  highPrice?: number
  lowPrice?: number
  closePrice?: number
  ycp?: number
  category?: string        // A | B | G | N | Z | Spot
  exchange?: string        // DSE | CSE
  circuitBreakerHigh?: number
  circuitBreakerLow?: number
  boardLotSize?: number
  marketCap?: number
  pe?: number
}

export interface MarketStatus {
  isOpen: boolean
  label: string
  activeStocks: number
}

export function normaliseMarketData(raw: any): StockTick[] {
  if (!raw) return []
  if (Array.isArray(raw)) return raw.map(normaliseStock)
  if (raw.items && Array.isArray(raw.items)) return raw.items.map(normaliseStock)
  if (raw.data && Array.isArray(raw.data)) return raw.data.map(normaliseStock)
  if (typeof raw === "object") return Object.values(raw).map(normaliseStock)
  return []
}

function normaliseStock(s: any): StockTick {
  return {
    stockId:            s.id ?? s.stockId ?? 0,
    tradingCode:        s.tradingCode ?? s.TradingCode ?? '',
    stockName:          s.companyName ?? s.stockName ?? s.CompanyName ?? '',
    companyName:        s.companyName ?? s.CompanyName ?? '',
    lastPrice:          s.lastTradePrice ?? s.lastPrice ?? s.LastTradePrice ?? 0,
    change:             s.change ?? s.Change ?? 0,
    changePercent:      s.changePercent ?? s.ChangePercent ?? 0,
    volume:             s.volume ?? s.Volume ?? 0,
    highPrice:          s.highPrice ?? s.HighPrice ?? 0,
    lowPrice:           s.lowPrice ?? s.LowPrice ?? 0,
    closePrice:         s.closePrice ?? s.ClosePrice ?? 0,
    openPrice:          s.openPrice ?? s.OpenPrice ?? 0,
    exchange:           s.exchange ?? s.Exchange ?? '',
    category:           s.category ?? s.Category ?? '',
    circuitBreakerHigh: s.circuitBreakerHigh ?? s.CircuitBreakerHigh ?? 0,
    circuitBreakerLow:  s.circuitBreakerLow  ?? s.CircuitBreakerLow  ?? 0,
    boardLotSize:       s.boardLotSize ?? s.BoardLotSize ?? 1,
  }
}

function inferMarketStatus(raw?: any): MarketStatus {
  if (raw?.marketStatus !== undefined) {
    const s = String(raw.marketStatus).toLowerCase()
    const isOpen = s === "open" || s === "1" || s === "true"
    return { isOpen, label: isOpen ? "Market Open" : "Market Closed", activeStocks: raw.activeStocks ?? 0 }
  }
  // Fallback: DSE hours Sun–Thu 10:00–14:30 BST (UTC+6)
  const now = new Date()
  const bst = (now.getUTCHours() + 6) % 24
  const min = now.getUTCMinutes()
  const t = bst * 60 + min
  const isWeekday = now.getUTCDay() !== 5 && now.getUTCDay() !== 6  // not Fri/Sat
  const isOpen = isWeekday && t >= 600 && t <= 870
  return { isOpen, label: isOpen ? "Market Open" : "Market Closed", activeStocks: 0 }
}

export function useMarketData() {
  const [ticks, setTicks] = useState<Map<string, StockTick>>(new Map())
  const [marketStatus, setMarketStatus] = useState<MarketStatus>({ isOpen: false, label: "Checking…", activeStocks: 0 })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const mountedRef = useRef(true)

  const fetchStatus = useCallback(async () => {
    if (mountedRef.current) setMarketStatus(inferMarketStatus())
  }, [])

  const seed = useCallback(async () => {
    try {
      const raw = await getMarketData()
      if (!mountedRef.current) return
      const items = normaliseMarketData(raw)
      const map = new Map<string, StockTick>()
      items.forEach((t: StockTick) => { if (t.tradingCode) map.set(t.tradingCode, t) })
      setTicks(map)
      setError(null)
    } catch (e: any) {
      if (mountedRef.current) setError(e?.message ?? "Market data unavailable")
    } finally {
      if (mountedRef.current) setLoading(false)
    }
  }, [])

  useEffect(() => {
    mountedRef.current = true
    seed()
    fetchStatus()
    const si = setInterval(fetchStatus, 60_000)
    return () => { mountedRef.current = false; clearInterval(si) }
  }, [seed, fetchStatus])

  // BulkPriceUpdate — incremental merge
  useEffect(() => {
    return subscribeMarket("BulkPriceUpdate", (raw: any) => {
      if (!mountedRef.current) return
      const items = normaliseMarketData(raw)
      if (!items.length) return
      setTicks(prev => {
        const next = new Map(prev)
        items.forEach((t: StockTick) => {
          if (t.tradingCode) next.set(t.tradingCode, { ...(next.get(t.tradingCode) ?? {}), ...t })
        })
        return next
      })
    })
  }, [])

  useEffect(() => {
    return subscribeMarket("IndexUpdate", (data: any) => {
      if (mountedRef.current && data?.marketStatus !== undefined)
        setMarketStatus(inferMarketStatus(data))
    })
  }, [])

  const ticksArray = Array.from(ticks.values())
  return {
    ticks, ticksArray, marketStatus, loading, error,
    getStock: (code: string) => ticks.get(code),
    refresh: seed,
  }
}
