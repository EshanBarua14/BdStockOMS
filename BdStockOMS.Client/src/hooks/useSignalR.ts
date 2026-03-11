import { useEffect, useRef, useCallback } from 'react'
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'

interface UseSignalROptions {
  hubUrl: string
  events: Record<string, (...args: unknown[]) => void>
  enabled?: boolean
}

export function useSignalR({ hubUrl, events, enabled = true }: UseSignalROptions) {
  const connectionRef = useRef<HubConnection | null>(null)
  const { user } = useAuthStore()

  useEffect(() => {
    if (!enabled || !user?.accessToken) return

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => user.accessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build()

    connectionRef.current = connection

    Object.entries(events).forEach(([event, handler]) => {
      connection.on(event, handler)
    })

    connection.start().catch(console.error)

    return () => {
      Object.keys(events).forEach(event => connection.off(event))
      connection.stop()
    }
  }, [hubUrl, enabled, user?.accessToken])

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    if (connectionRef.current?.state === 'Connected') {
      return connectionRef.current.invoke(method, ...args)
    }
  }, [])

  return { invoke }
}
