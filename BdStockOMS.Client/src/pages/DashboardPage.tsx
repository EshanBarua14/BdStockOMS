// @ts-nocheck
// src/pages/DashboardPage.tsx
// Day 52 — Dashboard with multi-page tabs, template system, price ticker
// Uses useTemplateStore (Zustand persist) for all state

import React, { useCallback, useState, useEffect } from "react"
import GridLayout from "react-grid-layout"
type Layout = { i: string; x: number; y: number; w: number; h: number }
import "react-grid-layout/css/styles.css"
import "react-resizable/css/styles.css"

import { useTemplateStore } from "../store/useTemplateStore"
import { useMarketData } from "../hooks/useMarketData"
import { useOrders } from "../hooks/useOrders"
import { WidgetPanel } from "../components/widgets/WidgetPanel"
import { WIDGET_REGISTRY } from "../components/widgets/registry"
import { DashboardTabs } from "../components/dashboard/DashboardTabs"
import { PriceTicker } from "../components/dashboard/PriceTicker"
import { TemplateManager } from "../components/dashboard/TemplateManager"

// ─── Error Boundary ───────────────────────────────────────────
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

// ─── Presets (quick-apply on active page) ─────────────────────
const PRESETS = ["Trading", "Research", "Portfolio"]

export default function DashboardPage() {
  const store = useTemplateStore()
  const template = store.getActiveTemplate()
  const page = store.getActivePage()
  const visibleLayout = store.getVisibleLayout()

  const market = useMarketData()
  const orders = useOrders()

  const [fullscreen, setFullscreen] = useState<string | null>(null)
  const [menuOpen, setMenuOpen] = useState<string | null>(null)
  const [showPicker, setShowPicker] = useState(false)
  const [showTemplates, setShowTemplates] = useState(false)
  const [gridWidth, setGridWidth] = useState(window.innerWidth)

  // Ensure active template is set on mount
  useEffect(() => {
    if (!store.activeTemplateId && store.templates.length > 0) {
      store.setActiveTemplate(store.templates[0].id)
    }
  }, [])

  // Grid container resize observer
  useEffect(() => {
    const obs = new ResizeObserver(entries => {
      for (const e of entries) setGridWidth(e.contentRect.width)
    })
    const el = document.getElementById("grid-container")
    if (el) obs.observe(el)
    return () => obs.disconnect()
  }, [])

  // ESC closes fullscreen
  useEffect(() => {
    const h = (e: KeyboardEvent) => { if (e.key === "Escape") setFullscreen(null) }
    window.addEventListener("keydown", h)
    return () => window.removeEventListener("keydown", h)
  }, [])

  const handleLayoutChange = useCallback((layout: Layout[]) => {
    store.updateLayout(layout)
  }, [store.updateLayout])

  const sharedProps = { marketData: market, ordersData: orders }

  // Widget IDs for the picker
  const allWidgetIds = Object.keys(WIDGET_REGISTRY)

  return (
    <div style={{
      display: 'flex', flexDirection: 'column', height: '100%',
      background: 'var(--t-bg)', color: 'var(--t-text1)',
      overflow: 'hidden', userSelect: 'none',
    }}>

      {/* ══ PRICE TICKER ════════════════════════════════════════════════ */}
      <PriceTicker />

      {/* ══ CONTROL BAR ═════════════════════════════════════════════════ */}
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
              fontSize: 9, fontWeight: 700,
              color: market.marketStatus.isOpen ? 'var(--t-buy)' : 'var(--t-text3)',
              fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.05em',
            }}>{market.marketStatus.label}</span>
            {market.marketStatus.activeStocks > 0 && (
              <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>
                · {market.marketStatus.activeStocks}
              </span>
            )}
          </div>
        </div>

        {/* Presets */}
        <div style={{
          display: 'flex', alignItems: 'center', gap: 3,
          padding: '0 8px', borderRight: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          {PRESETS.map(p => (
            <button key={p} onClick={() => store.applyPreset(p)} style={{
              padding: '3px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
              border: 'none', cursor: 'pointer', transition: 'all 0.12s',
              fontFamily: "'JetBrains Mono', monospace",
              background: 'var(--t-hover)', color: 'var(--t-text3)',
            }}
              onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-accent)'; e.currentTarget.style.color = '#000' }}
              onMouseLeave={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.color = 'var(--t-text3)' }}
            >{p}</button>
          ))}
        </div>

        {/* Template name (clickable to open manager) */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 10px',
          flex: 1, minWidth: 0, overflow: 'hidden',
        }}>
          <button onClick={() => setShowTemplates(true)} style={{
            display: 'flex', alignItems: 'center', gap: 6,
            background: 'none', border: 'none', cursor: 'pointer',
            color: 'var(--t-text2)', fontSize: 10, fontWeight: 600,
            fontFamily: "'JetBrains Mono', monospace", padding: '3px 8px',
            borderRadius: 6, transition: 'all 0.12s',
          }}
            onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.color = 'var(--t-accent)' }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--t-text2)' }}
          >
            <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {template?.name ?? 'No Template'}
            </span>
            <span style={{ fontSize: 8, color: 'var(--t-text3)' }}>▾</span>
          </button>
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

        {/* Templates button */}
        <div style={{
          display: 'flex', alignItems: 'center', padding: '0 8px',
          borderLeft: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <button onClick={() => setShowTemplates(true)} style={{
            padding: '3px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
            border: '1px solid var(--t-border)', cursor: 'pointer',
            transition: 'all 0.12s', fontFamily: "'JetBrains Mono', monospace",
            background: 'transparent', color: 'var(--t-text3)',
          }}
            onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-accent)'; e.currentTarget.style.borderColor = 'var(--t-accent)' }}
            onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
          >📁 Templates</button>
        </div>
      </div>

      {/* ══ DASHBOARD TABS (multi-page) ═════════════════════════════════ */}
      <DashboardTabs />

      {/* ══ WIDGET PICKER DROPDOWN ══════════════════════════════════════ */}
      {showPicker && (
        <div style={{
          position: 'absolute', top: 120, right: 12, zIndex: 50,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 12, boxShadow: '0 16px 48px rgba(0,0,0,0.5)',
          padding: 10, width: 260, maxHeight: '70vh', overflowY: 'auto',
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
            {allWidgetIds.map(id => {
              const reg = WIDGET_REGISTRY[id]
              if (!reg) return null
              const visible = page?.widgets.find(w => w.id === id)?.visible !== false
              return (
                <button key={id} onClick={() => store.setWidgetVisible(id, !visible)} style={{
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
                  {reg.title}
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* ══ GRID ════════════════════════════════════════════════════════ */}
      <div id="grid-container" style={{ flex: 1, overflow: 'auto' }}>
        {visibleLayout.length > 0 ? (
          <GridLayout
            layout={visibleLayout}
            cols={48}
            rowHeight={10}
            width={gridWidth}
            compactType={null}
            preventCollision={false}
            isDraggable
            isResizable
            onLayoutChange={handleLayoutChange}
            draggableHandle=".widget-drag-handle"
            margin={[4, 4]}
            containerPadding={[4, 4]}
          >
            {visibleLayout.map(l => {
              const reg = WIDGET_REGISTRY[l.i]
              if (!reg) return null
              const colorGroup = page?.widgets.find(w => w.id === l.i)?.colorGroup ?? null
              return (
                <div key={l.i}>
                  <WidgetErrorBoundary>
                    <WidgetPanel
                      id={l.i}
                      title={reg.title}
                      colorGroup={colorGroup}
                      onColorChange={c => store.setWidgetColor(l.i, c)}
                      onFullscreen={() => setFullscreen(l.i)}
                      onClose={() => store.setWidgetVisible(l.i, false)}
                      menuOpen={menuOpen === l.i}
                      onMenuToggle={() => setMenuOpen(p => p === l.i ? null : l.i)}
                    >
                      <reg.component {...sharedProps} colorGroup={colorGroup} />
                    </WidgetPanel>
                  </WidgetErrorBoundary>
                </div>
              )
            })}
          </GridLayout>
        ) : (
          <div style={{
            display: 'flex', flexDirection: 'column', alignItems: 'center',
            justifyContent: 'center', height: '100%', gap: 12, padding: 40,
          }}>
            <div style={{ fontSize: 40, opacity: 0.3 }}>📊</div>
            <div style={{ fontSize: 13, color: 'var(--t-text3)', fontWeight: 600 }}>
              No widgets on this page
            </div>
            <div style={{ fontSize: 11, color: 'var(--t-text3)', textAlign: 'center', maxWidth: 300 }}>
              Click "⊞ Widgets" to add widgets, or select a preset layout above.
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 8 }}>
              {PRESETS.map(p => (
                <button key={p} onClick={() => store.applyPreset(p)} style={{
                  padding: '6px 14px', fontSize: 11, borderRadius: 8, fontWeight: 600,
                  border: '1px solid var(--t-border)', cursor: 'pointer',
                  background: 'var(--t-hover)', color: 'var(--t-text2)',
                  fontFamily: "'JetBrains Mono', monospace", transition: 'all 0.12s',
                }}
                  onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--t-accent)'; e.currentTarget.style.color = 'var(--t-accent)' }}
                  onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--t-border)'; e.currentTarget.style.color = 'var(--t-text2)' }}
                >{p}</button>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* ══ FULLSCREEN OVERLAY ══════════════════════════════════════════ */}
      {fullscreen && (() => {
        const reg = WIDGET_REGISTRY[fullscreen]
        if (!reg) return null
        const colorGroup = page?.widgets.find(w => w.id === fullscreen)?.colorGroup ?? null
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
                <reg.component {...sharedProps} colorGroup={colorGroup} />
              </div>
            </div>
          </div>
        )
      })()}

      {/* ══ TEMPLATE MANAGER DRAWER ═════════════════════════════════════ */}
      <TemplateManager open={showTemplates} onClose={() => setShowTemplates(false)} />
    </div>
  )
}
