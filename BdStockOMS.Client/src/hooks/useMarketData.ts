// @ts-nocheck
import { useState, useEffect, useRef } from "react"
import { apiClient } from "@/api/client"
import { useAuthStore } from "@/store/authStore"
import { subscribeMarket, startGlobalMarketHub } from "./useSignalR"

export function useMarketData() {
  const user = useAuthStore(s => s.user)
  const [stocks, setStocks]       = useState([])
  const [connected, setConnected] = useState(false)
  const mapRef = useRef<Map<string, any>>(new Map())

  // Initial REST load
  useEffect(() => {
    if (!user?.token) return
    apiClient.get("/MarketData").then(r => {
      const list = r.data ?? []
      list.forEach(s => mapRef.current.set(s.tradingCode, s))
      setStocks(list)
    }).catch(() => {})

    // Start global hub
    startGlobalMarketHub(user.token).then(() => setConnected(true))
  }, [user?.token])

  // Live updates from SignalR
  useEffect(() => {
    const unsubBulk = subscribeMarket("BulkPriceUpdate", (updates: any[]) => {
      updates.forEach(u => {
        const existing = mapRef.current.get(u.tradingCode) ?? {}
        mapRef.current.set(u.tradingCode, { ...existing, ...u })
      })
      setStocks(Array.from(mapRef.current.values()))
      setConnected(true)
    })

    const unsubSingle = subscribeMarket("PriceUpdate", (u: any) => {
      const existing = mapRef.current.get(u.tradingCode) ?? {}
      mapRef.current.set(u.tradingCode, { ...existing, ...u })
      setStocks(Array.from(mapRef.current.values()))
    })

    return () => { unsubBulk(); unsubSingle() }
  }, [])

  return { stocks, connected }
}
