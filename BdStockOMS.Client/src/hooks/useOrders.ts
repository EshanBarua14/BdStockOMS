import { useState, useEffect, useCallback } from 'react'
import { ordersApi } from '@/api/orders'
import { useAuthStore } from '@/store/authStore'
import type { Order, PlaceOrderRequest } from '@/types'

export function useOrders() {
  const user              = useAuthStore(s => s.user)
  const [orders, setOrders]   = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)
  const [placing, setPlacing] = useState(false)

  const loadOrders = useCallback(async () => {
    if (!user) return
    setLoading(true)
    try {
      const data = await ordersApi.getAll()
      setOrders(Array.isArray(data) ? data : data.data ?? [])
      setError(null)
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Failed to load orders')
    } finally {
      setLoading(false)
    }
  }, [user])

  useEffect(() => { loadOrders() }, [loadOrders])

  const placeOrder = useCallback(async (req: PlaceOrderRequest) => {
    setPlacing(true)
    try {
      const order = await ordersApi.place(req)
      setOrders(prev => [order, ...prev])
      return { success: true, order }
    } catch (e: any) {
      return { success: false, error: e?.response?.data?.message ?? 'Order failed' }
    } finally {
      setPlacing(false)
    }
  }, [])

  const cancelOrder = useCallback(async (orderId: string, reason: string) => {
    try {
      await ordersApi.cancel(orderId, { orderId, reason })
      setOrders(prev => prev.map(o =>
        o.orderId === orderId ? { ...o, status: 'Cancelled' as const } : o
      ))
      return { success: true }
    } catch (e: any) {
      return { success: false, error: e?.response?.data?.message ?? 'Cancel failed' }
    }
  }, [])

  return { orders, loading, error, placing, placeOrder, cancelOrder, refresh: loadOrders }
}
