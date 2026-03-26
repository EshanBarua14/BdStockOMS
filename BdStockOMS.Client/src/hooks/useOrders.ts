// src/hooks/useOrders.ts
// Fixed Day 61 audit:
// - cancelOrder uses PUT (matches [HttpPut("{id}/cancel")] on backend)
// - Removed duplicate export of ORDER_STATUS etc (already in this file)
// - TradeExecuted SignalR event also refreshes orders

import { useState, useEffect, useCallback, useRef } from "react"
import { getOrders, placeOrder, cancelOrder } from "../api/client"
import { subscribeMarket } from "./useSignalR"

export interface Order {
  id: number
  stockId: number
  tradingCode: string
  stockName?: string
  orderType: number        // 0=Buy, 1=Sell
  orderCategory: number    // 0=Market, 1=Limit, 2=StopLoss
  quantity: number
  filledQuantity?: number
  averagePrice?: number
  limitPrice?: number
  status: number           // 0=Pending,1=Open,2=PartiallyFilled,3=Filled,4=Cancelled,5=Rejected,6=Expired
  createdAt: string
  updatedAt?: string
  investorId?: number
}

export interface PlaceOrderDto {
  stockId: number
  orderType: number
  orderCategory: number
  quantity: number
  limitPrice?: number
  investorId?: number
}

export const ORDER_STATUS: Record<number, { label: string; color: string }> = {
  0: { label: "Pending",         color: "text-amber-400"   },
  1: { label: "Open",            color: "text-blue-400"    },
  2: { label: "Partial Fill",    color: "text-purple-400"  },
  3: { label: "Filled",          color: "text-emerald-400" },
  4: { label: "Completed",       color: "text-cyan-400"    },
  5: { label: "Cancelled",       color: "text-zinc-500"    },
  6: { label: "Rejected",        color: "text-red-400"     },
}

export const ORDER_TYPE_LABEL: Record<number, string> = { 0: "Buy", 1: "Sell" }
export const ORDER_CAT_LABEL:  Record<number, string> = { 0: "Market", 1: "Limit", 2: "Stop Loss" }

function normaliseOrders(raw: any): Order[] {
  if (!raw) return []
  if (Array.isArray(raw)) return raw
  if (raw.items) return raw.items
  if (raw.data)  return raw.data
  if (typeof raw === "object") return Object.values(raw)
  return []
}

export function useOrders() {
  const [orders,  setOrders]  = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [placing, setPlacing] = useState(false)
  const [error,   setError]   = useState<string | null>(null)
  const mountedRef = useRef(true)
  const pollRef    = useRef<ReturnType<typeof setInterval> | null>(null)

  const fetchOrders = useCallback(async (silent = false) => {
    try {
      if (!silent) setLoading(true)
      const raw = await getOrders()
      if (!mountedRef.current) return
      const list = normaliseOrders(raw)
      list.sort((a: Order, b: Order) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      setOrders(list)
      setError(null)
    } catch (e: any) {
      if (mountedRef.current) setError(e?.message ?? "Failed to load orders")
    } finally {
      if (mountedRef.current && !silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    mountedRef.current = true
    fetchOrders()
    // Poll every 30s as backup (SignalR is primary)
    pollRef.current = setInterval(() => fetchOrders(true), 30_000)
    return () => {
      mountedRef.current = false
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [fetchOrders])

  // SignalR OrderUpdate — immediate merge
  useEffect(() => {
    return (subscribeMarket as any)("OrderUpdate", (updated: Order) => {
      if (!mountedRef.current) return
      setOrders(prev => {
        const idx = prev.findIndex(o => o.id === updated.id)
        const next = idx >= 0
          ? prev.map(o => o.id === updated.id ? { ...o, ...updated } : o)
          : [updated, ...prev]
        return next.sort((a: any, b: any) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      })
    })
  }, [])

  // Also refresh on TradeExecuted (order got filled)
  useEffect(() => {
    return (subscribeMarket as any)("TradeExecuted", () => {
      if (mountedRef.current) setTimeout(() => fetchOrders(true), 500)
    })
  }, [fetchOrders])

  const place = useCallback(async (dto: PlaceOrderDto): Promise<Order> => {
    setPlacing(true)
    const tempId = -(Date.now())
    const optimistic: Order = {
      id: tempId, stockId: dto.stockId, tradingCode: "",
      orderType: dto.orderType, orderCategory: dto.orderCategory,
      quantity: dto.quantity, limitPrice: dto.limitPrice,
      filledQuantity: 0, status: 0, createdAt: new Date().toISOString(),
    }
    setOrders(prev => [optimistic, ...prev])
    try {
      const result = await placeOrder(dto) as Order
      setOrders(prev =>
        [result, ...prev.filter(o => o.id !== tempId)]
          .sort((a: any, b: any) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      )
      // Double-refresh to catch server-side state
      setTimeout(() => fetchOrders(true), 400)
      setTimeout(() => fetchOrders(true), 2000)
      return result
    } catch (e) {
      setOrders(prev => prev.filter(o => o.id !== tempId))
      throw e
    } finally {
      setPlacing(false)
    }
  }, [fetchOrders])

  const cancel = useCallback(async (id: number) => {
    // Optimistic update: mark as cancelled immediately
    setOrders(prev => prev.map(o => o.id === id ? { ...o, status: 4 } : o))
    try {
      await cancelOrder(id) // Uses PUT /api/orders/{id}/cancel
      setTimeout(() => fetchOrders(true), 400)
    } catch {
      // Revert optimistic update on failure
      fetchOrders(true)
    }
  }, [fetchOrders])

  const executions = orders.filter(o => o.status === 2 || o.status === 3)

  return {
    orders,
    executions,
    loading,
    placing,
    error,
    place,
    cancel,
    cancelOrder: cancel,  // alias for OrderBookWidget compatibility
    refresh: () => fetchOrders(false),
  }
}
