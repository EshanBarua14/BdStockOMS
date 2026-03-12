// @ts-nocheck
// src/pages/DashboardPage.tsx
// Premium Dashboard — glass top bar, theme CSS vars, presets, widget picker

import React, { useCallback, useState, useEffect } from "react"
import GridLayout from "react-grid-layout"
type Layout = { i: string; x: number; y: number; w: number; h: number }
import "react-grid-layout/css/styles.css"
import "react-resizable/css/styles.css"

import { useDashboardPersistence, ALL_WIDGET_IDS, PresetName } from "../hooks/useDashboardPersistence"
import { useMarketData } from "../hooks/useMarketData"
import { useOrders } from "../hooks/useOrders"
import { WidgetPanel } from "../components/widgets/WidgetPanel"
import { SaveConfirmToast } from "../components/ui/SaveConfirmToast"
import { TopBarTicker } from "../components/ui/TopBarTicker"
import { WIDGET_REGISTRY } from "../components/widgets/registry"

class WidgetErrorBoundary extends React.Component {
  state = { hasError: false, error: null }
  static getDerivedStateFromError(error) { return { hasError: true, error } }
  componentDidCatch(error, info) { console.error("Widget crash:", error, info) }
  render() {
    if (this.state.hasError) return (
      <div style={{
        padding: 12, color: 'var(--t-sell)', fontSize: 11,
        fontFamily: "'JetBrains Mono', monospace",
        background: 'var(--t-surface)', height: '100%',
      }}>
        <div style={{ fontWeight: 700, marginBottom: 4 }}>Widget Error</div>
        <div style={{ color: 'var(--t-text3)', wordBreak: 'break-all' }}>{String(this.state.error)}</div>
      </div>
    )
    return this.props.children
  }
}

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

  useEffect(() => {
    const obs = new ResizeObserver(entries => {
      for (const e of entries) setGridWidth(e.contentRect.width)
    })
    const el = document.getElementById("grid-container")
    if (el) obs.observe(el)
    return () => obs.disconnect()
  }, [])

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
    <div style={{
      display: 'flex', flexDirection: 'column', height: '100%',
      background: 'var(--t-bg)', color: 'var(--t-text1)',
      overflow: 'hidden', userSelect: 'none',
    }}>

      {/* ══ TOP CONTROL BAR ══════════════════════════════════════════════ */}
      <div style={{
        display: 'flex', alignItems: 'stretch', height: 34, flexShrink: 0,
        background: 'var(--t-surface)', borderBottom: '1px solid var(--t-border)',
        overflow: 'hidden',
      }}>
        {/* Market status */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 10px',
          borderRight: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <div style={{
            display: 'flex', alignItems: 'center', gap: 5,
            padding: '3px 8px', borderRadius: 12,
            background: market.marketStatus.isOpen ? 'rgba(0,230,118,0.06)' : 'var(--t-hover)',
            border: `1px solid ${market.marketStatus.isOpen ? 'rgba(0,230,118,0.15)' : 'var(--t-border)'}`,
          }}>
            <span style={{
              width: 5, height: 5, borderRadius: '50%', flexShrink: 0,
              background: market.marketStatus.isOpen ? 'var(--t-buy)' : 'var(--t-text3)',
              animation: market.marketStatus.isOpen ? 'oms-pulse 2s ease-in-out infinite' : 'none',
            }} />
            <span style={{
              fontSize: 9, fontWeight: 700, color: market.marketStatus.isOpen ? 'var(--t-buy)' : 'var(--t-text3)',
              fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.05em',
            }}>{market.marketStatus.label}</span>
            {market.marketStatus.activeStocks > 0 && (
              <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>
                · {market.marketStatus.activeStocks}
              </span>
            )}
          </div>
        </div>

        {/* Live ticker */}
        <div style={{ flex: 1, minWidth: 0, overflow: 'hidden' }}>
          <TopBarTicker ticks={market.ticksArray} isMarketOpen={market.marketStatus.isOpen} />
        </div>

        {/* Presets */}
        <div style={{
          display: 'flex', alignItems: 'center', gap: 3,
          padding: '0 8px', borderLeft: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          {PRESETS.map(p => (
            <button key={p} onClick={() => dash.applyPreset(p)} style={{
              padding: '3px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
              border: 'none', cursor: 'pointer', transition: 'all 0.12s',
              fontFamily: "'JetBrains Mono', monospace",
              background: dash.activePreset === p ? 'var(--t-accent)' : 'var(--t-hover)',
              color: dash.activePreset === p ? '#000' : 'var(--t-text3)',
              boxShadow: dash.activePreset === p ? `0 0 8px var(--t-accent-glow)` : 'none',
            }}>{p}</button>
          ))}
        </div>

        {/* Widget picker toggle */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 8px',
          borderLeft: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <button onClick={() => setShowPicker(p => !p)} style={{
            padding: '3px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
            border: `1px solid ${showPicker ? 'var(--t-accent)' : 'var(--t-border)'}`,
            cursor: 'pointer', transition: 'all 0.12s',
            fontFamily: "'JetBrains Mono', monospace",
            background: showPicker ? 'var(--t-hover)' : 'transparent',
            color: showPicker ? 'var(--t-accent)' : 'var(--t-text3)',
          }}>⊞ Widgets</button>
        </div>

        {/* Save status */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 8px',
          borderLeft: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <SaveConfirmToast show={dash.showToast} saveStatus={dash.saveStatus} onSave={dash.save} />
        </div>

        {/* Reset */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 8px',
          borderLeft: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <button onClick={dash.reset} title="Reset to defaults" style={{
            padding: '3px 6px', fontSize: 10, borderRadius: 6,
            background: 'transparent', border: 'none', cursor: 'pointer',
            color: 'var(--t-text3)', transition: 'all 0.12s',
          }}
            onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-text1)'; e.currentTarget.style.background = 'var(--t-hover)' }}
            onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.background = 'transparent' }}
          >↺</button>
        </div>
      </div>

      {/* ══ WIDGET PICKER DROPDOWN ══════════════════════════════════════ */}
      {showPicker && (
        <div style={{
          position: 'absolute', top: 82, right: 12, zIndex: 50,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 12, boxShadow: '0 16px 48px rgba(0,0,0,0.5)',
          padding: 10, width: 240, maxHeight: '80vh', overflowY: 'auto',
        }}>
          <div style={{
            display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8,
          }}>
            <span style={{
              fontSize: 9, fontWeight: 700, color: 'var(--t-text3)',
              fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.08em',
            }}>WIDGETS</span>
            <button onClick={() => setShowPicker(false)} style={{
              background: 'none', border: 'none', color: 'var(--t-text3)',
              cursor: 'pointer', fontSize: 12,
            }}>✕</button>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 3 }}>
            {ALL_WIDGET_IDS.map(id => {
              const reg = WIDGET_REGISTRY[id]
              const visible = dash.isVisible(id)
              return (
                <button key={id} onClick={() => dash.setWidgetVisible(id, !visible)} style={{
                  display: 'flex', alignItems: 'center', gap: 6,
                  padding: '5px 8px', borderRadius: 6, fontSize: 11,
                  textAlign: 'left', cursor: 'pointer', transition: 'all 0.12s',
                  background: visible ? 'var(--t-hover)' : 'transparent',
                  border: `1px solid ${visible ? 'var(--t-accent)' + '30' : 'transparent'}`,
                  color: visible ? 'var(--t-accent)' : 'var(--t-text3)',
                }}>
                  <span style={{
                    width: 6, height: 6, borderRadius: '50%', flexShrink: 0,
                    background: visible ? 'var(--t-accent)' : 'var(--t-text3)',
                    opacity: visible ? 1 : 0.3,
                  }} />
                  {reg?.title ?? id}
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* ══ GRID ════════════════════════════════════════════════════════ */}
      <div id="grid-container" style={{ flex: 1, overflow: 'auto' }}>
        <GridLayout
          layout={dash.layout as any}
          cols={48}
          rowHeight={10}
          width={gridWidth}
          compactType={null}
          preventCollision={false}
          isDraggable
          isResizable
          onLayoutChange={handleLayoutChange as any}
          draggableHandle=".widget-drag-handle"
          margin={[4, 4]}
          containerPadding={[4, 4]}
        >
          {dash.layout.map(l => {
            const reg = WIDGET_REGISTRY[l.i]
            if (!reg) return null
            return (
              <div key={l.i}>
                <WidgetErrorBoundary>
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
                </WidgetErrorBoundary>
              </div>
            )
          })}
        </GridLayout>
      </div>

      {/* ══ FULLSCREEN OVERLAY ══════════════════════════════════════════ */}
      {fullscreen && (() => {
        const reg = WIDGET_REGISTRY[fullscreen]
        if (!reg) return null
        return (
          <div
            style={{
              position: 'fixed', inset: 0, zIndex: 9998,
              background: 'rgba(0,0,0,0.8)', backdropFilter: 'blur(4px)',
              display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16,
            }}
            onClick={e => { if (e.target === e.currentTarget) setFullscreen(null) }}
          >
            <div style={{
              width: '100%', height: '100%', maxWidth: '96vw', maxHeight: '96vh',
              background: 'var(--t-surface)', border: '1px solid var(--t-border)',
              borderRadius: 14, overflow: 'hidden',
              display: 'flex', flexDirection: 'column',
              boxShadow: '0 32px 64px rgba(0,0,0,0.6)',
            }}>
              <div style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '0 16px', height: 36, flexShrink: 0,
                borderBottom: '1px solid var(--t-border)',
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span style={{
                    fontSize: 11, fontWeight: 700, color: 'var(--t-text1)',
                    letterSpacing: '0.06em', textTransform: 'uppercase',
                    fontFamily: "'JetBrains Mono', monospace",
                  }}>{reg.title}</span>
                  <span style={{ fontSize: 10, color: 'var(--t-text3)' }}>Fullscreen · ESC to exit</span>
                </div>
                <button onClick={() => setFullscreen(null)} style={{
                  padding: '4px 8px', fontSize: 11, borderRadius: 6,
                  background: 'transparent', border: 'none', cursor: 'pointer',
                  color: 'var(--t-text3)', transition: 'all 0.12s',
                }}
                  onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.color = 'var(--t-text1)' }}
                  onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--t-text3)' }}
                >✕</button>
              </div>
              <div style={{ flex: 1, overflow: 'auto', padding: 4 }}>
                <reg.component {...sharedProps} colorGroup={dash.getColorGroup(fullscreen)} />
              </div>
            </div>
          </div>
        )
      })()}
    </div>
  )
}
