// @ts-nocheck
// src/pages/DashboardPage.tsx
// Day 55 — Full widget drawer, multi-instance, page management inline

import React, { useCallback, useState, useEffect, useRef } from "react"
import GridLayout from "react-grid-layout"
import "react-grid-layout/css/styles.css"
import "react-resizable/css/styles.css"

import { useTemplateStore } from "../store/useTemplateStore"
import { useMarketData } from "../hooks/useMarketData"
import { useOrders } from "../hooks/useOrders"
import { WidgetPanel } from "../components/widgets/WidgetPanel"
import { WIDGET_REGISTRY } from "../components/widgets/registry"
import { PriceTicker } from "../components/dashboard/PriceTicker"
import { BuySellConsoleEvents } from "../components/trading/BuySellConsole"
import { TemplateManager } from "../components/dashboard/TemplateManager"

const mono = "'JetBrains Mono', monospace"
const PRESETS = ["Trading", "Research", "Portfolio"]
const PAGE_ICONS = ["📊","📈","📉","💹","🔍","📋","⚡","🏦","💰","🎯","📌","🗂"]

// ─── Error Boundary ───────────────────────────────────────────
class WidgetErrorBoundary extends React.Component {
  state = { hasError: false, error: null }
  static getDerivedStateFromError(e) { return { hasError: true, error: e } }
  componentDidCatch(e, info) { console.error("Widget crash:", e, info) }
  render() {
    if (this.state.hasError) return (
      <div style={{ padding: 12, color: 'var(--t-sell)', fontSize: 11, fontFamily: mono, background: 'var(--t-surface)', height: '100%' }}>
        <div style={{ fontWeight: 700, marginBottom: 4 }}>Widget Error</div>
        <div style={{ color: 'var(--t-text3)', wordBreak: 'break-all' }}>{String(this.state.error)}</div>
      </div>
    )
    return this.props.children
  }
}

// ─── Widget Drawer (sidebar) ──────────────────────────────────
function WidgetDrawer({ open, onClose, onAdd }: { open: boolean; onClose: () => void; onAdd: (id: string) => void }) {
  const [search, setSearch] = useState('')
  const [dragging, setDragging] = useState<string | null>(null)

  const categories = [...new Set(Object.values(WIDGET_REGISTRY).map(r => r.category).filter(Boolean))]
  const filtered = Object.entries(WIDGET_REGISTRY).filter(([id, reg]) =>
    !search || reg.title.toLowerCase().includes(search.toLowerCase()) || id.includes(search.toLowerCase())
  )
  const byCategory = categories.map(cat => ({
    cat,
    items: filtered.filter(([, r]) => r.category === cat),
  })).filter(g => g.items.length > 0)

  if (!open) return null

  return (
    <>
      <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 48, background: 'transparent' }} />
      <div style={{
        position: 'fixed', top: 0, right: 0, bottom: 0, zIndex: 49,
        width: 280, background: 'var(--t-elevated)',
        borderLeft: '1px solid var(--t-border)',
        display: 'flex', flexDirection: 'column',
        boxShadow: '-16px 0 48px rgba(0,0,0,0.5)',
        animation: 'oms-slide-up 0.18s ease',
      }}>
        {/* Header */}
        <div style={{ padding: '14px 16px 10px', borderBottom: '1px solid var(--t-border)', flexShrink: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
            <span style={{ fontSize: 11, fontWeight: 800, color: 'var(--t-text1)', fontFamily: mono, letterSpacing: '0.06em' }}>⊞ WIDGETS</span>
            <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 16 }}>✕</button>
          </div>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search widgets…"
            style={{ width: '100%', boxSizing: 'border-box', background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 6, padding: '6px 10px', color: 'var(--t-text1)', fontSize: 11, fontFamily: mono, outline: 'none' }}
            onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
            onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
          />
          <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono, marginTop: 6 }}>Click or drag to add · Multiple instances allowed</div>
        </div>

        {/* Widget list */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '8px 12px' }}>
          {byCategory.map(({ cat, items }) => (
            <div key={cat} style={{ marginBottom: 14 }}>
              <div style={{ fontSize: 8, fontWeight: 700, color: 'var(--t-text3)', fontFamily: mono, letterSpacing: '0.1em', marginBottom: 6, paddingLeft: 4 }}>{cat?.toUpperCase()}</div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                {items.map(([id, reg]) => (
                  <div key={id}
                    draggable
                    onDragStart={e => { setDragging(id); e.dataTransfer.setData('widgetId', id) }}
                    onDragEnd={() => setDragging(null)}
                    onClick={() => { onAdd(id); onClose() }}
                    style={{
                      display: 'flex', alignItems: 'center', gap: 10,
                      padding: '8px 10px', borderRadius: 8, cursor: 'pointer',
                      background: dragging === id ? 'var(--t-hover)' : 'transparent',
                      border: '1px solid transparent',
                      transition: 'all 0.1s',
                    }}
                    onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
                    onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.borderColor = 'transparent' }}
                  >
                    <span style={{ fontSize: 16, flexShrink: 0 }}>{reg.icon ?? '📦'}</span>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)', fontFamily: mono }}>{reg.title}</div>
                      <div style={{ fontSize: 9, color: 'var(--t-text3)' }}>{reg.minW}×{reg.minH} min · drag or click</div>
                    </div>
                    <span style={{ fontSize: 10, color: 'var(--t-text3)', padding: '2px 6px', background: 'var(--t-surface)', borderRadius: 4, fontFamily: mono }}>+</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </>
  )
}

// ─── Page Tab (inline rename/delete) ─────────────────────────
function PageTab({ page, active, onClick, onRename, onDelete, onIconChange, canDelete }: any) {
  const [editing, setEditing] = useState(false)
  const [val, setVal] = useState(page.name)
  const [showMenu, setShowMenu] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const menuRef  = useRef<HTMLDivElement>(null)

  useEffect(() => { if (editing) setTimeout(() => inputRef.current?.select(), 10) }, [editing])
  useEffect(() => {
    const h = (e: MouseEvent) => { if (menuRef.current && !menuRef.current.contains(e.target as Node)) setShowMenu(false) }
    document.addEventListener('mousedown', h); return () => document.removeEventListener('mousedown', h)
  }, [])

  const commit = () => { setEditing(false); if (val.trim()) onRename(val.trim()) }

  return (
    <div style={{ position: 'relative', flexShrink: 0 }}>
      <div
        onClick={onClick}
        onDoubleClick={() => { setEditing(true); setVal(page.name) }}
        onContextMenu={e => { e.preventDefault(); setShowMenu(true) }}
        style={{
          display: 'flex', alignItems: 'center', gap: 5,
          padding: '4px 10px', cursor: 'pointer', userSelect: 'none',
          fontSize: 11, fontWeight: active ? 700 : 500,
          fontFamily: mono, borderBottom: `2px solid ${active ? 'var(--t-accent)' : 'transparent'}`,
          color: active ? 'var(--t-accent)' : 'var(--t-text3)',
          transition: 'all 0.1s', whiteSpace: 'nowrap',
        }}
      >
        <span>{page.icon}</span>
        {editing ? (
          <input ref={inputRef} value={val} onChange={e => setVal(e.target.value)}
            onBlur={commit} onKeyDown={e => { if (e.key === 'Enter') commit(); if (e.key === 'Escape') setEditing(false) }}
            onClick={e => e.stopPropagation()}
            style={{ width: 80, background: 'var(--t-hover)', border: '1px solid var(--t-accent)', borderRadius: 4, padding: '0 4px', color: 'var(--t-text1)', fontSize: 11, fontFamily: mono, outline: 'none' }}
          />
        ) : (
          <span>{page.name}</span>
        )}
      </div>

      {/* Right-click menu */}
      {showMenu && (
        <div ref={menuRef} style={{
          position: 'absolute', top: '100%', left: 0, zIndex: 200,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 8, overflow: 'hidden', boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
          minWidth: 160,
        }}>
          {[
            { label: '✏️ Rename', action: () => { setEditing(true); setVal(page.name); setShowMenu(false) } },
            ...PAGE_ICONS.slice(0, 8).map(icon => ({
              label: `${icon} Set icon`,
              action: () => { onIconChange(icon); setShowMenu(false) }
            })).slice(0, 1),
            { label: '🎨 Change Icon', action: () => setShowMenu(false), isSubmenu: true, submenu: PAGE_ICONS.map(icon => ({ label: icon, action: () => { onIconChange(icon); setShowMenu(false) } })) },
            ...(canDelete ? [{ label: '🗑 Delete', action: () => { onDelete(); setShowMenu(false) }, danger: true }] : []),
          ].filter((item: any) => !item.isSubmenu).map((item: any, i) => (
            <button key={i} onClick={item.action} style={{
              display: 'block', width: '100%', padding: '8px 14px', textAlign: 'left',
              background: 'transparent', border: 'none', cursor: 'pointer',
              fontSize: 11, fontFamily: mono, color: item.danger ? 'var(--t-sell)' : 'var(--t-text1)',
              borderBottom: '1px solid var(--t-border)',
            }}
              onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
              onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
            >{item.label}</button>
          ))}
          {/* Icon picker row */}
          <div style={{ padding: '6px 8px', display: 'flex', flexWrap: 'wrap', gap: 4 }}>
            {PAGE_ICONS.map(icon => (
              <button key={icon} onClick={() => { onIconChange(icon); setShowMenu(false) }}
                style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 14, padding: 2, borderRadius: 4 }}
                onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
                onMouseLeave={e => e.currentTarget.style.background = 'none'}
              >{icon}</button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Main Dashboard ───────────────────────────────────────────
export default function DashboardPage() {
  const store        = useTemplateStore()
  const template     = store.getActiveTemplate()
  const page         = store.getActivePage()
  const layout       = store.getVisibleLayout()

  const market       = useMarketData()
  const orders       = useOrders()

  const [fullscreen, setFullscreen]     = useState<string | null>(null)
  const [menuOpen, setMenuOpen]         = useState<string | null>(null)
  const [showDrawer, setShowDrawer]     = useState(false)
  const [showTemplates, setShowTemplates] = useState(false)
  const [gridWidth, setGridWidth]       = useState(window.innerWidth - 0)
  const gridRef = useRef<HTMLDivElement>(null)

  // Ensure activeTemplateId is set
  useEffect(() => {
    if (!store.activeTemplateId && store.templates.length > 0) {
      store.setActiveTemplate(store.templates[0].id)
    }
  }, [])

  // Grid width observer
  useEffect(() => {
    const el = document.getElementById('grid-container')
    if (!el) return
    const ro = new ResizeObserver(([e]) => setGridWidth(e.contentRect.width))
    ro.observe(el)
    return () => ro.disconnect()
  }, [])

  // ESC closes fullscreen
  useEffect(() => {
    const h = (e: KeyboardEvent) => { if (e.key === 'Escape') setFullscreen(null) }
    window.addEventListener('keydown', h); return () => window.removeEventListener('keydown', h)
  }, [])

  const sharedProps = { market, orders }

  const handleLayoutChange = useCallback((newLayout: any[]) => {
    store.updateLayout(newLayout)
  }, [store])

  const handleAddWidget = useCallback((widgetId: string) => {
    if (widgetId === 'buysell') { BuySellConsoleEvents.open('BUY'); return }
    store.addWidgetInstance(widgetId)
  }, [store])

  const handleRemoveInstance = useCallback((instanceId: string) => {
    store.removeWidgetInstance(instanceId)
  }, [store])

  const pages = template?.pages ?? []

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', overflow: 'hidden', background: 'var(--t-bg)', position: 'relative' }}>

      {/* ══ PRICE TICKER ══════════════════════════════════════════════ */}
      <PriceTicker ticks={market.ticksArray} />

      {/* ══ TOOLBAR ════════════════════════════════════════════════════ */}
      <div style={{ display: 'flex', alignItems: 'center', height: 36, flexShrink: 0, borderBottom: '1px solid var(--t-border)', background: 'var(--t-surface)', padding: '0 8px', gap: 4, overflow: 'hidden' }}>

        {/* Template name */}
        <button onClick={() => setShowTemplates(true)} style={{
          display: 'flex', alignItems: 'center', gap: 4, background: 'none', border: 'none',
          cursor: 'pointer', color: 'var(--t-text2)', fontSize: 10, fontWeight: 600,
          fontFamily: mono, padding: '3px 8px', borderRadius: 6, flexShrink: 0,
          transition: 'all 0.12s', maxWidth: 140, overflow: 'hidden',
        }}
          onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.color = 'var(--t-accent)' }}
          onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--t-text2)' }}
        >
          <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{template?.name ?? 'Workspace'}</span>
          <span style={{ fontSize: 8, color: 'var(--t-text3)', flexShrink: 0 }}>▾</span>
        </button>

        <div style={{ width: 1, height: 18, background: 'var(--t-border)', flexShrink: 0 }} />

        {/* Page tabs */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 0, flex: 1, overflow: 'hidden', overflowX: 'auto', scrollbarWidth: 'none' }}>
          {pages.map(p => (
            <PageTab key={p.id} page={p} active={p.id === template?.activePageId}
              onClick={() => store.setActivePage(p.id)}
              onRename={name => store.renamePage(p.id, name)}
              onDelete={() => store.deletePage(p.id)}
              onIconChange={icon => store.setPageIcon(p.id, icon)}
              canDelete={pages.length > 1}
            />
          ))}
          {/* Add page button */}
          <button onClick={() => {
            const name = `Page ${pages.length + 1}`
            store.addPage(name, 'Trading')
          }}
            style={{ flexShrink: 0, padding: '4px 8px', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--t-text3)', fontSize: 16, lineHeight: 1, borderRadius: 4 }}
            title="Add new page"
            onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-accent)'; e.currentTarget.style.background = 'var(--t-hover)' }}
            onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.background = 'none' }}
          >＋</button>
        </div>

        <div style={{ width: 1, height: 18, background: 'var(--t-border)', flexShrink: 0 }} />

        {/* Preset buttons */}
        <div style={{ display: 'flex', gap: 3, flexShrink: 0 }}>
          {PRESETS.map(p => (
            <button key={p} onClick={() => store.applyPreset(p)} style={{
              padding: '2px 7px', fontSize: 9, borderRadius: 5, fontWeight: 600,
              border: '1px solid var(--t-border)', cursor: 'pointer',
              background: 'transparent', color: 'var(--t-text3)', fontFamily: mono,
              transition: 'all 0.1s',
            }}
              onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--t-accent)'; e.currentTarget.style.color = 'var(--t-accent)' }}
              onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--t-border)'; e.currentTarget.style.color = 'var(--t-text3)' }}
            >{p}</button>
          ))}
        </div>

        <div style={{ width: 1, height: 18, background: 'var(--t-border)', flexShrink: 0 }} />

        {/* Widgets button */}
        <button onClick={() => setShowDrawer(d => !d)} style={{
          padding: '3px 10px', fontSize: 10, borderRadius: 6, fontWeight: 700,
          border: `1px solid ${showDrawer ? 'var(--t-accent)' : 'var(--t-border)'}`,
          cursor: 'pointer', transition: 'all 0.12s', fontFamily: mono, flexShrink: 0,
          background: showDrawer ? 'rgba(var(--t-accent-rgb),0.1)' : 'transparent',
          color: showDrawer ? 'var(--t-accent)' : 'var(--t-text3)',
        }}>⊞ Widgets</button>

        {/* Templates button */}
        <button onClick={() => setShowTemplates(true)} style={{
          padding: '3px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
          border: '1px solid var(--t-border)', cursor: 'pointer',
          fontFamily: mono, flexShrink: 0,
          background: 'transparent', color: 'var(--t-text3)', transition: 'all 0.1s',
        }}
          onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-accent)'; e.currentTarget.style.borderColor = 'var(--t-accent)' }}
          onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
        >📁 Templates</button>
      </div>

      {/* ══ GRID AREA ═══════════════════════════════════════════════════ */}
      <div id="grid-container" ref={gridRef} style={{ flex: 1, overflow: 'auto', position: 'relative' }}
        onDragOver={e => e.preventDefault()}
        onDrop={e => {
          e.preventDefault()
          const widgetId = e.dataTransfer.getData('widgetId')
          if (widgetId) handleAddWidget(widgetId)
        }}
      >
        {layout.length > 0 ? (
          <GridLayout
            layout={layout}
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
            {layout.map(l => {
              const instance = page?.instances?.find(i => i.instanceId === l.i)
              const widgetId = instance?.widgetId ?? l.i.replace(/-\d+$/, '')
              const reg = WIDGET_REGISTRY[widgetId]
              if (!reg) return null
              const colorGroup = instance?.colorGroup ?? null
              return (
                <div key={l.i}>
                  <WidgetErrorBoundary>
                    <WidgetPanel
                      id={l.i}
                      title={reg.title}
                      colorGroup={colorGroup}
                      onColorChange={c => store.setWidgetColor(l.i, c)}
                      onFullscreen={() => setFullscreen(l.i)}
                      onClose={() => handleRemoveInstance(l.i)}
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
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: 12, padding: 40 }}>
            <div style={{ fontSize: 40, opacity: 0.3 }}>📊</div>
            <div style={{ fontSize: 13, color: 'var(--t-text3)', fontWeight: 600 }}>No widgets on this page</div>
            <div style={{ fontSize: 11, color: 'var(--t-text3)', textAlign: 'center', maxWidth: 300 }}>
              Click "⊞ Widgets" to add widgets, or drag them from the panel.
            </div>
            <div style={{ display: 'flex', gap: 6, marginTop: 8, flexWrap: 'wrap', justifyContent: 'center' }}>
              {PRESETS.map(p => (
                <button key={p} onClick={() => store.applyPreset(p)} style={{
                  padding: '6px 14px', fontSize: 11, borderRadius: 8, fontWeight: 600,
                  border: '1px solid var(--t-border)', cursor: 'pointer',
                  background: 'var(--t-hover)', color: 'var(--t-text2)', fontFamily: mono,
                }}
                  onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--t-accent)'; e.currentTarget.style.color = 'var(--t-accent)' }}
                  onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--t-border)'; e.currentTarget.style.color = 'var(--t-text2)' }}
                >{p}</button>
              ))}
              <button onClick={() => setShowDrawer(true)} style={{
                padding: '6px 14px', fontSize: 11, borderRadius: 8, fontWeight: 700,
                border: '1px solid var(--t-accent)', cursor: 'pointer',
                background: 'rgba(var(--t-accent-rgb),0.1)', color: 'var(--t-accent)', fontFamily: mono,
              }}>⊞ Open Widget Drawer</button>
            </div>
          </div>
        )}
      </div>

      {/* ══ WIDGET DRAWER ════════════════════════════════════════════════ */}
      <WidgetDrawer open={showDrawer} onClose={() => setShowDrawer(false)} onAdd={handleAddWidget} />

      {/* ══ FULLSCREEN OVERLAY ══════════════════════════════════════════ */}
      {fullscreen && (() => {
        const instance = page?.instances?.find(i => i.instanceId === fullscreen)
        const widgetId = instance?.widgetId ?? fullscreen.replace(/-\d+$/, '')
        const reg = WIDGET_REGISTRY[widgetId]
        if (!reg) return null
        const colorGroup = instance?.colorGroup ?? null
        return (
          <div style={{ position: 'fixed', inset: 0, zIndex: 9998, background: 'rgba(0,0,0,0.8)', backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16 }}
            onClick={e => { if (e.target === e.currentTarget) setFullscreen(null) }}>
            <div style={{ width: '100%', height: '100%', maxWidth: '96vw', maxHeight: '96vh', background: 'var(--t-surface)', border: '1px solid var(--t-border)', borderRadius: 14, overflow: 'hidden', display: 'flex', flexDirection: 'column', boxShadow: '0 32px 64px rgba(0,0,0,0.6)' }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '0 16px', height: 36, flexShrink: 0, borderBottom: '1px solid var(--t-border)' }}>
                <span style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)', fontFamily: mono, letterSpacing: '0.06em', textTransform: 'uppercase' }}>{reg.title} · Fullscreen · ESC</span>
                <button onClick={() => setFullscreen(null)} style={{ padding: '4px 8px', fontSize: 11, borderRadius: 6, background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--t-text3)' }}>✕</button>
              </div>
              <div style={{ flex: 1, overflow: 'auto', padding: 4 }}>
                <reg.component {...sharedProps} colorGroup={colorGroup} />
              </div>
            </div>
          </div>
        )
      })()}

      {/* ══ TEMPLATE MANAGER ════════════════════════════════════════════ */}
      <TemplateManager open={showTemplates} onClose={() => setShowTemplates(false)} />
    </div>
  )
}
