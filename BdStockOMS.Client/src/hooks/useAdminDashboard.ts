import { useState, useEffect } from 'react'
import { adminApi } from '@/api/admin'

export interface DashboardData {
  userStats?:   any
  orderStats?:  any
  systemStats?: any
  activity?:    any[]
}

export function useAdminDashboard() {
  const [data, setData]       = useState<DashboardData>({})
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState<string | null>(null)

  useEffect(() => {
    const load = async () => {
      try {
        const [dashboard] = await Promise.all([adminApi.getDashboard()])
        setData(dashboard ?? {})
        setError(null)
      } catch (e: any) {
        setError(e?.response?.data?.message ?? 'Failed to load dashboard')
      } finally {
        setLoading(false)
      }
    }
    load()
    const interval = setInterval(load, 30_000)
    return () => clearInterval(interval)
  }, [])

  return { data, loading, error }
}
