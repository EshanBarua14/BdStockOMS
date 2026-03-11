import { create } from 'zustand'

export interface TenantSummary {
  brokerageHouseId: number
  brokerageName:    string
  databaseName:     string
  isProvisioned:    boolean
  isActive:         boolean
  lastMigratedAt?:  string
  userCount?:       number
}

interface TenantState {
  tenants:    TenantSummary[]
  loading:    boolean
  error:      string | null
  setTenants: (tenants: TenantSummary[]) => void
  setLoading: (v: boolean) => void
  setError:   (e: string | null) => void
}

export const useTenantStore = create<TenantState>((set) => ({
  tenants:    [],
  loading:    false,
  error:      null,
  setTenants: (tenants) => set({ tenants }),
  setLoading: (loading) => set({ loading }),
  setError:   (error)   => set({ error }),
}))
