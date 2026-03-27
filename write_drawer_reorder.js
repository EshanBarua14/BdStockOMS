const fs = require("fs");
const f = "BdStockOMS.Client/src/pages/DashboardPage.tsx";
let c = fs.readFileSync(f, "utf8");

// Replace the WidgetDrawer function with an enhanced version that has Add + Reorder tabs
const oldDrawer = `// ── Widget Drawer ───────────────────────────────────────────────────────────
function WidgetDrawer({ open, onClose, onAdd, onDragStart }) {
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
                    onDragStart={e => { e.dataTransfer.setData('widgetId', id); onDragStart?.(id); setTimeout(onClose, 50) }}
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
}`;

const newDrawer = `// ── Widget Drawer ───────────────────────────────────────────────────────────
function WidgetDrawer({ open, onClose, onAdd, onDragStart, layout, instances, onRemove, onReorder }) {
  const [search, setSearch] = useState('')
  const [tab, setTab] = useState('add')
  const [dragIdx, setDragIdx] = useState(null)
  const [dragOverIdx, setDragOverIdx] = useState(null)
  const categories = [...new Set(Object.values(WIDGET_REGISTRY).map(r => r.category).filter(Boolean))]
  const filtered = Object.entries(WIDGET_REGISTRY).filter(([id, reg]) =>
    !search || reg.title.toLowerCase().includes(search.toLowerCase())
  )
  const byCategory = categories
    .map(cat => ({ cat, items: filtered.filter(([, r]) => r.category === cat) }))
    .filter(g => g.items.length > 0)

  // Reorder drag handlers
  const handleDragStartReorder = (idx) => setDragIdx(idx)
  const handleDragOverReorder  = (e, idx) => { e.preventDefault(); setDragOverIdx(idx) }
  const handleDropReorder      = (idx) => {
    if (dragIdx === null || dragIdx === idx) { setDragIdx(null); setDragOverIdx(null); return }
    const newOrder = [...(instances || [])]
    const [moved]  = newOrder.splice(dragIdx, 1)
    newOrder.splice(idx, 0, moved)
    onReorder?.(newOrder.map(i => i.instanceId))
    setDragIdx(null); setDragOverIdx(null)
  }

  if (!open) return null
  return (
    <>
      <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 48 }} />
      <div style={{ position: 'fixed', top: 0, right: 0, bottom: 0, zIndex: 49, width: 260, background: 'var(--t-elevated)', borderLeft: '1px solid var(--t-border)', display: 'flex', flexDirection: 'column', boxShadow: '-16px 0 48px rgba(0,0,0,0.5)' }}>

        {/* Header */}
        <div style={{ padding: '14px 14px 0', borderBottom: '1px solid var(--t-border)', flexShrink: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
            <span style={{ fontSize: 11, fontWeight: 800, color: 'var(--t-text1)', fontFamily: mono }}>⊞ WIDGETS</span>
            <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 16 }}>✕</button>
          </div>

          {/* Tabs */}
          <div style={{ display: 'flex', gap: 2, marginBottom: 0 }}>
            {[['add','Add'],['manage','Manage']].map(([id, label]) => (
              <button key={id} onClick={() => setTab(id)}
                style={{ flex: 1, padding: '6px 0', fontSize: 10, fontWeight: 700, fontFamily: mono,
                  background: 'none', border: 'none', cursor: 'pointer',
                  borderBottom: tab === id ? '2px solid var(--t-accent)' : '2px solid transparent',
                  color: tab === id ? 'var(--t-accent)' : 'var(--t-text3)', marginBottom: -1 }}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {/* Add Tab */}
        {tab === 'add' && (
          <>
            <div style={{ padding: '10px 14px 8px', borderBottom: '1px solid var(--t-border)', flexShrink: 0 }}>
              <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search widgets…"
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
                      onDragStart={e => { e.dataTransfer.setData('widgetId', id); onDragStart?.(id); setTimeout(onClose, 50) }}
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
          </>
        )}

        {/* Manage Tab — drag to reorder + remove */}
        {tab === 'manage' && (
          <>
            <div style={{ padding: '10px 14px 8px', borderBottom: '1px solid var(--t-border)', flexShrink: 0 }}>
              <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>
                ⠿ Drag to reorder · ✕ to remove · {(instances||[]).length} active widgets
              </div>
            </div>
            <div style={{ flex: 1, overflowY: 'auto', padding: '8px 10px' }}>
              {(instances || []).map((inst, idx) => {
                const reg = WIDGET_REGISTRY[inst.widgetId]
                const isDragging  = dragIdx === idx
                const isDragOver  = dragOverIdx === idx
                return (
                  <div key={inst.instanceId}
                    draggable
                    onDragStart={() => handleDragStartReorder(idx)}
                    onDragOver={e => handleDragOverReorder(e, idx)}
                    onDrop={() => handleDropReorder(idx)}
                    onDragEnd={() => { setDragIdx(null); setDragOverIdx(null) }}
                    style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '7px 8px',
                      borderRadius: 7, marginBottom: 2, cursor: 'grab',
                      background: isDragOver ? 'var(--t-hover)' : isDragging ? 'rgba(255,255,255,0.04)' : 'transparent',
                      border: isDragOver ? '1px solid var(--t-accent)' : '1px solid transparent',
                      opacity: isDragging ? 0.5 : 1, transition: 'all 0.1s' }}>
                    <span style={{ fontSize: 12, color: 'var(--t-text3)', cursor: 'grab', flexShrink: 0 }}>⠿</span>
                    <span style={{ fontSize: 14, flexShrink: 0 }}>{reg?.icon ?? '□'}</span>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--t-text1)', fontFamily: mono, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {reg?.title ?? inst.widgetId}
                      </div>
                      <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>{inst.instanceId}</div>
                    </div>
                    <button onClick={() => onRemove?.(inst.instanceId)}
                      style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--t-text3)', fontSize: 12, padding: '2px 4px', borderRadius: 3, flexShrink: 0 }}
                      onMouseEnter={e => e.currentTarget.style.color = 'var(--t-sell)'}
                      onMouseLeave={e => e.currentTarget.style.color = 'var(--t-text3)'}>
                      ✕
                    </button>
                  </div>
                )
              })}
              {(instances||[]).length === 0 && (
                <div style={{ fontSize: 11, color: 'var(--t-text3)', textAlign: 'center', padding: 24, fontFamily: mono }}>
                  No widgets on this page.<br/>Switch to Add tab to add some.
                </div>
              )}
            </div>
          </>
        )}
      </div>
    </>
  )
}`;

c = c.replace(oldDrawer, newDrawer);

// Now wire the new props into the WidgetDrawer usage
c = c.replace(
  "<WidgetDrawer open={showWidgets} onClose={() => setShowWidgets(false)} onAdd={handleAddWidget} />",
  "<WidgetDrawer open={showWidgets} onClose={() => setShowWidgets(false)} onAdd={handleAddWidget} layout={store.getActivePage()?.layout} instances={store.getActivePage()?.instances} onRemove={id => store.removeWidgetInstance(id)} onReorder={ids => { const page = store.getActivePage(); if (!page) return; const newInst = ids.map(id => page.instances.find(i => i.instanceId === id)).filter(Boolean); store.updateLayout(page.layout); }} />"
);

fs.writeFileSync(f, c);
console.log("WidgetDrawer drag-to-reorder added");
