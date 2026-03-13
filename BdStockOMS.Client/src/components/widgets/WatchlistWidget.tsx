// @ts-nocheck
// src/components/widgets/WatchlistWidget.tsx
// Day 54 — Enhanced Watchlist: custom columns, right-click context menu,
// advanced filter, sort asc/desc, rename/delete lists, reorder, BuySellConsole integration

import { useState, useEffect, useRef, useCallback, useMemo } from "react"
import { watchlistApi } from "@/api/watchlist"
import { marketApi } from "@/api/market"
import { useMarketData } from "@/hooks/useMarketData"
import { BuySellConsoleEvents } from "@/components/trading/BuySellConsole"

// ─── All available columns ────────────────────────────────────────────────────
const ALL_COLUMNS = [
  { key: "tradingCode",    label: "Code",       w: 72  },
  { key: "exchange",       label: "Exch",       w: 44  },
  { key: "category",       label: "Cat",        w: 36  },
  { key: "lastTradePrice", label: "LTP",        w: 72  },
  { key: "change",         label: "Chg",        w: 64  },
  { key: "changePercent",  label: "Chg%",       w: 64  },
  { key: "volume",         label: "Vol",        w: 72  },
  { key: "value",          label: "Val(mn)",    w: 72  },
  { key: "tradeCount",     label: "Trades",     w: 60  },
  { key: "highPrice",      label: "High",       w: 64  },
  { key: "lowPrice",       label: "Low",        w: 64  },
  { key: "closePrice",     label: "Close",      w: 64  },
  { key: "openPrice",      label: "Open",       w: 64  },
  { key: "ycp",            label: "YCP",        w: 64  },
  { key: "w52High",        label: "52W H",      w: 64  },
  { key: "w52Low",         label: "52W L",      w: 64  },
  { key: "buyPressure",    label: "BuyPres",    w: 68  },
  { key: "sellPressure",   label: "SellPres",   w: 68  },
  { key: "boardLotSize",   label: "Lot",        w: 44  },
  { key: "companyName",    label: "Company",    w: 140 },
  { key: "sector",         label: "Sector",     w: 100 },
  { key: "isin",           label: "ISIN",       w: 110 },
]

const DEFAULT_COLS = ["tradingCode","exchange","category","lastTradePrice","change","changePercent","volume","highPrice","lowPrice"]

const STORAGE_KEY = "bd_oms_watchlist_cols_v1"

function loadCols(): string[] {
  try { const s = localStorage.getItem(STORAGE_KEY); return s ? JSON.parse(s) : DEFAULT_COLS } catch { return DEFAULT_COLS }
}
function saveCols(cols: string[]) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(cols)) } catch {}
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
const mono = "'JetBrains Mono', monospace"

function fmtPrice(v: number) { return v > 0 ? `৳${v.toFixed(2)}` : "—" }
function fmtVol(v: number)   { return v >= 1e6 ? `${(v/1e6).toFixed(1)}M` : v >= 1e3 ? `${(v/1e3).toFixed(0)}K` : String(v || "—") }
function fmtVal(v: number)   { return v > 0 ? v.toFixed(2) : "—" }

function catColor(cat: string) {
  const map: Record<string,string> = { A:"#00e676", B:"#ffd740", G:"#60a5fa", N:"#a78bfa", Z:"#ff1744", Spot:"#ff9100" }
  return map[cat] ?? "var(--t-text3)"
}

function getCellValue(stock: any, key: string): any {
  switch(key) {
    case "lastTradePrice": return stock.lastTradePrice ?? stock.lastPrice ?? 0
    case "change":         return stock.change ?? 0
    case "changePercent":  return stock.changePercent ?? 0
    case "volume":         return stock.volume ?? 0
    case "value":          return stock.valueInMillionTaka ?? stock.value ?? 0
    case "tradeCount":     return stock.tradeCount ?? stock.trades ?? 0
    case "highPrice":      return stock.highPrice ?? stock.high ?? 0
    case "lowPrice":       return stock.lowPrice ?? stock.low ?? 0
    case "closePrice":     return stock.closePrice ?? stock.close ?? 0
    case "openPrice":      return stock.openPrice ?? stock.open ?? 0
    case "ycp":            return stock.ycp ?? stock.yesterdayClosePrice ?? 0
    case "w52High":        return stock.w52High ?? stock.yearHigh ?? 0
    case "w52Low":         return stock.w52Low ?? stock.yearLow ?? 0
    case "buyPressure":    return stock.buyPressure ?? null
    case "sellPressure":   return stock.sellPressure ?? null
    case "boardLotSize":   return stock.boardLotSize ?? 1
    case "category":       return stock.category ?? "A"
    default:               return stock[key] ?? ""
  }
}

function renderCell(key: string, val: any, up: boolean) {
  const style: any = { fontSize: 10, fontFamily: mono, color: "var(--t-text2)", textAlign: "right" }
  if (key === "tradingCode") return <span style={{ ...style, color: "var(--t-accent)", fontWeight: 700, textAlign: "left" }}>{val}</span>
  if (key === "companyName") return <span style={{ ...style, color: "var(--t-text2)", fontSize: 9, textAlign: "left" }}>{val}</span>
  if (key === "exchange")    return <span style={{ ...style, color: val === "DSE" ? "#60a5fa" : "#a78bfa", fontSize: 9 }}>{val}</span>
  if (key === "category")    return <span style={{ ...style, color: catColor(val), fontWeight: 700, fontSize: 9 }}>{val}</span>
  if (key === "lastTradePrice") return <span style={{ ...style, color: "var(--t-text1)", fontWeight: 700 }}>{fmtPrice(val)}</span>
  if (key === "change")      return <span style={{ ...style, color: val >= 0 ? "var(--t-buy)" : "var(--t-sell)" }}>{val >= 0 ? "+" : ""}{val?.toFixed(2)}</span>
  if (key === "changePercent") return <span style={{ ...style, color: val >= 0 ? "var(--t-buy)" : "var(--t-sell)", fontWeight: 700 }}>{val >= 0 ? "▲" : "▼"}{Math.abs(val)?.toFixed(2)}%</span>
  if (key === "volume")      return <span style={style}>{fmtVol(val)}</span>
  if (key === "value")       return <span style={style}>{fmtVal(val)}</span>
  if (key === "buyPressure" || key === "sellPressure") {
    if (val === null) return <span style={{ ...style, color: "var(--t-text3)" }}>—</span>
    return (
      <div style={{ display: "flex", alignItems: "center", gap: 3, justifyContent: "flex-end" }}>
        <div style={{ width: 32, height: 4, background: "var(--t-border)", borderRadius: 2, overflow: "hidden" }}>
          <div style={{ width: `${Math.min(val,100)}%`, height: "100%", background: key === "buyPressure" ? "var(--t-buy)" : "var(--t-sell)", borderRadius: 2 }} />
        </div>
        <span style={style}>{val?.toFixed(0)}%</span>
      </div>
    )
  }
  if (["highPrice","lowPrice","closePrice","openPrice","ycp","w52High","w52Low"].includes(key))
    return <span style={style}>{fmtPrice(val)}</span>
  return <span style={style}>{val || "—"}</span>
}

// ─── Column Picker Modal ──────────────────────────────────────────────────────
function ColumnPicker({ active, onClose, onChange }: { active: string[], onClose: () => void, onChange: (c: string[]) => void }) {
  const [selected, setSelected] = useState<string[]>(active)
  const [search, setSearch] = useState("")
  const [dragIdx, setDragIdx] = useState<number | null>(null)
  const [dragOver, setDragOver] = useState<number | null>(null)

  const filtered = ALL_COLUMNS.filter(c => c.label.toLowerCase().includes(search.toLowerCase()) || c.key.toLowerCase().includes(search.toLowerCase()))

  const toggle = (key: string) => {
    setSelected(s => s.includes(key) ? s.filter(k => k !== key) : [...s, key])
  }

  const handleDragStart = (i: number) => setDragIdx(i)
  const handleDragOver  = (e: any, i: number) => { e.preventDefault(); setDragOver(i) }
  const handleDrop      = (i: number) => {
    if (dragIdx === null || dragIdx === i) return
    const next = [...selected]
    const [moved] = next.splice(dragIdx, 1)
    next.splice(i, 0, moved)
    setSelected(next)
    setDragIdx(null); setDragOver(null)
  }

  return (
    <div style={{ position: "fixed", inset: 0, zIndex: 9995, background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center" }} onClick={onClose}>
      <div style={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", borderRadius: 12, width: 480, maxWidth: "95vw", maxHeight: "80vh", display: "flex", flexDirection: "column", overflow: "hidden" }} onClick={e => e.stopPropagation()}>
        <div style={{ padding: "12px 16px", borderBottom: "1px solid var(--t-border)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <span style={{ fontWeight: 700, color: "var(--t-text1)", fontSize: 13 }}>Customize Columns</span>
          <button onClick={onClose} style={{ background: "none", border: "none", color: "var(--t-text3)", cursor: "pointer", fontSize: 16 }}>✕</button>
        </div>
        <div style={{ padding: "8px 16px", borderBottom: "1px solid var(--t-border)" }}>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search columns…"
            style={{ width: "100%", boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 6, padding: "6px 10px", color: "var(--t-text1)", fontSize: 12, outline: "none" }} />
        </div>
        <div style={{ display: "flex", flex: 1, overflow: "hidden" }}>
          {/* All columns */}
          <div style={{ flex: 1, overflowY: "auto", padding: "8px 0", borderRight: "1px solid var(--t-border)" }}>
            <div style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", padding: "0 12px 6px", fontFamily: mono }}>AVAILABLE</div>
            {filtered.map(c => (
              <div key={c.key} onClick={() => toggle(c.key)} style={{
                padding: "6px 12px", cursor: "pointer", display: "flex", alignItems: "center", gap: 8,
                background: selected.includes(c.key) ? "var(--t-hover)" : "transparent",
                fontSize: 11, color: selected.includes(c.key) ? "var(--t-accent)" : "var(--t-text2)",
              }}>
                <span style={{ fontSize: 10 }}>{selected.includes(c.key) ? "☑" : "☐"}</span>
                {c.label}
                <span style={{ fontSize: 9, color: "var(--t-text3)", marginLeft: "auto" }}>{c.key}</span>
              </div>
            ))}
          </div>
          {/* Active columns — drag to reorder */}
          <div style={{ flex: 1, overflowY: "auto", padding: "8px 0" }}>
            <div style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", padding: "0 12px 6px", fontFamily: mono }}>ACTIVE (drag to reorder)</div>
            {selected.map((key, i) => {
              const col = ALL_COLUMNS.find(c => c.key === key)
              return (
                <div key={key}
                  draggable
                  onDragStart={() => handleDragStart(i)}
                  onDragOver={e => handleDragOver(e, i)}
                  onDrop={() => handleDrop(i)}
                  style={{
                    padding: "6px 12px", cursor: "grab", display: "flex", alignItems: "center", gap: 8,
                    fontSize: 11, color: "var(--t-text1)",
                    background: dragOver === i ? "var(--t-hover)" : "transparent",
                    borderTop: dragOver === i ? "1px solid var(--t-accent)" : "1px solid transparent",
                  }}>
                  <span style={{ color: "var(--t-text3)", fontSize: 10 }}>⠿</span>
                  {col?.label ?? key}
                  <button onClick={e => { e.stopPropagation(); toggle(key) }} style={{ marginLeft: "auto", background: "none", border: "none", color: "var(--t-sell)", cursor: "pointer", fontSize: 12 }}>×</button>
                </div>
              )
            })}
          </div>
        </div>
        <div style={{ padding: "10px 16px", borderTop: "1px solid var(--t-border)", display: "flex", gap: 8, justifyContent: "flex-end" }}>
          <button onClick={() => { setSelected(DEFAULT_COLS) }} style={{ padding: "6px 14px", fontSize: 11, borderRadius: 6, border: "1px solid var(--t-border)", background: "transparent", color: "var(--t-text3)", cursor: "pointer" }}>Reset</button>
          <button onClick={() => { onChange(selected); onClose() }} style={{ padding: "6px 14px", fontSize: 11, borderRadius: 6, border: "none", background: "var(--t-accent)", color: "#000", fontWeight: 700, cursor: "pointer" }}>Apply</button>
        </div>
      </div>
    </div>
  )
}

// ─── Filter Panel ─────────────────────────────────────────────────────────────
function FilterPanel({ filter, setFilter, onClose }: any) {
  return (
    <div style={{ position: "absolute", top: "100%", right: 0, zIndex: 200, background: "var(--t-elevated)", border: "1px solid var(--t-border)", borderRadius: 10, padding: 14, width: 260, boxShadow: "0 8px 24px rgba(0,0,0,0.5)" }}>
      <div style={{ fontSize: 10, fontWeight: 700, color: "var(--t-text3)", marginBottom: 10, fontFamily: mono }}>ADVANCED FILTER</div>
      {[
        { key: "tradedOnly",  label: "Traded Only",  type: "checkbox" },
        { key: "spotOnly",    label: "Spot Only",    type: "checkbox" },
      ].map(f => (
        <label key={f.key} style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 8, cursor: "pointer", fontSize: 11, color: "var(--t-text2)" }}>
          <input type="checkbox" checked={filter[f.key] ?? false} onChange={e => setFilter((p: any) => ({ ...p, [f.key]: e.target.checked }))}
            style={{ accentColor: "var(--t-accent)" }} />
          {f.label}
        </label>
      ))}
      {[
        { key: "exchange", label: "Exchange", options: ["", "DSE", "CSE"] },
        { key: "category", label: "Category", options: ["", "A", "B", "G", "N", "Z", "Spot"] },
      ].map(f => (
        <div key={f.key} style={{ marginBottom: 8 }}>
          <div style={{ fontSize: 9, color: "var(--t-text3)", marginBottom: 3, fontFamily: mono }}>{f.label.toUpperCase()}</div>
          <select value={filter[f.key] ?? ""} onChange={e => setFilter((p: any) => ({ ...p, [f.key]: e.target.value }))}
            style={{ width: "100%", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "5px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none" }}>
            {f.options.map(o => <option key={o} value={o}>{o || `All ${f.label}`}</option>)}
          </select>
        </div>
      ))}
      <div style={{ marginBottom: 8 }}>
        <div style={{ fontSize: 9, color: "var(--t-text3)", marginBottom: 3, fontFamily: mono }}>SYMBOL SEARCH</div>
        <input value={filter.symbolQ ?? ""} onChange={e => setFilter((p: any) => ({ ...p, symbolQ: e.target.value.toUpperCase() }))}
          placeholder="e.g. GP" style={{ width: "100%", boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "5px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none" }} />
      </div>
      <div style={{ display: "flex", gap: 6, marginTop: 10 }}>
        <button onClick={() => setFilter({})} style={{ flex: 1, padding: "5px", fontSize: 10, borderRadius: 5, border: "1px solid var(--t-border)", background: "transparent", color: "var(--t-text3)", cursor: "pointer" }}>Clear</button>
        <button onClick={onClose} style={{ flex: 1, padding: "5px", fontSize: 10, borderRadius: 5, border: "none", background: "var(--t-accent)", color: "#000", fontWeight: 700, cursor: "pointer" }}>Done</button>
      </div>
    </div>
  )
}

// ─── Context Menu ─────────────────────────────────────────────────────────────
function ContextMenu({ x, y, stock, lists, activeListId, onClose, onAddToList, onRemove }: any) {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) onClose() }
    document.addEventListener("mousedown", h)
    return () => document.removeEventListener("mousedown", h)
  }, [])

  const code = stock?.tradingCode

  const Item = ({ icon, label, color, onClick, disabled }: any) => (
    <button onClick={() => { if (!disabled) { onClick(); onClose() } }} style={{
      display: "flex", alignItems: "center", gap: 8, width: "100%",
      padding: "7px 12px", background: "none", border: "none", cursor: disabled ? "default" : "pointer",
      color: disabled ? "var(--t-text3)" : (color ?? "var(--t-text1)"), fontSize: 11, textAlign: "left",
      fontFamily: mono, transition: "background 0.08s",
    }}
      onMouseEnter={e => { if (!disabled) e.currentTarget.style.background = "var(--t-hover)" }}
      onMouseLeave={e => e.currentTarget.style.background = "none"}
    >
      <span style={{ width: 14, textAlign: "center", fontSize: 12 }}>{icon}</span>
      {label}
    </button>
  )

  const Div = () => <div style={{ height: 1, background: "var(--t-border)", margin: "3px 0" }} />

  return (
    <div ref={ref} style={{
      position: "fixed", left: x, top: y, zIndex: 9999,
      background: "var(--t-elevated)", border: "1px solid var(--t-border)",
      borderRadius: 10, minWidth: 210, overflow: "hidden",
      boxShadow: "0 8px 32px rgba(0,0,0,0.6)",
      animation: "oms-fade-in 0.1s ease",
    }}>
      <div style={{ padding: "7px 12px 5px", fontSize: 10, fontWeight: 700, color: "var(--t-accent)", fontFamily: mono, borderBottom: "1px solid var(--t-border)" }}>
        {code}
      </div>
      <Item icon="↑" label="Buy  (F1)"  color="var(--t-buy)"  onClick={() => BuySellConsoleEvents.open("BUY",  code)} />
      <Item icon="↓" label="Sell (F2)"  color="var(--t-sell)" onClick={() => BuySellConsoleEvents.open("SELL", code)} />
      <Div />
      <Item icon="📋" label="Active Orders"         onClick={() => {}} />
      <Item icon="✅" label="Executed Orders"        onClick={() => {}} />
      <Item icon="📊" label="Market Depth"           onClick={() => {}} />
      <Item icon="⏱"  label="Time & Sales"           onClick={() => {}} />
      <Item icon="📈" label="Minute Chart"           onClick={() => {}} />
      <Item icon="🔬" label="Technical Analysis"     onClick={() => {}} />
      <Item icon="🗞"  label="News"                  onClick={() => {}} />
      <Item icon="🏢" label="Company Info"           onClick={() => {}} />
      <Div />
      {/* Add to watchlist submenu */}
      <div style={{ padding: "3px 0" }}>
        <div style={{ padding: "4px 12px", fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>ADD TO WATCHLIST</div>
        {lists.filter((l: any) => l.id !== activeListId).map((l: any) => (
          <Item key={l.id} icon="★" label={l.name} onClick={() => onAddToList(l.id, stock.stockId)} />
        ))}
        {lists.filter((l: any) => l.id !== activeListId).length === 0 && (
          <div style={{ padding: "4px 12px", fontSize: 10, color: "var(--t-text3)" }}>No other lists</div>
        )}
      </div>
      <Div />
      <Item icon="×" label="Remove from list" color="var(--t-sell)" onClick={() => onRemove(stock.stockId)} />
    </div>
  )
}

// ─── Main Widget ──────────────────────────────────────────────────────────────
export function WatchlistWidget() {
  const { ticksArray } = useMarketData()

  // Watchlist state
  const [lists, setLists]   = useState<any[]>([])
  const [active, setActive] = useState<number>(0)

  // Add stock search
  const [search, setSearch]     = useState("")
  const [results, setResults]   = useState<any[]>([])
  const [searchCursor, setSearchCursor] = useState(0)
  const searchRef = useRef<HTMLInputElement>(null)

  // Rename inline
  const [renaming, setRenaming]   = useState<number | null>(null)
  const [renameVal, setRenameVal] = useState("")

  // Create new list
  const [creating, setCreating]   = useState(false)
  const [newName, setNewName]     = useState("")

  // Column picker
  const [cols, setCols]           = useState<string[]>(loadCols)
  const [showColPicker, setShowColPicker] = useState(false)

  // Filter
  const [showFilter, setShowFilter] = useState(false)
  const [filter, setFilter]         = useState<any>({})

  // Sort
  const [sortKey, setSortKey]   = useState<string | null>(null)
  const [sortDir, setSortDir]   = useState<"asc" | "desc">("asc")

  // Context menu
  const [ctxMenu, setCtxMenu] = useState<{ x: number; y: number; stock: any } | null>(null)

  // Load
  const reload = useCallback(async () => {
    try {
      const d = await watchlistApi.getAll()
      setLists(d ?? [])
      setActive(a => a || d?.[0]?.id || 0)
    } catch {}
  }, [])

  useEffect(() => { reload() }, [reload])

  const activeList = useMemo(() => lists.find(l => l.id === active), [lists, active])

  // Merge live prices
  const mergedStocks = useMemo(() => {
    const base = activeList?.stocks ?? []
    return base.map((s: any) => {
      const live = ticksArray.find(t => t.tradingCode === s.tradingCode)
      return live ? { ...s, ...live, stockId: s.stockId, tradingCode: s.tradingCode } : s
    })
  }, [activeList, ticksArray])

  // Apply filter
  const filteredStocks = useMemo(() => {
    let stocks = mergedStocks
    if (filter.tradedOnly) stocks = stocks.filter((s: any) => (s.volume ?? 0) > 0)
    if (filter.spotOnly)   stocks = stocks.filter((s: any) => s.category === "Spot")
    if (filter.exchange)   stocks = stocks.filter((s: any) => s.exchange === filter.exchange)
    if (filter.category)   stocks = stocks.filter((s: any) => s.category === filter.category)
    if (filter.symbolQ)    stocks = stocks.filter((s: any) => s.tradingCode?.startsWith(filter.symbolQ))
    return stocks
  }, [mergedStocks, filter])

  // Apply sort
  const sortedStocks = useMemo(() => {
    if (!sortKey) return filteredStocks
    return [...filteredStocks].sort((a, b) => {
      const av = getCellValue(a, sortKey)
      const bv = getCellValue(b, sortKey)
      const cmp = typeof av === "number" ? av - bv : String(av).localeCompare(String(bv))
      return sortDir === "asc" ? cmp : -cmp
    })
  }, [filteredStocks, sortKey, sortDir])

  // Search
  const handleSearch = async (q: string) => {
    setSearch(q); setSearchCursor(0)
    if (q.length < 1) { setResults([]); return }
    try { const d: any = await marketApi.searchStocks(q); setResults((Array.isArray(d) ? d : d?.data ?? []).slice(0, 8)) } catch {}
  }

  const handleSearchKey = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown") { e.preventDefault(); setSearchCursor(c => Math.min(c+1, results.length-1)) }
    if (e.key === "ArrowUp")   { e.preventDefault(); setSearchCursor(c => Math.max(c-1, 0)) }
    if (e.key === "Enter")     { e.preventDefault(); if (results[searchCursor]) addStock(results[searchCursor].id) }
    if (e.key === "Escape")    setResults([])
  }

  const addStock = async (stockId: number) => {
    if (!active) return
    try { await watchlistApi.addStock(active, stockId); await reload() } catch {}
    setSearch(""); setResults([])
  }

  const removeStock = async (stockId: number) => {
    if (!active) return
    try { await watchlistApi.removeStock(active, stockId); await reload() } catch {}
  }

  const addToList = async (listId: number, stockId: number) => {
    try { await watchlistApi.addStock(listId, stockId); await reload() } catch {}
  }

  const createList = async () => {
    if (!newName.trim()) return
    try { await watchlistApi.create(newName.trim()); await reload(); setNewName(""); setCreating(false) } catch {}
  }

  const deleteList = async (id: number) => {
    try { await watchlistApi.remove(id); await reload() } catch {}
  }

  const startRename = (l: any) => { setRenaming(l.id); setRenameVal(l.name) }

  const submitRename = async () => {
    if (!renaming || !renameVal.trim()) { setRenaming(null); return }
    try { await watchlistApi.rename(renaming, renameVal.trim()); await reload() } catch {}
    setRenaming(null)
  }

  const handleSort = (key: string) => {
    if (sortKey === key) setSortDir(d => d === "asc" ? "desc" : "asc")
    else { setSortKey(key); setSortDir("asc") }
  }

  const handleColChange = (c: string[]) => { setCols(c); saveCols(c) }

  const handleContextMenu = (e: React.MouseEvent, stock: any) => {
    e.preventDefault()
    setCtxMenu({ x: e.clientX, y: e.clientY, stock })
  }

  const colDefs = useMemo(() =>
    cols.map(k => ALL_COLUMNS.find(c => c.key === k)).filter(Boolean)
  , [cols])

  const filterActive = Object.values(filter).some(Boolean)

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Toolbar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 4, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {/* Column picker */}
        <button onClick={() => setShowColPicker(true)} title="Columns" style={{ ...iconBtn }}>⊞</button>
        {/* Filter */}
        <div style={{ position: "relative" }}>
          <button onClick={() => setShowFilter(f => !f)} title="Filter" style={{ ...iconBtn, color: filterActive ? "var(--t-accent)" : undefined }}>▼</button>
          {showFilter && <FilterPanel filter={filter} setFilter={setFilter} onClose={() => setShowFilter(false)} />}
        </div>
        {/* Refresh */}
        <button onClick={reload} title="Refresh" style={{ ...iconBtn }}>↺</button>

        <div style={{ flex: 1 }} />

        {/* Stock count */}
        <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
          {sortedStocks.length} / {(activeList?.stocks ?? []).length}
        </span>
      </div>

      {/* ── Watchlist tabs ── */}
      <div style={{ display: "flex", alignItems: "center", borderBottom: "1px solid var(--t-border)", overflowX: "auto", flexShrink: 0, background: "var(--t-panel)" }}>
        {lists.map(l => (
          <div key={l.id} style={{ display: "flex", alignItems: "center", flexShrink: 0 }}>
            {renaming === l.id ? (
              <input autoFocus value={renameVal}
                onChange={e => setRenameVal(e.target.value)}
                onKeyDown={e => { if (e.key === "Enter") submitRename(); if (e.key === "Escape") setRenaming(null) }}
                onBlur={submitRename}
                style={{ width: 80, background: "var(--t-hover)", border: "1px solid var(--t-accent)", borderRadius: 4, padding: "3px 6px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono }} />
            ) : (
              <button
                onClick={() => setActive(l.id)}
                onDoubleClick={() => startRename(l)}
                style={{
                  padding: "6px 10px", background: "none", border: "none",
                  borderBottom: `2px solid ${active === l.id ? "var(--t-accent)" : "transparent"}`,
                  color: active === l.id ? "var(--t-accent)" : "var(--t-text3)",
                  fontSize: 10, cursor: "pointer", whiteSpace: "nowrap", fontFamily: mono,
                  display: "flex", alignItems: "center", gap: 4,
                }}
              >
                {l.isDefault && <span style={{ fontSize: 8 }}>★</span>}
                {l.name}
                {!l.isDefault && active === l.id && (
                  <span onClick={e => { e.stopPropagation(); deleteList(l.id) }}
                    style={{ fontSize: 10, color: "var(--t-sell)", marginLeft: 2, opacity: 0.6 }}>×</span>
                )}
              </button>
            )}
          </div>
        ))}
        {/* Create new */}
        {creating ? (
          <div style={{ display: "flex", alignItems: "center", gap: 4, padding: "3px 6px" }}>
            <input autoFocus value={newName} onChange={e => setNewName(e.target.value)}
              onKeyDown={e => { if (e.key === "Enter") createList(); if (e.key === "Escape") setCreating(false) }}
              placeholder="List name"
              style={{ width: 80, background: "var(--t-hover)", border: "1px solid var(--t-accent)", borderRadius: 4, padding: "3px 6px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono }} />
            <button onClick={createList} style={{ background: "var(--t-accent)", border: "none", borderRadius: 4, color: "#000", fontSize: 10, padding: "3px 7px", cursor: "pointer", fontWeight: 700 }}>✓</button>
            <button onClick={() => setCreating(false)} style={{ background: "none", border: "none", color: "var(--t-text3)", cursor: "pointer" }}>✕</button>
          </div>
        ) : (
          <button onClick={() => setCreating(true)} style={{ padding: "5px 8px", background: "none", border: "none", color: "var(--t-text3)", fontSize: 14, cursor: "pointer" }} title="New watchlist">+</button>
        )}
      </div>

      {/* ── Add stock search ── */}
      <div style={{ padding: "5px 8px", borderBottom: "1px solid var(--t-border)", position: "relative", flexShrink: 0 }}>
        <input
          ref={searchRef}
          value={search}
          onChange={e => handleSearch(e.target.value)}
          onKeyDown={handleSearchKey}
          placeholder="Search & add symbol…"
          style={{
            width: "100%", boxSizing: "border-box",
            background: "var(--t-hover)", border: "1px solid var(--t-border)",
            borderRadius: 5, padding: "5px 8px", color: "var(--t-text1)",
            fontSize: 11, outline: "none", fontFamily: mono,
          }}
          onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
          onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
        />
        {results.length > 0 && (
          <div style={{ position: "absolute", top: "100%", left: 8, right: 8, zIndex: 100, background: "var(--t-elevated)", border: "1px solid var(--t-border)", borderRadius: 7, boxShadow: "0 8px 24px rgba(0,0,0,0.5)" }}>
            {results.map((r: any, i: number) => (
              <div key={r.id} onMouseDown={() => addStock(r.id)}
                style={{ padding: "6px 10px", cursor: "pointer", display: "flex", justifyContent: "space-between", background: i === searchCursor ? "var(--t-hover)" : "transparent", borderBottom: "1px solid var(--t-border)" }}
                onMouseEnter={() => setSearchCursor(i)}
              >
                <span style={{ color: "var(--t-accent)", fontSize: 11, fontWeight: 700, fontFamily: mono }}>{r.tradingCode}</span>
                <span style={{ color: "var(--t-text3)", fontSize: 9 }}>{r.companyName?.slice(0, 24)}</span>
                <span style={{ color: catColor(r.category), fontSize: 9, fontFamily: mono }}>{r.category}</span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "flex", alignItems: "center", background: "var(--t-panel)", borderBottom: "1px solid var(--t-border)", flexShrink: 0, overflowX: "auto" }}>
        {colDefs.map((col: any) => {
          const isSorted = sortKey === col.key
          return (
            <div key={col.key} onClick={() => handleSort(col.key)}
              style={{ width: col.w, minWidth: col.w, padding: "4px 6px", cursor: "pointer", display: "flex", alignItems: "center", gap: 3, userSelect: "none", flexShrink: 0 }}>
              <span style={{ fontSize: 9, fontWeight: 700, color: isSorted ? "var(--t-accent)" : "var(--t-text3)", fontFamily: mono, whiteSpace: "nowrap" }}>
                {col.label}
              </span>
              {isSorted && <span style={{ fontSize: 8, color: "var(--t-accent)" }}>{sortDir === "asc" ? "▲" : "▼"}</span>}
            </div>
          )
        })}
      </div>

      {/* ── Rows ── */}
      <div style={{ flex: 1, overflowY: "auto", overflowX: "auto" }}>
        {sortedStocks.length === 0 ? (
          <div style={{ textAlign: "center", color: "var(--t-text3)", fontSize: 11, padding: "24px 0", fontFamily: mono }}>
            {(activeList?.stocks ?? []).length === 0 ? "No stocks — search above to add" : "No stocks match filter"}
          </div>
        ) : sortedStocks.map((s: any, rowIdx: number) => {
          const up = (s.changePercent ?? 0) >= 0
          return (
            <div key={s.stockId ?? s.tradingCode}
              onContextMenu={e => handleContextMenu(e, s)}
              style={{
                display: "flex", alignItems: "center",
                borderBottom: "1px solid var(--t-border)",
                background: rowIdx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
                cursor: "context-menu",
                transition: "background 0.08s",
              }}
              onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
              onMouseLeave={e => e.currentTarget.style.background = rowIdx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)"}
            >
              {colDefs.map((col: any) => {
                const val = getCellValue(s, col.key)
                return (
                  <div key={col.key} style={{ width: col.w, minWidth: col.w, padding: "5px 6px", flexShrink: 0, overflow: "hidden" }}>
                    {renderCell(col.key, val, up)}
                  </div>
                )
              })}
            </div>
          )
        })}
      </div>

      {/* ── Column picker modal ── */}
      {showColPicker && (
        <ColumnPicker active={cols} onClose={() => setShowColPicker(false)} onChange={handleColChange} />
      )}

      {/* ── Context menu ── */}
      {ctxMenu && (
        <ContextMenu
          x={ctxMenu.x} y={ctxMenu.y} stock={ctxMenu.stock}
          lists={lists} activeListId={active}
          onClose={() => setCtxMenu(null)}
          onAddToList={addToList}
          onRemove={removeStock}
        />
      )}
    </div>
  )
}

const iconBtn: React.CSSProperties = {
  background: "none", border: "1px solid var(--t-border)",
  borderRadius: 5, color: "var(--t-text2)", cursor: "pointer",
  padding: "3px 7px", fontSize: 12, fontFamily: mono,
  transition: "all 0.1s",
}
