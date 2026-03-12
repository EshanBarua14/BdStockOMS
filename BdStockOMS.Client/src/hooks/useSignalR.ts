// @ts-nocheck
import { useEffect, useRef, useCallback } from "react"
import * as signalR from "@microsoft/signalr"
import { useAuthStore } from "@/store/authStore"

const RETRY_DELAYS = [0, 1000, 3000, 5000, 10000]

interface UseSignalROptions {
  hub:    "stockprice" | "notification"
  events: Record<string, (...args: any[]) => void>
  groups?: string[]   // stock codes to subscribe to
}

const connections: Record<string, signalR.HubConnection> = {}

export function useSignalR({ hub, events, groups = [] }: UseSignalROptions) {
  const user    = useAuthStore(s => s.user)
  const connRef = useRef<signalR.HubConnection | null>(null)

  const getOrCreate = useCallback(() => {
    const url = `${import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7219"}/hubs/${hub}`
    if (connections[hub]) return connections[hub]

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => user?.token ?? "",
        skipNegotiation: false,
      })
      .withAutomaticReconnect(RETRY_DELAYS)
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connections[hub] = conn
    return conn
  }, [hub, user?.token])

  useEffect(() => {
    if (!user?.token) return
    const conn = getOrCreate()
    connRef.current = conn

    // Register all event handlers
    Object.entries(events).forEach(([event, handler]) => {
      conn.off(event)
      conn.on(event, handler)
    })

    const start = async () => {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        try {
          await conn.start()
          // Subscribe to stock groups
          for (const code of groups) {
            await conn.invoke("SubscribeToStock", code).catch(() => {})
          }
        } catch (e) {
          console.warn(`SignalR [${hub}] connect failed:`, e)
        }
      }
    }

    start()

    return () => {
      Object.keys(events).forEach(event => conn.off(event))
    }
  }, [user?.token, hub, JSON.stringify(groups)])

  return connRef
}

// Shared global SignalR state — single connection, multiple consumers
type MarketEventMap = {
  BulkPriceUpdate:  (updates: any[]) => void
  PriceUpdate:      (update: any)    => void
  DepthUpdate:      (depth: any)     => void
  PressureUpdate:   (list: any[])    => void
  IndexUpdate:      (indices: any)   => void
  NewsUpdate:       (item: any)      => void
}

const _listeners: Partial<Record<keyof MarketEventMap, Set<Function>>> = {}
let   _globalConn: signalR.HubConnection | null = null
let   _starting = false

export function subscribeMarket<K extends keyof MarketEventMap>(
  event: K, cb: MarketEventMap[K]
): () => void {
  if (!_listeners[event]) _listeners[event] = new Set()
  _listeners[event]!.add(cb)
  return () => _listeners[event]?.delete(cb)
}

export async function startGlobalMarketHub(token: string) {
  if (_globalConn || _starting) return
  _starting = true
  const url = `${import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7219"}/hubs/stockprice`

  _globalConn = new signalR.HubConnectionBuilder()
    .withUrl(url, { accessTokenFactory: () => token })
    .withAutomaticReconnect(RETRY_DELAYS)
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  const events: (keyof MarketEventMap)[] = [
    "BulkPriceUpdate","PriceUpdate","DepthUpdate","PressureUpdate","IndexUpdate","NewsUpdate"
  ]
  events.forEach(ev => {
    _globalConn!.on(ev, (...args) => {
      _listeners[ev]?.forEach(cb => cb(...args))
    })
  })

  try {
    await _globalConn.start()
    console.log("Global market hub connected.")
  } catch(e) {
    console.warn("Global market hub failed:", e)
  }
  _starting = false
}
