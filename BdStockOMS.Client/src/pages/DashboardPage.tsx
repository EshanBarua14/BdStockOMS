// @ts-nocheck
// src/pages/DashboardPage.tsx — Day 56

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
import { TemplateManager } from "../components/dashboard/TemplateManager"

const mono = "'JetBrains Mono', monospace"
const PAGE_ICONS = ["📊","📈","📉","💹","🔍","📋","⚡","🏦","💰","🎯","📌","🗂","💼","🛡️","📰","🤖","⚖️","🚀"]

// ── Error Boundary ──────────────────────────────────────────────────────────
class WidgetErrorBoundary extends React.Component {
  state = { hasError: false, error: null }
  static getDerivedStateFromError(e) { return { hasError: true, error: e } }
  componentDidCatch(e, i) { console.error("Widget crash:", e, i) }
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

// ── Widget Drawer ───────────────────────────────────────────────────────────
function WidgetDrawer({ open, onClose, onAdd }) {
  const [search, setSearch] = useState('')
  const categories = [...new Set(Object.values(WIDGET_REGISTRY).map(r => r.category).filter(Boolean))]
  const filtered = Object.entries(WIDGET_REGISTRY).filter(([id, reg]) =>
    !search || reg.title.toLowerCase().includes(search.toLowerCase())
  )
  const byCategory = categories
    .map(cat => ({ cat, items: filtered.filter(([, r]) => r.category === cat) }))
    .filter(g => g.items.length > 0)

  if (!open) return null
  return (
    <>
      <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 48 }} />
      <div style={{ position: 'fixed', top: 0, right: 0, bottom: 0, zIndex: 49, width: 260, background: 'var(--t-elevated)', borderLeft: '1px solid var(--t-border)', display: 'flex', flexDirection: 'column', boxShadow: '-16px 0 48px rgba(0,0,0,0.5)' }}>
        <div style={{ padding: '14px 14px 10px', borderBottom: '1px solid var(--t-border)', flexShrink: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
            <span style={{ fontSize: 11, fontWeight: 800, color: 'var(--t-text1)', fontFamily: mono }}>⊞ ADD WIDGETS</span>
            <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 16 }}>✕</button>
          </div>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search…"
            style={{ width: '100%', boxSizing: 'border-box', background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 6, padding: '6px 10px', color: 'var(--t-text1)', fontSize: 11, fontFamily: mono, outline: 'none' }}
            onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
            onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
          />
          <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono, marginTop: 5 }}>Click to add · Drag onto grid · Add multiple</div>
        </div>
        <div style={{ flex: 1, overflowY: 'auto', padding: '8px 10px' }}>
          {byCategory.map(({ cat, items }) => (
            <div key={cat} style={{ marginBottom: 12 }}>
              <div style={{ fontSize: 8, fontWeight: 700, color: 'var(--t-text3)', fontFamily: mono, letterSpacing: '0.1em', marginBottom: 5, paddingLeft: 2 }}>{cat && cat.toUpperCase()}</div>
              {items.map(([id, reg]) => (
                  <div key={id}
                    draggable={true}
                    onDragStart={e => e.dataTransfer.setData('widgetId', id)}
                    onClick={() => { onAdd(id); onClose() }}
                    style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 8px', borderRadius: 7, cursor: 'pointer', border: '1px solid transparent', background: 'transparent', marginBottom: 2, transition: 'all 0.1s' }}
                    onMouseEnter={e => { e.currentTarget.style.background = 'var(--t-hover)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
                    onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.borderColor = 'transparent' }}
                  >
                    <span style={{ fontSize: 15, flexShrink: 0 }}>{reg.icon}</span>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)', fontFamily: mono }}>{reg.title}</div>
                      <div style={{ fontSize: 9, color: 'var(--t-text3)' }}>{reg.defaultW}x{reg.defaultH} · drag or click</div>
                    </div>
                    <span style={{ fontSize: 10, color: 'var(--t-text3)', fontFamily: mono, flexShrink: 0 }}>+</span>
                  </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </>
  )
}

// ── Page Drawer ─────────────────────────────────────────────────────────────
function PageDrawer({ open, onClose, pages, activePageId, onSelect, onAdd, onRename, onDelete, onIconChange }) {
  const [editingId, setEditingId]     = useState(null)
  const [editVal, setEditVal]         = useState('')
  const [showIconFor, setShowIconFor] = useState(null)
  const inputRef = useRef(null)

  useEffect(() => { if (editingId && inputRef.current) inputRef.current.select() }, [editingId])

  const startEdit = (pg) => { setEditingId(pg.id); setEditVal(pg.name) }
  const commitEdit = () => { if (editVal.trim()) onRename(editingId, editVal.trim()); setEditingId(null) }

  if (!open) return null
  return (
    <>
      <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 48 }} />
      <div style={{ position: 'fixed', top: 0, left: 0, bottom: 0, zIndex: 49, width: 240, background: 'var(--t-elevated)', borderRight: '1px solid var(--t-border)', display: 'flex', flexDirection: 'column', boxShadow: '16px 0 48px rgba(0,0,0,0.5)' }}>
        <div style={{ padding: '14px 14px 10px', borderBottom: '1px solid var(--t-border)', flexShrink: 0, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <span style={{ fontSize: 11, fontWeight: 800, color: 'var(--t-text1)', fontFamily: mono }}>📋 PAGES</span>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 16 }}>✕</button>
        </div>
        <div style={{ flex: 1, overflowY: 'auto', padding: '8px 10px' }}>
          {pages.map(pg => (
            <div key={pg.id} style={{ borderRadius: 8, marginBottom: 6, border: `1px solid ${pg.id === activePageId ? 'var(--t-accent)40' : 'var(--t-border)'}`, background: pg.id === activePageId ? 'var(--t-hover)' : 'transparent' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '8px 10px' }}>
                {/* Icon picker */}
                <div style={{ position: 'relative', flexShrink: 0 }}>
                  <button onClick={() => setShowIconFor(showIconFor === pg.id ? null : pg.id)}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 16, padding: 0, lineHeight: 1 }} title="Change icon"
                  >{pg.icon}</button>
                  {showIconFor === pg.id && (
                    <div style={{ position: 'absolute', top: '100%', left: 0, zIndex: 300, background: 'var(--t-elevated)', border: '1px solid var(--t-border)', borderRadius: 8, padding: 6, display: 'flex', flexWrap: 'wrap', gap: 3, width: 180, boxShadow: '0 8px 24px rgba(0,0,0,0.5)', marginTop: 4 }}>
                      {PAGE_ICONS.map(icon => (
                        <button key={icon} onClick={() => { onIconChange(pg.id, icon); setShowIconFor(null) }}
                          style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 16, padding: 3, borderRadius: 4 }}
                          onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
                          onMouseLeave={e => e.currentTarget.style.background = 'none'}
                        >{icon}</button>
                      ))}
                    </div>
                  )}
                </div>
                {/* Name / edit input */}
                {editingId === pg.id ? (
                  <input ref={inputRef} value={editVal} onChange={e => setEditVal(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter') commitEdit(); if (e.key === 'Escape') setEditingId(null) }}
                    style={{ flex: 1, background: 'var(--t-hover)', border: '1px solid var(--t-accent)', borderRadius: 4, padding: '2px 6px', color: 'var(--t-text1)', fontSize: 11, fontFamily: mono, outline: 'none' }}
                  />
                ) : (
                  <span onClick={() => { onSelect(pg.id); onClose() }}
                    style={{ flex: 1, fontSize: 11, fontWeight: pg.id === activePageId ? 700 : 500, color: pg.id === activePageId ? 'var(--t-accent)' : 'var(--t-text1)', cursor: 'pointer', fontFamily: mono, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {pg.name}
                  </span>
                )}
              </div>
              {/* Action buttons */}
              <div style={{ display: 'flex', gap: 4, padding: '0 10px 8px', justifyContent: 'flex-end' }}>
                {editingId === pg.id ? (
                  <>
                    <button onClick={commitEdit} style={{ padding: '3px 10px', fontSize: 9, fontWeight: 800, borderRadius: 4, border: 'none', cursor: 'pointer', background: 'var(--t-buy)', color: '#000', fontFamily: mono }}>✓ Save</button>
                    <button onClick={() => setEditingId(null)} style={{ padding: '3px 8px', fontSize: 9, borderRadius: 4, border: '1px solid var(--t-border)', cursor: 'pointer', background: 'transparent', color: 'var(--t-text3)', fontFamily: mono }}>Cancel</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => startEdit(pg)} style={{ padding: '3px 8px', fontSize: 9, borderRadius: 4, border: '1px solid var(--t-border)', cursor: 'pointer', background: 'transparent', color: 'var(--t-text2)', fontFamily: mono }}>✏ Edit</button>
                    {pages.length > 1 && (
                      <button onClick={() => onDelete(pg.id)}
                        style={{ padding: '3px 8px', fontSize: 9, borderRadius: 4, border: '1px solid transparent', cursor: 'pointer', background: 'transparent', color: 'var(--t-sell)', fontFamily: mono }}
                        onMouseEnter={e => e.currentTarget.style.borderColor = 'var(--t-sell)'}
                        onMouseLeave={e => e.currentTarget.style.borderColor = 'transparent'}
                      >🗑 Delete</button>
                    )}
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
        <div style={{ padding: '10px 14px', borderTop: '1px solid var(--t-border)', flexShrink: 0 }}>
          <button onClick={() => { onAdd(); onClose() }}
            style={{ width: '100%', padding: '9px', fontSize: 11, fontWeight: 700, borderRadius: 8, border: '1px dashed var(--t-border)', cursor: 'pointer', background: 'transparent', color: 'var(--t-text2)', fontFamily: mono, transition: 'all 0.1s' }}
            onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--t-accent)'; e.currentTarget.style.color = 'var(--t-accent)' }}
            onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--t-border)'; e.currentTarget.style.color = 'var(--t-text2)' }}
          >＋ New Page</button>
        </div>
      </div>
    </>
  )
}

// ── Main Dashboard ──────────────────────────────────────────────────────────

// ── Inline page tab with right-click menu ─────────────────────────────────
function PageTab({ page, isActive, onSelect, onRename, onDelete, onDuplicate, onIconChange }: any) {
  const [menu, setMenu] = useState<{x:number,y:number}|null>(null)
  const [renaming, setRenaming] = useState(false)
  const [renameVal, setRenameVal] = useState(page.name)
  const ref = useRef<HTMLInputElement>(null)
  const mono = "'JetBrains Mono', monospace"

  useEffect(() => { if (renaming && ref.current) ref.current.focus() }, [renaming])
  useEffect(() => { if (menu) { const close = () => setMenu(null); window.addEventListener('click', close); return () => window.removeEventListener('click', close) } }, [menu])

  const commitRename = () => { if (renameVal.trim()) onRename(renameVal.trim()); setRenaming(false) }

  if (renaming) return (
    <input ref={ref} value={renameVal} onChange={e => setRenameVal(e.target.value)}
      onBlur={commitRename} onKeyDown={e => { if (e.key === 'Enter') commitRename(); if (e.key === 'Escape') setRenaming(false) }}
      id={`page-rename-${page.id}`} name={`page-rename-${page.id}`}
      style={{ width: 80, fontSize: 10, fontFamily: mono, background: 'var(--t-hover)', border: '1px solid var(--t-accent)', borderRadius: 4, padding: '2px 6px', color: 'var(--t-text1)', outline: 'none' }}
    />
  )

  return (
    <div style={{ position: 'relative', flexShrink: 0 }}>
      <button onClick={onSelect}
        onContextMenu={e => { e.preventDefault(); setMenu({ x: e.clientX, y: e.clientY }) }}
        onDoubleClick={() => { setRenameVal(page.name); setRenaming(true) }}
        style={{
          padding: '3px 10px', fontSize: 10, fontWeight: isActive ? 700 : 400,
          border: 'none', borderBottom: `2px solid ${isActive ? 'var(--t-accent)' : 'transparent'}`,
          background: 'transparent', cursor: 'pointer', fontFamily: mono,
          color: isActive ? 'var(--t-accent)' : 'var(--t-text3)',
          transition: 'all 0.1s', whiteSpace: 'nowrap', borderRadius: 0,
        }}>{page.icon} {page.name}</button>
      {menu && (
        <div onClick={e => e.stopPropagation()} style={{
          position: 'fixed', left: menu.x, top: menu.y, zIndex: 9999,
          background: 'var(--t-surface)', border: '1px solid var(--t-border)',
          borderRadius: 8, padding: '4px 0', minWidth: 140, boxShadow: '0 8px 32px rgba(0,0,0,0.4)',
        }}>
          {[
            { label: '✏️ Rename', action: () => { setRenaming(true); setMenu(null) } },
            { label: '📋 Duplicate', action: () => { onDuplicate(); setMenu(null) } },
            { label: '🔖 Save as template', action: () => {
              const cur = store.getActivePage()
              const name = window.prompt('Template name:', page?.name ?? 'My Template')
              if (name?.trim()) {
                const tid = store.createTemplate(name.trim())
                store.setActiveTemplate(tid)
                if (cur?.layout) store.updateLayout(cur.layout)
                setMenu(null)
                const toast = document.createElement('div')
                toast.textContent = '✅ Saved as template: ' + name
                toast.style.cssText = 'position:fixed;bottom:24px;left:50%;transform:translateX(-50%);background:#00D4AA;color:#000;padding:8px 20px;border-radius:8px;font-size:12px;font-weight:700;z-index:99999;font-family:monospace'
                document.body.appendChild(toast)
                setTimeout(() => toast.remove(), 2500)
              }
              setMenu(null)
            } },
            { label: '🗑️ Delete page', action: () => { onDelete(); setMenu(null) }, danger: true },
          ].map(item => (
            <button key={item.label} onClick={item.action} style={{
              display: 'block', width: '100%', textAlign: 'left', padding: '7px 14px',
              fontSize: 11, fontFamily: mono, background: 'none', border: 'none', cursor: 'pointer',
              color: item.danger ? 'var(--t-sell)' : 'var(--t-text1)',
            }}
              onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
              onMouseLeave={e => e.currentTarget.style.background = 'none'}
            >{item.label}</button>
          ))}
        </div>
      )}
    </div>
  )
}

export default function DashboardPage() {
  const store    = useTemplateStore()
  const template = store.getActiveTemplate()
  const page     = store.getActivePage()
  const layout   = store.getVisibleLayout()
  const market   = useMarketData()
  const orders   = useOrders()

  const [fullscreen, setFullscreen]       = useState(null)
  const [menuOpen, setMenuOpen]           = useState(null)
  const [showWidgets, setShowWidgets]     = useState(false)
  const [showPages, setShowPages]         = useState(false)
  const [showTemplates, setShowTemplates] = useState(false)
  const [gridWidth, setGridWidth]         = useState(window.innerWidth)
  const gridRef = useRef(null)

  useEffect(() => {
    if (!store.activeTemplateId && store.templates.length > 0) {
      store.setActiveTemplate(store.templates[0].id)
    }
  }, [store.activeTemplateId, store.templates.length])

  useEffect(() => {
    const el = document.getElementById('grid-container')
    if (!el) return
    const ro = new ResizeObserver(([e]) => setGridWidth(e.contentRect.width))
    ro.observe(el); return () => ro.disconnect()
  }, [])

  useEffect(() => {
    const h = (e) => { if (e.key === 'Escape') setFullscreen(null) }
    window.addEventListener('keydown', h); return () => window.removeEventListener('keydown', h)
  }, [])

  // Guard: if no template loaded yet, show loading or reset
  if (!template && store.templates.length === 0) {
    return (
      <div style={{ display:'flex', flexDirection:'column', alignItems:'center', justifyContent:'center', height:'100vh', gap:16, background:'var(--t-bg)' }}>
        <div style={{ fontSize:48, opacity:0.3 }}>📊</div>
        <div style={{ fontSize:14, color:'var(--t-text2)', fontFamily:"'JetBrains Mono',monospace" }}>No dashboard found</div>
        <button onClick={() => { localStorage.removeItem('bd_oms_templates_v1'); window.location.reload() }}
          style={{ padding:'10px 24px', fontSize:12, fontWeight:700, borderRadius:8, border:'1px solid var(--t-accent)', cursor:'pointer', background:'transparent', color:'var(--t-accent)', fontFamily:"'JetBrains Mono',monospace" }}>
          ↺ Reset Dashboard
        </button>
      </div>
    )
  }

  const sharedProps = { market, orders }
  const pages = template?.pages ?? []

  const handleLayoutChange = useCallback((newLayout) => {
    // Merge with existing layout to preserve w/h set by user
    // react-grid-layout can fire onLayoutChange with incomplete items on mount
    const existing = store.getActivePage()?.layout ?? []
    const merged = newLayout.map(nl => {
      const prev = existing.find(e => e.i === nl.i)
      if (!prev) return nl
      // Keep the larger of the two sizes to avoid shrinking on re-render
      return {
        ...nl,
        w: nl.w ?? prev.w,
        h: nl.h ?? prev.h,
        minW: nl.minW ?? prev.minW,
        minH: nl.minH ?? prev.minH,
      }
    })
    store.updateLayout(merged)
  }, [store])

  const handleAddWidget = useCallback((widgetId) => {

    store.addWidgetInstance(widgetId)
  }, [store])

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', overflow: 'hidden', background: 'var(--t-bg)' }}>
      <PriceTicker ticks={market.ticksArray} />

      {/* TOOLBAR */}
      <div style={{ display: 'flex', alignItems: 'center', height: 38, flexShrink: 0, borderBottom: '1px solid var(--t-border)', background: 'var(--t-surface)', padding: '0 10px', gap: 6 }}>
        {/* Pages button */}
        <button onClick={() => { setShowPages(p => !p); setShowWidgets(false) }} style={{
          display: 'flex', alignItems: 'center', gap: 5, padding: '4px 10px', fontSize: 10, fontWeight: 700, borderRadius: 6,
          border: `1px solid ${showPages ? 'var(--t-accent)' : 'var(--t-border)'}`,
          background: showPages ? 'rgba(var(--t-accent-rgb),0.1)' : 'transparent',
          color: showPages ? 'var(--t-accent)' : 'var(--t-text2)', cursor: 'pointer', fontFamily: mono, transition: 'all 0.1s', flexShrink: 0,
        }}>
          <span>{page?.icon ?? '📊'}</span>
          <span style={{ maxWidth: 100, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{page?.name ?? 'Pages'}</span>
          <span style={{ fontSize: 8, color: 'var(--t-text3)' }}>▾</span>
        </button>

        {/* Page tabs */}
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 0, overflow: 'hidden', overflowX: 'auto', scrollbarWidth: 'none' }}>
          {pages.map(p => (
            <PageTab key={p.id} page={p} isActive={p.id === template?.activePageId}
              onSelect={() => store.setActivePage(p.id)}
              onRename={(name) => store.renamePage(p.id, name)}
              onDelete={() => { if (pages.length > 1) store.deletePage(p.id) }}
              onDuplicate={() => { const cur = store.getActivePage(); store.addPage(`${p.name} Copy`, 'Trading', cur?.layout, cur?.instances) }}
              onIconChange={(icon) => store.setPageIcon(p.id, icon)}
            />
          ))}
          <button onClick={() => store.addPage(`Page ${pages.length + 1}`)}
            style={{ flexShrink: 0, padding: '3px 8px', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--t-text3)', fontSize: 14, borderRadius: 4 }}
            title="New page"
            onMouseEnter={e => e.currentTarget.style.color = 'var(--t-accent)'}
            onMouseLeave={e => e.currentTarget.style.color = 'var(--t-text3)'}
          >＋</button>
        </div>

        <div style={{ width: 1, height: 18, background: 'var(--t-border)', flexShrink: 0 }} />

        <button onClick={() => { setShowWidgets(w => !w); setShowPages(false) }} style={{
          padding: '4px 10px', fontSize: 10, fontWeight: 700, borderRadius: 6,
          border: `1px solid ${showWidgets ? 'var(--t-accent)' : 'var(--t-border)'}`,
          background: showWidgets ? 'rgba(var(--t-accent-rgb),0.1)' : 'transparent',
          color: showWidgets ? 'var(--t-accent)' : 'var(--t-text3)',
          cursor: 'pointer', fontFamily: mono, flexShrink: 0, transition: 'all 0.1s',
        }}>⊞ Widgets</button>

        <button onClick={() => setShowTemplates(true)} style={{
          padding: '4px 8px', fontSize: 10, borderRadius: 6, fontWeight: 600,
          border: '1px solid var(--t-border)', cursor: 'pointer', fontFamily: mono, flexShrink: 0,
          background: 'transparent', color: 'var(--t-text3)', transition: 'all 0.1s',
        }}
          onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-accent)'; e.currentTarget.style.borderColor = 'var(--t-accent)' }}
          onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
        >📁 Templates</button>
      </div>

      {/* GRID */}
      <div id="grid-container" ref={gridRef} style={{ flex: 1, overflow: 'auto', position: 'relative' }}
        onDragOver={e => e.preventDefault()}
        onDrop={e => { e.preventDefault(); const id = e.dataTransfer.getData('widgetId'); if (id) handleAddWidget(id) }}
      >
        {layout.length > 0 ? (
          <GridLayout layout={layout} cols={48} rowHeight={10} width={gridWidth}
            compactType={null} preventCollision={false} isDraggable isResizable
            resizeHandles={["se","sw","ne","nw","e","w","n","s"]}
            onLayoutChange={handleLayoutChange}
            onResizeStop={(layout) => store.updateLayout(layout)}
            onDragStop={(layout) => store.updateLayout(layout)}
            draggableHandle=".widget-drag-handle"
            margin={[4, 4]} containerPadding={[4, 4]}
          >
            {layout.filter(l => l && l.i).map(l => {
              const instance = page?.instances?.find(i => i.instanceId === l.i)
              const widgetId = instance?.widgetId ?? l.i.replace(/-\d+$/, '')
              const reg = WIDGET_REGISTRY[widgetId]
              if (!reg) return null
              const colorGroup = instance?.colorGroup ?? null
              return (
                <div key={l.i}>
                  <WidgetErrorBoundary>
                    <WidgetPanel id={l.i} title={reg.title} colorGroup={colorGroup}
                      onColorChange={c => store.setWidgetColor(l.i, c)}
                      onFullscreen={() => setFullscreen(l.i)}
                      onClose={() => store.removeWidgetInstance(l.i)}
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
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: 14, padding: 40 }}>
            <div style={{ fontSize: 48, opacity: 0.2 }}>📊</div>
            <div style={{ fontSize: 14, color: 'var(--t-text3)', fontWeight: 700, fontFamily: mono }}>This page is empty</div>
            <div style={{ fontSize: 11, color: 'var(--t-text3)', textAlign: 'center', maxWidth: 280 }}>Open the widget drawer to add widgets. You can add the same widget multiple times.</div>
            <button onClick={() => setShowWidgets(true)}
              style={{ marginTop: 4, padding: '10px 24px', fontSize: 12, fontWeight: 700, borderRadius: 8, border: '1px solid var(--t-accent)', cursor: 'pointer', background: 'rgba(var(--t-accent-rgb),0.1)', color: 'var(--t-accent)', fontFamily: mono }}>
              ⊞ Open Widget Drawer
            </button>
          </div>
        )}
      </div>

      <WidgetDrawer open={showWidgets} onClose={() => setShowWidgets(false)} onAdd={handleAddWidget} />
      <PageDrawer open={showPages} onClose={() => setShowPages(false)}
        pages={pages} activePageId={template?.activePageId}
        onSelect={id => store.setActivePage(id)}
        onAdd={() => store.addPage(`Page ${pages.length + 1}`)}
        onRename={(id, name) => store.renamePage(id, name)}
        onDelete={id => store.deletePage(id)}
        onIconChange={(id, icon) => store.setPageIcon(id, icon)}
      />

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
                <span style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)', fontFamily: mono, textTransform: 'uppercase', letterSpacing: '0.06em' }}>{reg.title} · ESC</span>
                <button onClick={() => setFullscreen(null)} style={{ padding: '4px 8px', background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--t-text3)', fontSize: 14 }}>✕</button>
              </div>
              <div style={{ flex: 1, overflow: 'auto', padding: 4 }}>
                <reg.component {...sharedProps} colorGroup={colorGroup} />
              </div>
            </div>
          </div>
        )
      })()}

      <TemplateManager open={showTemplates} onClose={() => setShowTemplates(false)} />
    </div>
  )
}
