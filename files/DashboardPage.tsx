// src/pages/DashboardPage.tsx
// Complete dashboard: top-bar live ticker, persist hook, save toast, presets, widget picker

import React, { useCallback, useState, useEffect } from "react"
import GridLayout, { Layout } from "react-grid-layout"
import "react-grid-layout/css/styles.css"
import "react-resizable/css/styles.css"

import { useDashboardPersistence, ALL_WIDGET_IDS, PresetName } from "../hooks/useDashboardPersistence"
import { useMarketData } from "../hooks/useMarketData"
import { useOrders } from "../hooks/useOrders"
import { WidgetPanel } from "../components/widgets/WidgetPanel"
import { SaveConfirmToast } from "../components/ui/SaveConfirmToast"
import { TopBarTicker } from "../components/ui/TopBarTicker"
import { WIDGET_REGISTRY } from "../components/widgets/registry"

const USER_ID = 163

const PRESETS: PresetName[] = ["Trading", "Research", "Portfolio", "Full"]

export default function DashboardPage() {
  const dash = useDashboardPersistence(USER_ID)
  const market = useMarketData()
  const orders = useOrders()

  const [fullscreen, setFullscreen] = useState<string | null>(null)
  const [menuOpen, setMenuOpen] = useState<string | null>(null)
  const [showPicker, setShowPicker] = useState(false)
  const [gridWidth, setGridWidth] = useState(window.innerWidth)

  // Responsive grid width
  useEffect(() => {
    const obs = new ResizeObserver(entries => {
      for (const e of entries) setGridWidth(e.contentRect.width)
    })
    const el = document.getElementById("grid-container")
    if (el) obs.observe(el)
    return () => obs.disconnect()
  }, [])

  // ESC exits fullscreen
  useEffect(() => {
    const h = (e: KeyboardEvent) => { if (e.key === "Escape") setFullscreen(null) }
    window.addEventListener("keydown", h)
    return () => window.removeEventListener("keydown", h)
  }, [])

  const handleLayoutChange = useCallback((layout: Layout[]) => {
    dash.setLayout(layout)
  }, [dash.setLayout])

  const sharedProps = { marketData: market, ordersData: orders }

  return (
    <div className="flex flex-col h-screen bg-zinc-950 text-zinc-100 overflow-hidden select-none">

      {/* ══ TOP BAR ══════════════════════════════════════════════════════════ */}
      <div className="flex items-stretch h-9 bg-zinc-900 border-b border-zinc-800 flex-shrink-0 overflow-hidden">

        {/* Logo / brand */}
        <div className="flex items-center gap-2 px-3 border-r border-zinc-800 flex-shrink-0">
          <div className="w-4 h-4 rounded bg-blue-600 flex items-center justify-center">
            <span className="text-[8px] font-black text-white">BD</span>
          </div>
          <span className="text-[11px] font-bold text-zinc-300 tracking-wider hidden sm:block">OMS</span>
        </div>

        {/* Market status badge */}
        <div className="flex items-center px-3 border-r border-zinc-800 flex-shrink-0">
          <div className={`flex items-center gap-1.5 px-2 py-0.5 rounded text-[10px] font-semibold
            ${market.marketStatus.isOpen
              ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/30"
              : "bg-zinc-800/60 text-zinc-500 border border-zinc-700/50"
            }`}>
            <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0
              ${market.marketStatus.isOpen ? "bg-emerald-400 animate-pulse" : "bg-zinc-600"}`}
            />
            {market.marketStatus.label}
            {market.marketStatus.activeStocks > 0 &&
              <span className="text-zinc-600 ml-1">· {market.marketStatus.activeStocks}</span>
            }
          </div>
        </div>

        {/* Live scrolling ticker — takes remaining space */}
        <div className="flex-1 min-w-0 overflow-hidden">
          <TopBarTicker
            ticks={market.ticksArray}
            isMarketOpen={market.marketStatus.isOpen}
          />
        </div>

        {/* Preset buttons */}
        <div className="flex items-center gap-0.5 px-2 border-l border-zinc-800 flex-shrink-0">
          {PRESETS.map(p => (
            <button key={p} onClick={() => dash.applyPreset(p)}
              className={`px-2 py-1 text-[10px] rounded font-semibold transition-all
                ${dash.activePreset === p
                  ? "bg-blue-600 text-white"
                  : "text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800"
                }`}>
              {p}
            </button>
          ))}
        </div>

        {/* Widget picker button */}
        <div className="flex items-center px-2 border-l border-zinc-800 flex-shrink-0">
          <button onClick={() => setShowPicker(p => !p)}
            className={`px-2 py-1 text-[10px] rounded border transition-all
              ${showPicker ? "bg-zinc-700 border-zinc-600 text-zinc-200" : "border-zinc-700/50 text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800"}`}>
            ⊞ Widgets
          </button>
        </div>

        {/* Save status */}
        <div className="flex items-center px-2 border-l border-zinc-800 flex-shrink-0">
          <SaveConfirmToast
            show={dash.showToast}
            saveStatus={dash.saveStatus}
            onSave={dash.save}
          />
        </div>

        {/* Reset */}
        <div className="flex items-center px-2 border-l border-zinc-800 flex-shrink-0">
          <button onClick={dash.reset}
            className="px-2 py-1 text-[10px] rounded text-zinc-600 hover:text-zinc-300 hover:bg-zinc-800 transition-all"
            title="Reset to defaults">
            ↺
          </button>
        </div>
      </div>

      {/* ══ WIDGET PICKER DROPDOWN ══════════════════════════════════════════ */}
      {showPicker && (
        <div className="absolute top-9 right-2 z-50 bg-zinc-900 border border-zinc-700 rounded-xl shadow-2xl p-3 w-60"
             style={{ maxHeight: "80vh", overflowY: "auto" }}>
          <div className="flex items-center justify-between mb-2">
            <span className="text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Widgets</span>
            <button onClick={() => setShowPicker(false)} className="text-zinc-600 hover:text-zinc-300 text-xs">✕</button>
          </div>
          <div className="grid grid-cols-2 gap-1">
            {ALL_WIDGET_IDS.map(id => {
              const reg = WIDGET_REGISTRY[id]
              const visible = dash.isVisible(id)
              return (
                <button key={id} onClick={() => dash.setWidgetVisible(id, !visible)}
                  className={`flex items-center gap-1.5 px-2 py-1.5 rounded text-xs text-left transition-all
                    ${visible
                      ? "bg-blue-600/15 text-blue-400 border border-blue-600/30"
                      : "text-zinc-500 hover:text-zinc-300 hover:bg-zinc-800 border border-transparent"
                    }`}>
                  <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${visible ? "bg-blue-400" : "bg-zinc-700"}`} />
                  {reg?.title ?? id}
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* ══ GRID ════════════════════════════════════════════════════════════ */}
      <div id="grid-container" className="flex-1 overflow-auto">
        <GridLayout
          layout={dash.layout}
          cols={48}
          rowHeight={10}
          width={gridWidth}
          compactType={null}
          preventCollision={false}
          isDraggable
          isResizable
          onLayoutChange={handleLayoutChange}
          draggableHandle=".widget-drag-handle"
          margin={[3, 3]}
          containerPadding={[3, 3]}
        >
          {dash.layout.map(l => {
            const reg = WIDGET_REGISTRY[l.i]
            if (!reg) return null
            return (
              <div key={l.i}>
                <WidgetPanel
                  id={l.i}
                  title={reg.title}
                  colorGroup={dash.getColorGroup(l.i)}
                  onColorChange={c => dash.setColorGroup(l.i, c)}
                  onFullscreen={() => setFullscreen(l.i)}
                  onClose={() => dash.setWidgetVisible(l.i, false)}
                  menuOpen={menuOpen === l.i}
                  onMenuToggle={() => setMenuOpen(p => p === l.i ? null : l.i)}
                >
                  <reg.component {...sharedProps} colorGroup={dash.getColorGroup(l.i)} />
                </WidgetPanel>
              </div>
            )
          })}
        </GridLayout>
      </div>

      {/* ══ FULLSCREEN OVERLAY ══════════════════════════════════════════════ */}
      {fullscreen && (() => {
        const reg = WIDGET_REGISTRY[fullscreen]
        if (!reg) return null
        return (
          <div
            className="fixed inset-0 z-[9998] bg-black/85 backdrop-blur-sm flex items-center justify-center p-4"
            onClick={e => { if (e.target === e.currentTarget) setFullscreen(null) }}
          >
            <div className="w-full h-full max-w-[96vw] max-h-[96vh] bg-zinc-900 border border-zinc-700 rounded-xl overflow-hidden flex flex-col shadow-2xl">
              <div className="flex items-center justify-between px-4 py-2 border-b border-zinc-800 flex-shrink-0">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-zinc-200 uppercase tracking-wider">{reg.title}</span>
                  <span className="text-[10px] text-zinc-600">Fullscreen · ESC to exit</span>
                </div>
                <button onClick={() => setFullscreen(null)} className="text-zinc-500 hover:text-zinc-200 text-xs px-2 py-1 hover:bg-zinc-800 rounded transition-colors">✕</button>
              </div>
              <div className="flex-1 overflow-auto p-1">
                <reg.component {...sharedProps} colorGroup={dash.getColorGroup(fullscreen)} />
              </div>
            </div>
          </div>
        )
      })()}
    </div>
  )
}
