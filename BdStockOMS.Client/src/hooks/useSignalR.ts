import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'

type HubEventMap = Record<string, (...args: unknown[]) => void>

interface UseSignalROptions {
  hubUrl: string
  events: HubEventMap
  enabled?: boolean
}

export function useSignalR({ hubUrl, events, enabled = true }: UseSignalROptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const { user } = useAuthStore()

  const buildConnection = useCallback(() => {
    return new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => user?.accessToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(
        import.meta.env.DEV
          ? signalR.LogLevel.Information
          : signalR.LogLevel.Warning,
      )
      .build()
  }, [hubUrl, user?.accessToken])

  useEffect(() => {
    if (!enabled || !user?.accessToken) return

    const connection = buildConnection()
    connectionRef.current = connection

    // Register all event handlers
    for (const [event, handler] of Object.entries(events)) {
      connection.on(event, handler)
    }

    connection
      .start()
      .catch((err) => console.error('[SignalR] Connection error:', err))

    return () => {
      for (const event of Object.keys(events)) {
        connection.off(event)
      }
      connection.stop().catch(() => undefined)
      connectionRef.current = null
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [enabled, user?.accessToken, hubUrl])

  const invoke = useCallback(
    async (method: string, ...args: unknown[]) => {
      if (!connectionRef.current) return
      try {
        return await connectionRef.current.invoke(method, ...args)
      } catch (err) {
        console.error(`[SignalR] Invoke error (${method}):`, err)
      }
    },
    [],
  )

  return { invoke }
}
