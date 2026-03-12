// src/hooks/useDashboardPersistence.ts
// Saves: layout positions, widget visibility, color groups, active preset — all debounced auto-save

import { useState, useCallback, useRef, useEffect } from "react"
import type { Layout } from "react-grid-layout"

export type ColorGroup = "teal" | "blue" | "amber" | "purple" | "red" | null
export type PresetName = "Trading" | "Research" | "Portfolio" | "Full"

export interface WidgetState {
  id: string
  visible: boolean
  colorGroup: ColorGroup
}

interface DashboardState {
  layout: Layout[]
  widgets: WidgetState[]
  activePreset: PresetName | null
}

export interface SaveStatus {
  saved: boolean
  dirty: boolean
  timestamp: string | null
}

// ─── Storage keys ─────────────────────────────────────────────────────────────
const K_LAYOUT  = (uid: any) => `bd_oms_layout_v2_${uid}`
const K_WIDGETS = (uid: any) => `bd_oms_widgets_v2_${uid}`
const K_PRESET  = (uid: any) => `bd_oms_preset_v2_${uid}`
const K_META    = (uid: any) => `bd_oms_meta_v2_${uid}`

// ─── Preset layout definitions ─────────────────────────────────────────────────
export const PRESET_LAYOUTS: Record<PresetName, Layout[]> = {
  Trading: [
    { i: "ticker",     x: 0,  y: 0,  w: 48, h: 4  },
    { i: "index",      x: 0,  y: 4,  w: 10, h: 14 },
    { i: "chart",      x: 10, y: 4,  w: 24, h: 30 },
    { i: "orderbook",  x: 34, y: 4,  w: 14, h: 20 },
    { i: "order",      x: 0,  y: 18, w: 10, h: 20 },
    { i: "executions", x: 34, y: 24, w: 14, h: 14 },
    { i: "rms",        x: 0,  y: 38, w: 24, h: 12 },
    { i: "notif",      x: 24, y: 38, w: 24, h: 12 },
  ],
  Research: [
    { i: "ticker",    x: 0,  y: 0,  w: 48, h: 4  },
    { i: "movers",    x: 0,  y: 4,  w: 14, h: 22 },
    { i: "chart",     x: 14, y: 4,  w: 20, h: 30 },
    { i: "heatmap",   x: 34, y: 4,  w: 14, h: 20 },
    { i: "news",      x: 0,  y: 26, w: 14, h: 20 },
    { i: "ai",        x: 14, y: 34, w: 20, h: 16 },
    { i: "watchlist", x: 34, y: 24, w: 14, h: 22 },
  ],
  Portfolio: [
    { i: "ticker",     x: 0,  y: 0,  w: 48, h: 4  },
    { i: "portfolio",  x: 0,  y: 4,  w: 24, h: 24 },
    { i: "executions", x: 24, y: 4,  w: 24, h: 24 },
    { i: "rms",        x: 0,  y: 28, w: 24, h: 14 },
    { i: "order",      x: 24, y: 28, w: 24, h: 14 },
  ],
  Full: [
    { i: "ticker",     x: 0,  y: 0,  w: 48, h: 4  },
    { i: "index",      x: 0,  y: 4,  w: 8,  h: 14 },
    { i: "chart",      x: 8,  y: 4,  w: 22, h: 30 },
    { i: "orderbook",  x: 30, y: 4,  w: 10, h: 20 },
    { i: "depth",      x: 40, y: 4,  w: 8,  h: 20 },
    { i: "order",      x: 0,  y: 18, w: 8,  h: 20 },
    { i: "movers",     x: 30, y: 24, w: 10, h: 14 },
    { i: "pressure",   x: 40, y: 24, w: 8,  h: 14 },
    { i: "portfolio",  x: 0,  y: 38, w: 16, h: 14 },
    { i: "executions", x: 16, y: 38, w: 16, h: 14 },
    { i: "rms",        x: 32, y: 38, w: 16, h: 14 },
    { i: "watchlist",  x: 0,  y: 52, w: 12, h: 16 },
    { i: "heatmap",    x: 12, y: 52, w: 12, h: 16 },
    { i: "news",       x: 24, y: 52, w: 12, h: 16 },
    { i: "notif",      x: 36, y: 52, w: 12, h: 16 },
    { i: "ai",         x: 0,  y: 68, w: 24, h: 14 },
  ],
}

export const ALL_WIDGET_IDS = [
  "ticker","index","movers","watchlist","chart","orderbook",
  "order","portfolio","executions","heatmap","depth","pressure",
  "notif","news","ai","rms"
]

const DEFAULT_WIDGETS: WidgetState[] = ALL_WIDGET_IDS.map(id => ({
  id, visible: true, colorGroup: null
}))

// ─── Persist / Load ───────────────────────────────────────────────────────────
function persist(uid: any, state: DashboardState) {
  try {
    localStorage.setItem(K_LAYOUT(uid),  JSON.stringify(state.layout))
    localStorage.setItem(K_WIDGETS(uid), JSON.stringify(state.widgets))
    localStorage.setItem(K_PRESET(uid),  JSON.stringify(state.activePreset))
    localStorage.setItem(K_META(uid),    JSON.stringify({ savedAt: new Date().toISOString() }))
  } catch (e) { console.warn("[persist]", e) }
}

function hydrate(uid: any): DashboardState {
  try {
    const layout  = JSON.parse(localStorage.getItem(K_LAYOUT(uid)!)  ?? "null")
    const widgets = JSON.parse(localStorage.getItem(K_WIDGETS(uid)!) ?? "null")
    const preset  = JSON.parse(localStorage.getItem(K_PRESET(uid)!)  ?? "null")

    const mergedWidgets: WidgetState[] = ALL_WIDGET_IDS.map(id => {
      const saved = (widgets ?? []).find((w: WidgetState) => w.id === id)
      return saved ?? { id, visible: true, colorGroup: null }
    })

    return {
      layout:       layout  ?? PRESET_LAYOUTS["Trading"],
      widgets:      mergedWidgets,
      activePreset: preset  ?? "Trading",
    }
  } catch {
    return { layout: PRESET_LAYOUTS["Trading"], widgets: DEFAULT_WIDGETS, activePreset: "Trading" }
  }
}

// ─── Hook ─────────────────────────────────────────────────────────────────────
export function useDashboardPersistence(userId: number | string) {
  const [state, setState] = useState<DashboardState>(() => hydrate(userId))
  const [saveStatus, setSaveStatus] = useState<SaveStatus>({ saved: true, dirty: false, timestamp: null })
  const [showToast, setShowToast] = useState(false)
  const autoTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const pending = useRef<DashboardState | null>(null)

  useEffect(() => {
    try {
      const meta = localStorage.getItem(K_META(userId))
      if (meta) setSaveStatus(s => ({ ...s, timestamp: JSON.parse(meta).savedAt }))
    } catch {}
  }, [userId])

  // Debounced auto-save (1.5s idle after any change)
  const scheduleAutoSave = useCallback((next: DashboardState) => {
    pending.current = next
    if (autoTimer.current) clearTimeout(autoTimer.current)
    autoTimer.current = setTimeout(() => {
      if (pending.current) {
        persist(userId, pending.current)
        const ts = new Date().toISOString()
        setSaveStatus({ saved: true, dirty: false, timestamp: ts })
        pending.current = null
      }
    }, 1500)
  }, [userId])

  // Manual save with toast
  const save = useCallback(() => {
    const current = pending.current ?? state
    persist(userId, current)
    const ts = new Date().toISOString()
    setSaveStatus({ saved: true, dirty: false, timestamp: ts })
    pending.current = null
    setShowToast(true)
    setTimeout(() => setShowToast(false), 3000)
  }, [state, userId])

  const update = useCallback((patch: Partial<DashboardState>, immediate = false) => {
    setState(prev => {
      const next = { ...prev, ...patch }
      if (immediate) {
        persist(userId, next)
        const ts = new Date().toISOString()
        setSaveStatus({ saved: true, dirty: false, timestamp: ts })
        setShowToast(true)
        setTimeout(() => setShowToast(false), 3000)
      } else {
        setSaveStatus(s => ({ ...s, dirty: true, saved: false }))
        scheduleAutoSave(next)
      }
      return next
    })
  }, [userId, scheduleAutoSave])

  const setLayout = useCallback((layout: Layout[]) => {
    update({ layout })
  }, [update])

  const setWidgetVisible = useCallback((id: string, visible: boolean) => {
    setState(prev => {
      const widgets = prev.widgets.map(w => w.id === id ? { ...w, visible } : w)
      const next = { ...prev, widgets }
      scheduleAutoSave(next)
      setSaveStatus(s => ({ ...s, dirty: true, saved: false }))
      return next
    })
  }, [scheduleAutoSave])

  const setColorGroup = useCallback((id: string, colorGroup: ColorGroup) => {
    setState(prev => {
      const widgets = prev.widgets.map(w => w.id === id ? { ...w, colorGroup } : w)
      const next = { ...prev, widgets }
      scheduleAutoSave(next)
      setSaveStatus(s => ({ ...s, dirty: true, saved: false }))
      return next
    })
  }, [scheduleAutoSave])

  const applyPreset = useCallback((preset: PresetName) => {
    const presetLayout = PRESET_LAYOUTS[preset]
    setState(prev => {
      const widgets = prev.widgets.map(w => ({
        ...w,
        visible: presetLayout.some(l => l.i === w.id),
      }))
      const next: DashboardState = { layout: presetLayout, widgets, activePreset: preset }
      persist(userId, next)
      const ts = new Date().toISOString()
      setSaveStatus({ saved: true, dirty: false, timestamp: ts })
      setShowToast(true)
      setTimeout(() => setShowToast(false), 3000)
      return next
    })
  }, [userId])

  const reset = useCallback(() => {
    const fresh: DashboardState = { layout: PRESET_LAYOUTS["Trading"], widgets: DEFAULT_WIDGETS, activePreset: "Trading" }
    setState(fresh)
    persist(userId, fresh)
    setSaveStatus({ saved: true, dirty: false, timestamp: new Date().toISOString() })
  }, [userId])

  const visibleLayout = state.layout.filter(l =>
    state.widgets.find(w => w.id === l.i)?.visible !== false
  )

  return {
    layout: visibleLayout,
    fullLayout: state.layout,
    widgets: state.widgets,
    activePreset: state.activePreset,
    saveStatus,
    showToast,
    setLayout,
    setWidgetVisible,
    setColorGroup,
    applyPreset,
    save,
    reset,
    getColorGroup: (id: string) => state.widgets.find(w => w.id === id)?.colorGroup ?? null,
    isVisible: (id: string) => state.widgets.find(w => w.id === id)?.visible !== false,
  }
}
