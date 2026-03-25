// src/hooks/useColorGroupSync.ts
// Symbol sync bus for color-linked widgets.
// When any widget in a color group selects a stock, all widgets in that group update.

import { create } from "zustand"

interface ColorGroupSyncStore {
  symbols: Record<string, string>   // colorGroup → tradingCode
  publish: (group: string, code: string) => void
}

export const useColorGroupSync = create<ColorGroupSyncStore>((set) => ({
  symbols: {},
  publish: (group, code) => {
    if (!group || !code) return
    set(s => ({ symbols: { ...s.symbols, [group]: code } }))
  },
}))

// ─── Hook for widgets ─────────────────────────────────────────────────────────
// Returns [linkedSymbol, emitSymbol]
// linkedSymbol: current symbol for this widget's color group (null if no group)
// emitSymbol:   call this when the widget selects a stock
export function useLinkedSymbol(colorGroup: string | null | undefined): [string | null, (code: string) => void] {
  const publish = useColorGroupSync(s => s.publish)
  const symbols = useColorGroupSync(s => s.symbols)
  const linked  = colorGroup ? (symbols[colorGroup] ?? null) : null
  const emit    = (code: string) => { if (colorGroup) publish(colorGroup, code) }
  return [linked, emit]
}
