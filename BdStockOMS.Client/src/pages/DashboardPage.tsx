// @ts-nocheck
import { useState, useCallback, useEffect, useMemo } from "react"
import GridLayout from "react-grid-layout"
import "react-grid-layout/css/styles.css"
import "react-resizable/css/styles.css"
import { WIDGET_REGISTRY } from "@/components/widgets/registry"
import { WidgetToolbar }   from "@/components/widgets/WidgetToolbar"
import { MarketTickerStrip } from "@/components/widgets/MarketTickerStrip"
import { useAuthStore }    from "@/store/authStore"
import { ThemeMenu }       from "@/components/ui/ThemeMenu"

// ── Storage helpers ───────────────────────────────────────────────────────
const LAYOUT_KEY  = (uid) => `bd_oms_layout_v2_${uid}`
const WIDGETS_KEY = (uid) => `bd_oms_widgets_v2_${uid}`

const DEFAULT_WIDGETS = ["ticker","index","movers","watchlist","chart","orderbook","order","portfolio"]

function defaultLayout(activeIds, width) {
  const cols = 12
  const positions = [
    { id:"ticker",     x:0,  y:0,  w:12, h:1  },
    { id:"index",      x:0,  y:1,  w:3,  h:5  },
    { id:"movers",     x:3,  y:1,  w:3,  h:5  },
    { id:"chart",      x:6,  y:1,  w:6,  h:5  },
    { id:"watchlist",  x:0,  y:6,  w:3,  h:6  },
    { id:"order",      x:3,  y:6,  w:3,  h:7  },
    { id:"orderbook",  x:6,  y:6,  w:6,  h:5  },
    { id:"portfolio",  x:0,  y:12, w:3,  h:6  },
    { id:"executions", x:3,  y:12, w:3,  h:5  },
    { id:"heatmap",    x:6,  y:11, w:6,  h:5  },
    { id:"depth",      x:0,  y:18, w:3,  h:6  },
    { id:"pressure",   x:3,  y:18, w:3,  h:5  },
    { id:"notif",      x:6,  y:16, w:3,  h:5  },
    { id:"news",       x:9,  y:16, w:3,  h:5  },
    { id:"ai",         x:0,  y:24, w:3,  h:6  },
    { id:"rms",        x:3,  y:24, w:3,  h:5  },
  ]
  return activeIds.map(id => {
    const def = WIDGET_REGISTRY.find(w => w.id === id)
    const pos = positions.find(p => p.id === id)
    return {
      i: id,
      x: pos?.x ?? 0,
      y: pos?.y ?? 99,
      w: pos?.w ?? def?.defaultW ?? 3,
      h: pos?.h ?? def?.defaultH ?? 4,
      minW: def?.minW ?? 2,
      minH: def?.minH ?? 2,
    }
  })
}

// ── Widget wrapper ─────────────────────────────────────────────────────────
function WidgetPanel({ def, onClose, linkedSymbol, onSymbolClick, colorGroup, onColorGroup }) {
  const [minimized, setMinimized] = useState(false)
  const Comp = def.component

  const GROUP_COLORS = ["#00D4AA","#3B82F6","#F59E0B","#8B5CF6","#FF6B6B"]

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "#0D1320", border: "1px solid rgba(255,255,255,0.07)", borderRadius: 8, overflow: "hidden", boxShadow: "0 4px 20px rgba(0,0,0,0.3)" }}>
      {/* Widget header */}
      <div className="widget-drag-handle" style={{ display: "flex", alignItems: "center", gap: 6, padding: "4px 8px", background: "#0A0F1C", borderBottom: "1px solid rgba(255,255,255,0.06)", cursor: "grab", userSelect: "none", flexShrink: 0 }}>
        <span style={{ fontSize: 12 }}>{def.icon}</span>
        <span style={{ color: "rgba(255,255,255,0.6)", fontSize: 11, fontFamily: "'Space Mono',monospace", flex: 1, letterSpacing: "0.04em" }}>{def.label.toUpperCase()}</span>

        {/* Color group selector */}
        <div style={{ display: "flex", gap: 3 }}>
          {GROUP_COLORS.map(c => (
            <button key={c} onClick={() => onColorGroup(c === colorGroup ? null : c)}
              title="Link widget to color group"
              style={{ width: 8, height: 8, borderRadius: "50%", background: c, border: `1px solid ${colorGroup === c ? "#fff" : "transparent"}`, cursor: "pointer", padding: 0, outline: "none", opacity: colorGroup === c ? 1 : 0.4, transition: "opacity 0.15s" }} />
          ))}
        </div>

        {/* Linked symbol indicator */}
        {linkedSymbol && (
          <span style={{ background: "rgba(0,212,170,0.1)", border: "1px solid rgba(0,212,170,0.3)", color: "#00D4AA", fontSize: 9, padding: "1px 5px", borderRadius: 3, fontFamily: "'Space Mono',monospace" }}>{linkedSymbol}</span>
        )}

        <button onClick={() => setMinimized(v => !v)} title={minimized ? "Expand" : "Minimize"}
          style={{ background: "none", border: "none", color: "rgba(255,255,255,0.3)", cursor: "pointer", fontSize: 12, padding: "0 2px", lineHeight: 1 }}>
          {minimized ? "□" : "—"}
        </button>
        <button onClick={onClose} title="Remove widget"
          style={{ background: "none", border: "none", color: "rgba(255,255,255,0.2)", cursor: "pointer", fontSize: 14, padding: "0 2px", lineHeight: 1 }}>
          ×
        </button>
      </div>

      {/* Widget body */}
      {!minimized && (
        <div style={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
          <Comp linkedSymbol={linkedSymbol} onSymbolClick={onSymbolClick} />
        </div>
      )}
    </div>
  )
}

// ── DashboardPage ──────────────────────────────────────────────────────────
export function DashboardPage() {
  const user = useAuthStore(s => s.user)
  const uid  = user?.userId ?? "guest"

  const [activeIds, setActiveIds]     = useState(() => {
    try { return JSON.parse(localStorage.getItem(WIDGETS_KEY(uid)) ?? "null") ?? DEFAULT_WIDGETS } catch { return DEFAULT_WIDGETS }
  })
  const [layout, setLayout]           = useState(null)
  const [gridWidth, setGridWidth]     = useState(1200)
  const [symbolLinks, setSymbolLinks] = useState({})   // widgetId -> symbol
  const [colorGroups, setColorGroups] = useState({})   // widgetId -> color
  const [savedMsg, setSavedMsg]       = useState(false)

  // Measure grid container width
  const containerRef = useCallback(node => {
    if (!node) return
    const obs = new ResizeObserver(entries => setGridWidth(entries[0].contentRect.width))
    obs.observe(node)
    setGridWidth(node.offsetWidth)
    return () => obs.disconnect()
  }, [])

  // Load saved layout
  useEffect(() => {
    try {
      const saved = JSON.parse(localStorage.getItem(LAYOUT_KEY(uid)) ?? "null")
      if (saved) setLayout(saved)
    } catch {}
  }, [uid])

  // Compute current layout
  const currentLayout = useMemo(() => {
    const saved = layout ?? []
    return activeIds.map(id => {
      const existing = saved.find(l => l.i === id)
      if (existing) return existing
      const def = WIDGET_REGISTRY.find(w => w.id === id)
      const dl  = defaultLayout([id], gridWidth)
      return dl[0]
    })
  }, [activeIds, layout, gridWidth])

  const handleLayoutChange = useCallback((newLayout) => {
    setLayout(prev => {
      const merged = activeIds.map(id => {
        const nl = newLayout.find(l => l.i === id)
        return nl ?? (prev ?? []).find(l => l.i === id) ?? currentLayout.find(l => l.i === id)
      }).filter(Boolean)
      return merged
    })
  }, [activeIds, currentLayout])

  const toggleWidget = useCallback((id) => {
    setActiveIds(prev => {
      const next = prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
      localStorage.setItem(WIDGETS_KEY(uid), JSON.stringify(next))
      return next
    })
  }, [uid])

  const saveLayout = useCallback(() => {
    localStorage.setItem(LAYOUT_KEY(uid), JSON.stringify(currentLayout))
    localStorage.setItem(WIDGETS_KEY(uid), JSON.stringify(activeIds))
    setSavedMsg(true)
    setTimeout(() => setSavedMsg(false), 2000)
  }, [uid, currentLayout, activeIds])

  const resetLayout = useCallback(() => {
    setActiveIds(DEFAULT_WIDGETS)
    setLayout(defaultLayout(DEFAULT_WIDGETS, gridWidth))
    localStorage.removeItem(LAYOUT_KEY(uid))
    localStorage.removeItem(WIDGETS_KEY(uid))
  }, [uid, gridWidth])

  // Symbol linking via color groups
  const handleSymbolClick = useCallback((fromId, symbol) => {
    const myColor = colorGroups[fromId]
    if (!myColor) { setSymbolLinks(p => ({ ...p, [fromId]: symbol })); return }
    const linked = Object.entries(colorGroups).filter(([, c]) => c === myColor).map(([id]) => id)
    setSymbolLinks(p => { const n = { ...p }; linked.forEach(id => { n[id] = symbol }); return n })
  }, [colorGroups])

  const setColorGroup = useCallback((id, color) => {
    setColorGroups(p => ({ ...p, [id]: color }))
  }, [])

  const activeDefs = activeIds.map(id => WIDGET_REGISTRY.find(w => w.id === id)).filter(Boolean)

  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100%", background: "#080C14" }}>

      {/* Dashboard Topbar */}
      <div style={{ display: "flex", alignItems: "center", gap: 10, padding: "6px 14px", borderBottom: "1px solid rgba(255,255,255,0.06)", background: "#0A0F1C", flexShrink: 0, flexWrap: "wrap" }}>

        {/* Page label */}
        <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace", letterSpacing: "0.1em" }}>TRADING TERMINAL</span>

        <div style={{ width: 1, height: 16, background: "rgba(255,255,255,0.08)" }} />

        {/* Widget toolbar */}
        <WidgetToolbar activeIds={activeIds} onToggle={toggleWidget} onReset={resetLayout} onSave={saveLayout} />

        {/* Presets */}
        <div style={{ display: "flex", gap: 4 }}>
          {[
            { label: "Trading",   ids: ["ticker","order","orderbook","executions","chart","depth","pressure","rms"] },
            { label: "Research",  ids: ["ticker","chart","heatmap","movers","ai","news","index"] },
            { label: "Portfolio", ids: ["ticker","portfolio","executions","notif","rms","watchlist"] },
            { label: "Full",      ids: WIDGET_REGISTRY.map(w => w.id) },
          ].map(p => (
            <button key={p.label} onClick={() => { setActiveIds(p.ids); setLayout(defaultLayout(p.ids, gridWidth)); localStorage.setItem(WIDGETS_KEY(uid), JSON.stringify(p.ids)) }}
              style={{ padding: "4px 9px", background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, color: "rgba(255,255,255,0.45)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace", transition: "all 0.15s" }}>
              {p.label}
            </button>
          ))}
        </div>

        {savedMsg && (
          <span style={{ color: "#00D4AA", fontSize: 10, fontFamily: "'Space Mono',monospace", animation: "fadeIn 0.2s" }}>✓ Layout saved</span>
        )}

        <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 8 }}>
          <ThemeMenu variant="compact" />
          <span style={{ color: "rgba(255,255,255,0.2)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>
            {user?.fullName} · <span style={{ color: "#00D4AA" }}>{user?.role}</span>
          </span>
        </div>
      </div>

      {/* Grid canvas */}
      <div ref={containerRef} style={{ flex: 1, overflow: "auto", padding: "4px" }}>
        {gridWidth > 0 && (
          <GridLayout
            layout={currentLayout}
            cols={12}
            rowHeight={60}
            width={gridWidth - 8}
            onLayoutChange={handleLayoutChange}
            draggableHandle=".widget-drag-handle"
            margin={[4, 4]}
            containerPadding={[0, 0]}
            isResizable={true}
            isDraggable={true}
            resizeHandles={["se","sw","ne","nw","e","w","s"]}
            style={{ minHeight: "100%" }}
          >
            {activeDefs.map(def => (
              <div key={def.id}>
                <WidgetPanel
                  def={def}
                  onClose={() => toggleWidget(def.id)}
                  linkedSymbol={symbolLinks[def.id]}
                  onSymbolClick={(sym) => handleSymbolClick(def.id, sym)}
                  colorGroup={colorGroups[def.id]}
                  onColorGroup={(c) => setColorGroup(def.id, c)}
                />
              </div>
            ))}
          </GridLayout>
        )}

        {activeIds.length === 0 && (
          <div style={{ height: "60vh", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 12 }}>
            <span style={{ fontSize: 48 }}>⊞</span>
            <div style={{ color: "#fff", fontSize: 18, fontWeight: 700 }}>Your dashboard is empty</div>
            <div style={{ color: "rgba(255,255,255,0.4)", fontSize: 13 }}>Click <strong style={{ color: "#00D4AA" }}>Widgets</strong> above to add panels</div>
          </div>
        )}
      </div>

      <style>{`
        .react-grid-item.react-grid-placeholder { background: rgba(0,212,170,0.08) !important; border: 1px dashed rgba(0,212,170,0.4) !important; border-radius: 8px !important; }
        .react-resizable-handle { opacity: 0; transition: opacity 0.15s; }
        .react-grid-item:hover .react-resizable-handle { opacity: 1; }
        .react-resizable-handle::after { border-color: rgba(0,212,170,0.5) !important; }
        @keyframes fadeIn { from { opacity:0 } to { opacity:1 } }
      `}</style>
    </div>
  )
}

export default DashboardPage
