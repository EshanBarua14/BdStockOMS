// src/hooks/useOrders.ts
// Fixed: 3s polling, optimistic place/cancel, immediate double-refresh, SignalR merge

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
  0: { label: "Pending",       color: "text-amber-400" },
  1: { label: "Open",          color: "text-blue-400" },
  2: { label: "Partial Fill",  color: "text-cyan-400" },
  3: { label: "Filled",        color: "text-emerald-400" },
  4: { label: "Cancelled",     color: "text-zinc-500" },
  5: { label: "Rejected",      color: "text-red-400" },
  6: { label: "Expired",       color: "text-zinc-600" },
}

export const ORDER_TYPE_LABEL: Record<number, string> = { 0: "Buy", 1: "Sell" }
export const ORDER_CAT_LABEL: Record<number, string>  = { 0: "Market", 1: "Limit", 2: "Stop Loss" }

function normaliseOrders(raw: any): Order[] {
  if (!raw) return []
  if (Array.isArray(raw)) return raw
  if (raw.items) return raw.items
  if (raw.data) return raw.data
  if (typeof raw === "object") return Object.values(raw)
  return []
}

export function useOrders() {
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [placing, setPlacing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const mountedRef = useRef(true)
  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null)

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
    // 3s polling — was 8s
    pollRef.current = setInterval(() => fetchOrders(true), 3000)
    return () => {
      mountedRef.current = false
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [fetchOrders])

  // SignalR OrderUpdate — immediate merge
  useEffect(() => {
    return subscribeMarket("OrderUpdate", (updated: Order) => {
      if (!mountedRef.current) return
      setOrders(prev => {
        const idx = prev.findIndex(o => o.id === updated.id)
        const next = idx >= 0
          ? prev.map(o => o.id === updated.id ? { ...o, ...updated } : o)
          : [updated, ...prev]
        return next.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      })
    })
  }, [])

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
      const result = await placeOrder(dto)
      setOrders(prev =>
        [result, ...prev.filter(o => o.id !== tempId)]
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      )
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
    setOrders(prev => prev.map(o => o.id === id ? { ...o, status: 4 } : o))
    try {
      await cancelOrder(id)
      setTimeout(() => fetchOrders(true), 400)
    } catch { fetchOrders(true) }
  }, [fetchOrders])

  const executions = orders.filter(o => o.status === 2 || o.status === 3)
  return { orders, executions, loading, placing, error, place, cancel, refresh: () => fetchOrders(false) }
}
