// @ts-nocheck
// CommandPalette.tsx - Day 77
// Ctrl+K global command palette with navigation, stock search, actions

import { useState, useEffect, useRef, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { searchStocks } from '@/api/client'

const mono = "'JetBrains Mono', monospace"

// ── All navigable commands ────────────────────────────────────────────────────
const NAV_COMMANDS = [
  { id: 'dashboard',      label: 'Go to Dashboard',         category: 'Navigation', path: '/dashboard',          icon: '⬛' },
  { id: 'orders',         label: 'Go to Orders',             category: 'Navigation', path: '/orders',             icon: '📋' },
  { id: 'portfolio',      label: 'Go to Portfolio',          category: 'Navigation', path: '/portfolio',          icon: '💼' },
  { id: 'market',         label: 'Go to Market',             category: 'Navigation', path: '/market',             icon: '📈' },
  { id: 'trade-monitor',  label: 'Go to Trade Monitor',      category: 'Navigation', path: '/trade-monitor',      icon: '🖥' },
  { id: 'rms',            label: 'Go to RMS',                category: 'Navigation', path: '/rms',                icon: '🛡' },
  { id: 'reports',        label: 'Go to Reports',            category: 'Navigation', path: '/reports',            icon: '📊' },
  { id: 'accounts',       label: 'Go to Accounts',           category: 'Navigation', path: '/accounts',           icon: '💰' },
  { id: 'ipo',            label: 'Go to IPO',                category: 'Navigation', path: '/ipo',                icon: '🏦' },
  { id: 'tbond',          label: 'Go to T-Bond',             category: 'Navigation', path: '/tbond',              icon: '📜' },
  { id: 'admin-brokers',  label: 'Go to Broker Management',  category: 'Admin',      path: '/admin/brokers',      icon: '🏢' },
  { id: 'admin-branches', label: 'Go to Branch Management',  category: 'Admin',      path: '/admin/branches',     icon: '🏪' },
  { id: 'admin-bo',       label: 'Go to BO Accounts',        category: 'Admin',      path: '/admin/bo-accounts',  icon: '👤' },
  { id: 'admin-bos',      label: 'Go to BOS Reconciliation', category: 'Admin',      path: '/admin/bos',          icon: '🔄' },
  { id: 'admin-fix',      label: 'Go to FIX Engine',         category: 'Admin',      path: '/admin/fix',          icon: '⚡' },
  { id: 'settings',       label: 'Go to Settings',           category: 'Admin',      path: '/settings/general',   icon: '⚙' },
  { id: 'settings-market',label: 'Settings: Market',         category: 'Admin',      path: '/settings/market',    icon: '⚙' },
  { id: 'settings-fix',   label: 'Settings: FIX Engine',     category: 'Admin',      path: '/settings/fix-engine',icon: '⚙' },
  { id: 'settings-roles', label: 'Settings: Roles',          category: 'Admin',      path: '/settings/roles',     icon: '⚙' },
]

function highlight(text, query) {
  if (!query) return <span>{text}</span>
  const idx = text.toLowerCase().indexOf(query.toLowerCase())
  if (idx === -1) return <span>{text}</span>
  return (
    <span>
      {text.slice(0, idx)}
      <mark style={{background:'rgba(0,212,170,0.25)',color:'var(--t-accent)',borderRadius:2,padding:'0 1px'}}>{text.slice(idx, idx+query.length)}</mark>
      {text.slice(idx+query.length)}
    </span>
  )
}

export function CommandPalette({ open, onClose }) {
  const [query, setQuery]         = useState('')
  const [selected, setSelected]   = useState(0)
  const [stocks, setStocks]       = useState([])
  const [searching, setSearching] = useState(false)
  const inputRef  = useRef(null)
  const listRef   = useRef(null)
  const navigate  = useNavigate()
  const debounce  = useRef(null)

  // Focus input when opened
  useEffect(() => {
    if (open) {
      setQuery(''); setSelected(0); setStocks([])
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [open])

  // Stock search debounce
  useEffect(() => {
    clearTimeout(debounce.current)
    if (query.length < 2) { setStocks([]); return }
    setSearching(true)
    debounce.current = setTimeout(async () => {
      try {
        const res = await searchStocks(query)
        const arr = Array.isArray(res) ? res : res?.data ?? []
        setStocks(arr.slice(0, 6))
      } catch { setStocks([]) }
      finally { setSearching(false) }
    }, 250)
    return () => clearTimeout(debounce.current)
  }, [query])

  // Filter nav commands
  const navResults = query.length === 0
    ? NAV_COMMANDS.slice(0, 8)
    : NAV_COMMANDS.filter(c =>
        c.label.toLowerCase().includes(query.toLowerCase()) ||
        c.category.toLowerCase().includes(query.toLowerCase())
      )

  // Combined results: stocks first if query exists, then nav
  const stockItems = stocks.map(s => ({
    id: 'stock-' + (s.tradingCode ?? s.symbol),
    label: (s.tradingCode ?? s.symbol) + ' — ' + (s.companyName ?? s.name ?? ''),
    category: 'Stock',
    path: null,
    stock: s,
    icon: '📉',
  }))

  const allItems = query.length >= 2
    ? [...stockItems, ...navResults]
    : navResults

  const total = allItems.length

  // Keyboard navigation
  useEffect(() => {
    if (!open) return
    const handler = (e) => {
      if (e.key === 'ArrowDown') { e.preventDefault(); setSelected(s => Math.min(s+1, total-1)) }
      if (e.key === 'ArrowUp')   { e.preventDefault(); setSelected(s => Math.max(s-1, 0)) }
      if (e.key === 'Enter')     { e.preventDefault(); execute(allItems[selected]) }
      if (e.key === 'Escape')    { onClose() }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, selected, allItems, total])

  // Scroll selected item into view
  useEffect(() => {
    const el = listRef.current?.children[selected]
    el?.scrollIntoView({ block: 'nearest' })
  }, [selected])

  const execute = useCallback((item) => {
    if (!item) return
    if (item.path) navigate(item.path)
    onClose()
  }, [navigate, onClose])

  if (!open) return null

  const categories = [...new Set(allItems.map(i => i.category))]

  return (
    <div
      onClick={onClose}
      style={{
        position:'fixed',inset:0,zIndex:9999,
        background:'rgba(0,0,0,0.6)',backdropFilter:'blur(4px)',
        display:'flex',alignItems:'flex-start',justifyContent:'center',
        paddingTop:'15vh',
      }}
    >
      <div
        onClick={e=>e.stopPropagation()}
        style={{
          width:'100%',maxWidth:580,borderRadius:12,overflow:'hidden',
          border:'1px solid var(--t-border)',background:'var(--t-panel)',
          boxShadow:'0 25px 60px rgba(0,0,0,0.5)',
        }}
      >
        {/* Search input */}
        <div style={{display:'flex',alignItems:'center',gap:10,padding:'12px 16px',borderBottom:'1px solid var(--t-border)'}}>
          <span style={{fontSize:16,color:'var(--t-text3)'}}>⌕</span>
          <input
            ref={inputRef}
            value={query}
            onChange={e=>{setQuery(e.target.value);setSelected(0)}}
            placeholder="Search pages, stocks, commands..."
            style={{
              flex:1,background:'none',border:'none',outline:'none',
              fontSize:15,color:'var(--t-text1)',fontFamily:mono,
            }}
          />
          {searching && <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>searching...</span>}
          <kbd style={{fontSize:10,color:'var(--t-text3)',background:'var(--t-hover)',border:'1px solid var(--t-border)',borderRadius:4,padding:'2px 6px',fontFamily:mono}}>ESC</kbd>
        </div>

        {/* Results */}
        <div ref={listRef} style={{maxHeight:380,overflowY:'auto'}}>
          {total === 0 ? (
            <div style={{padding:'24px 16px',textAlign:'center',color:'var(--t-text3)',fontSize:13,fontFamily:mono}}>
              {query.length >= 2 ? 'No results found' : 'Type to search...'}
            </div>
          ) : (
            categories.map(cat => {
              const items = allItems.filter(i => i.category === cat)
              const startIdx = allItems.indexOf(items[0])
              return (
                <div key={cat}>
                  <div style={{padding:'6px 16px 2px',fontSize:9,color:'var(--t-text3)',fontFamily:mono,letterSpacing:'0.1em',textTransform:'uppercase'}}>{cat}</div>
                  {items.map((item, localIdx) => {
                    const globalIdx = startIdx + localIdx
                    const isSelected = globalIdx === selected
                    return (
                      <div
                        key={item.id}
                        onClick={()=>execute(item)}
                        onMouseEnter={()=>setSelected(globalIdx)}
                        style={{
                          display:'flex',alignItems:'center',gap:10,
                          padding:'8px 16px',cursor:'pointer',
                          background:isSelected?'rgba(0,212,170,0.08)':'transparent',
                          borderLeft:isSelected?'2px solid var(--t-accent)':'2px solid transparent',
                          transition:'background 0.08s',
                        }}
                      >
                        <span style={{fontSize:14,width:20,textAlign:'center',flexShrink:0}}>{item.icon}</span>
                        <div style={{flex:1,minWidth:0}}>
                          <div style={{fontSize:13,color:'var(--t-text1)',fontFamily:mono,overflow:'hidden',textOverflow:'ellipsis',whiteSpace:'nowrap'}}>
                            {highlight(item.label, query)}
                          </div>
                          {item.stock && (
                            <div style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>
                              {item.stock.lastTradePrice ? 'BDT ' + item.stock.lastTradePrice.toFixed(2) : ''}
                              {item.stock.changePercent ? '  ' + (item.stock.changePercent > 0 ? '+' : '') + item.stock.changePercent.toFixed(2) + '%' : ''}
                            </div>
                          )}
                        </div>
                        {isSelected && <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>↵</span>}
                      </div>
                    )
                  })}
                </div>
              )
            })
          )}
        </div>

        {/* Footer */}
        <div style={{padding:'8px 16px',borderTop:'1px solid var(--t-border)',display:'flex',gap:16,alignItems:'center'}}>
          {[['↑↓','navigate'],['↵','select'],['ESC','close']].map(([k,v])=>(
            <span key={k} style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono,display:'flex',alignItems:'center',gap:4}}>
              <kbd style={{background:'var(--t-hover)',border:'1px solid var(--t-border)',borderRadius:3,padding:'1px 5px',fontSize:10}}>{k}</kbd>
              {v}
            </span>
          ))}
          <span style={{marginLeft:'auto',fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>BdStockOMS</span>
        </div>
      </div>
    </div>
  )
}
