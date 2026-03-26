// src/store/useSettingsStore.ts
// Centralized settings store — persists to localStorage automatically

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface OmsSettings {
  // Display
  showAnimations: boolean
  compactMode: boolean
  showVolumeBars: boolean
  // Trading
  confirmOrders: boolean
  boRequired: boolean
  showRmsWarnings: boolean
  defaultOrderType: 'Limit' | 'Market' | 'Stop'
  // Alerts
  priceAlerts: boolean
  orderAlerts: boolean
  rmsAlerts: boolean
  newsAlerts: boolean
  // Ticker
  showTicker: boolean
  tickerSpeed: 'slow' | 'normal' | 'fast'
  tickerFilter: 'all' | 'watchlist' | 'dse' | 'cse'
  // Keyboard
  f1Buy: boolean
  f2Sell: boolean
  escClose: boolean
  // Data
  autoSaveLayout: boolean
  analyticsEnabled: boolean
}

const DEFAULTS: OmsSettings = {
  showAnimations: true,
  compactMode: false,
  showVolumeBars: true,
  confirmOrders: true,
  boRequired: true,
  showRmsWarnings: true,
  defaultOrderType: 'Limit',
  priceAlerts: true,
  orderAlerts: true,
  rmsAlerts: true,
  newsAlerts: true,
  showTicker: true,
  tickerSpeed: 'normal',
  tickerFilter: 'all',
  f1Buy: true,
  f2Sell: true,
  escClose: true,
  autoSaveLayout: true,
  analyticsEnabled: false,
}

interface SettingsStore {
  settings: OmsSettings
  set: (key: keyof OmsSettings, value: any) => void
  reset: () => void
}

export const useSettingsStore = create<SettingsStore>()(
  persist(
    (set) => ({
      settings: DEFAULTS,
      set: (key, value) =>
        set(s => ({ settings: { ...s.settings, [key]: value } })),
      reset: () => set({ settings: DEFAULTS }),
    }),
    {
      name: 'bd_oms_settings_v1',
    }
  )
)

// Convenience selector — avoids boilerplate in consumers
// Usage: const confirmOrders = useSetting('confirmOrders')
export function useSetting<K extends keyof OmsSettings>(key: K): OmsSettings[K] {
  return useSettingsStore(s => s.settings[key])
}
