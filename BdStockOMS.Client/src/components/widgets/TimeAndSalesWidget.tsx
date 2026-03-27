// @ts-nocheck
// src/components/widgets/TimeAndSalesWidget.tsx
// Day 63 — Full micro-level filter system:
// Aggressor, Price Range, Volume Range, Value Range, Time Range,
// Price Direction, Min Trade Count, Large Trade highlight, Reset All

import { useState, useEffect, useRef, useCallback, useMemo } from "react"
import { subscribeMarket } from "@/hooks/useSignalR"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"
import { apiClient } from "@/api/client"

const mono = "'JetBrains Mono', monospace"

type AggressorSide = 0 | 1 | -1

interface TASEntry {
  id: number
  tradeMatchId: string
  tradingCode: string
  price: number
  volume: number
  value: number
  executedAt: string
  aggressor: AggressorSide
  priceChange: number
  previousClose?: number
}

interface Filters {
  aggressor:      AggressorSide | "all"
  priceMin:       string
  priceMax:       string
  volMin:         string
  volMax:         string
  valueMin:       string
  valueMax:       string
  timeFrom:       string
  timeTo:         string
  direction:      "all" | "up" | "down" | "flat"
  largeOnly:      boolean
  largeThreshold: string
}

const DEFAULT_FILTERS: Filters = {
  aggressor:      "all",
  priceMin:       "",
  priceMax:       "",
  volMin:         "",
  volMax:         "",
  valueMin:       "",
  valueMax:       "",
  timeFrom:       "",
  timeTo:         "",
  direction:      "all",
  largeOnly:      false,
  largeThreshold: "100000",
}

function fmtTime(d: string) {
  return new Date(d).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}
function fmtVol(v: number) {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(2)}M`
  if (v >= 1_000)     return `${(v / 1_000).toFixed(1)}K`
  return v.toLocaleString()
}
function fmtVal(v: number) {
  if (v >= 1e7) return `৳${(v / 1e7).toFixed(2)}Cr`
  if (v >= 1e5) return `৳${(v / 1e5).toFixed(1)}L`
  if (v >= 1e3) return `৳${(v / 1e3).toFixed(1)}K`
  return `৳${v.toFixed(0)}`
}

function countActiveFilters(f: Filters): number {
  let n = 0
  if (f.aggressor !== "all") n++
  if (f.priceMin || f.priceMax) n++
  if (f.volMin   || f.volMax)   n++
  if (f.valueMin || f.valueMax) n++
  if (f.timeFrom || f.timeTo)   n++
  if (f.direction !== "all")    n++
  if (f.largeOnly)              n++
  return n
}

// ─── Filter Panel ──────────────────────────────────────────────────────────
function FilterPanel({ filters, onChange, onReset }: {
  filters: Filters
  onChange: (patch: Partial<Filters>) => void
  onReset: () => void
}) {
  const active = countActiveFilters(filters)

  const label = (text: string) => (
    <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono, marginBottom: 3, letterSpacing: "0.06em" }}>{text}</div>
  )

  const inp = (val: string, key: keyof Filters, placeholder: string, w = "100%") => (
    <input
      type="number" value={val} placeholder={placeholder}
      onChange={e => onChange({ [key]: e.target.value })}
      style={{ width: w, boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "4px 6px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono }}
      onFocus={e => e.currentTarget.style.borderColor = "#22d3ee"}
      onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
    />
  )

  const timeInp = (val: string, key: keyof Filters, placeholder: string) => (
    <input
      type="time" value={val} placeholder={placeholder}
      onChange={e => onChange({ [key]: e.target.value })}
      style={{ flex: 1, background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "4px 6px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono, colorScheme: "dark" }}
      onFocus={e => e.currentTarget.style.borderColor = "#22d3ee"}
      onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
    />
  )

  return (
    <div style={{ padding: "8px", borderBottom: "1px solid var(--t-border)", background: "var(--t-bg)", display: "flex", flexDirection: "column", gap: 8 }}>

      {/* Row 1: Aggressor + Direction */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
        <div>
          {label("AGGRESSOR")}
          <div style={{ display: "flex", gap: 2 }}>
            {([["all","ALL"], [1,"▲ BUY"], [-1,"▼ SELL"]] as const).map(([v, l]) => (
              <button key={String(v)} onClick={() => onChange({ aggressor: v as any })} style={{
                flex: 1, padding: "3px 0", fontSize: 8, fontWeight: 700, fontFamily: mono,
                background: filters.aggressor === v ? (v === 1 ? "rgba(0,230,118,0.12)" : v === -1 ? "rgba(255,23,68,0.12)" : "rgba(255,255,255,0.08)") : "transparent",
                border: `1px solid ${filters.aggressor === v ? (v === 1 ? "rgba(0,230,118,0.3)" : v === -1 ? "rgba(255,23,68,0.3)" : "var(--t-border)") : "var(--t-border)"}`,
                borderRadius: 3, color: filters.aggressor === v ? (v === 1 ? "var(--t-buy)" : v === -1 ? "var(--t-sell)" : "var(--t-text1)") : "var(--t-text3)", cursor: "pointer",
              }}>{l}</button>
            ))}
          </div>
        </div>
        <div>
          {label("PRICE DIRECTION")}
          <div style={{ display: "flex", gap: 2 }}>
            {([["all","ALL"], ["up","▲"], ["down","▼"], ["flat","="]] as const).map(([v, l]) => (
              <button key={v} onClick={() => onChange({ direction: v })} style={{
                flex: 1, padding: "3px 0", fontSize: 9, fontWeight: 700, fontFamily: mono,
                background: filters.direction === v ? "rgba(255,255,255,0.08)" : "transparent",
                border: `1px solid ${filters.direction === v ? "var(--t-border)" : "var(--t-border)"}`,
                borderRadius: 3,
                color: filters.direction === v ? (v === "up" ? "var(--t-buy)" : v === "down" ? "var(--t-sell)" : "var(--t-text1)") : "var(--t-text3)",
                cursor: "pointer",
              }}>{l}</button>
            ))}
          </div>
        </div>
      </div>

      {/* Row 2: Price range */}
      <div>
        {label("PRICE RANGE ৳")}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          {inp(filters.priceMin, "priceMin", "Min")}
          <span style={{ color: "var(--t-text3)", fontSize: 9 }}>—</span>
          {inp(filters.priceMax, "priceMax", "Max")}
        </div>
      </div>

      {/* Row 3: Volume range */}
      <div>
        {label("VOLUME RANGE")}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          {inp(filters.volMin, "volMin", "Min e.g. 1000")}
          <span style={{ color: "var(--t-text3)", fontSize: 9 }}>—</span>
          {inp(filters.volMax, "volMax", "Max")}
        </div>
      </div>

      {/* Row 4: Value range */}
      <div>
        {label("VALUE RANGE ৳")}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          {inp(filters.valueMin, "valueMin", "Min e.g. 50000")}
          <span style={{ color: "var(--t-text3)", fontSize: 9 }}>—</span>
          {inp(filters.valueMax, "valueMax", "Max")}
        </div>
      </div>

      {/* Row 5: Time range */}
      <div>
        {label("TIME RANGE (HH:MM)")}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          {timeInp(filters.timeFrom, "timeFrom", "From")}
          <span style={{ color: "var(--t-text3)", fontSize: 9 }}>—</span>
          {timeInp(filters.timeTo, "timeTo", "To")}
        </div>
      </div>

      {/* Row 6: Large trade filter */}
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <label style={{ display: "flex", alignItems: "center", gap: 6, cursor: "pointer" }}>
          <div
            onClick={() => onChange({ largeOnly: !filters.largeOnly })}
            style={{
              width: 28, height: 14, borderRadius: 7, cursor: "pointer", transition: "background 0.15s",
              background: filters.largeOnly ? "#22d3ee" : "var(--t-hover)",
              border: "1px solid var(--t-border)", position: "relative",
            }}
          >
            <div style={{ position: "absolute", top: 2, left: filters.largeOnly ? 14 : 2, width: 10, height: 10, borderRadius: "50%", background: "#fff", transition: "left 0.15s" }} />
          </div>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text2)" }}>LARGE TRADES ONLY</span>
        </label>
        {filters.largeOnly && (
          <div style={{ display: "flex", alignItems: "center", gap: 4, marginLeft: "auto" }}>
            <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>≥ ৳</span>
            {inp(filters.largeThreshold, "largeThreshold", "100000", "90px")}
          </div>
        )}
      </div>

      {/* Reset */}
      {active > 0 && (
        <button onClick={onReset} style={{
          padding: "4px 0", fontSize: 9, fontWeight: 700, fontFamily: mono,
          background: "rgba(255,23,68,0.08)", border: "1px solid rgba(255,23,68,0.2)",
          borderRadius: 4, color: "var(--t-sell)", cursor: "pointer",
        }}>
          ✕ Reset all filters ({active} active)
        </button>
      )}
    </div>
  )
}

// ─── Main Widget ──────────────────────────────────────────────────────────────
export function TimeAndSalesWidget({
  defaultTradingCode = "BRACBANK",
  colorGroup,
}: {
  defaultTradingCode?: string
  colorGroup?: string | null
}) {
  const [tradingCode, setTradingCode] = useState(defaultTradingCode)
  const [_linked, emitSymbol]         = useLinkedSymbol(colorGroup ?? null)
  const [inputCode, setInputCode] = useState(defaultTradingCode)
  const [exchange, setExchange] = useState<'ALL'|'DSE'|'CSE'>('ALL')
  const [entries,    setEntries]      = useState<TASEntry[]>([])
  const [loading,    setLoading]      = useState(false)
  const [showId,     setShowId]       = useState(false)
  const [showFilters,setShowFilters]  = useState(false)
  const [filters,    setFilters]      = useState<Filters>(DEFAULT_FILTERS)
  const [connected,  setConnected]    = useState(false)
  const [pinnedSymbols, setPinnedSymbols] = useState<string[]>([defaultTradingCode])
  const [activeSymbolTab, setActiveSymbolTab] = useState(defaultTradingCode)
  const [flashIds,   setFlashIds]     = useState<Set<number>>(new Set())
  const tableRef = useRef<HTMLDivElement>(null)

  useEffect(() => { if (_linked) { setTradingCode(_linked); setInputCode(_linked) } }, [_linked])

  const fetchData = useCallback(async (code: string) => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ count: "200" })
      // Pass server-side filters where supported
      if (filters.aggressor !== "all") params.set("aggressorFilter", String(filters.aggressor))
      if (filters.volMin)   params.set("minVolume", filters.volMin)
      if (filters.priceMin) params.set("minPrice",  filters.priceMin)
      if (filters.priceMax) params.set("maxPrice",  filters.priceMax)
      const res = await apiClient.get(`/api/timeandsales/${code}?${params}`).then(r => r.data as TASEntry[])
      setEntries(res)
    } catch { setEntries([]) }
    finally { setLoading(false) }
  }, [filters.aggressor, filters.volMin, filters.priceMin, filters.priceMax])

  useEffect(() => { fetchData(tradingCode) }, [tradingCode, fetchData])

  useEffect(() => {
    const unsub = subscribeMarket("ReceiveTimeAndSales", (entry: TASEntry) => {
      if (entry.tradingCode !== tradingCode) return
      setFlashIds(s => new Set([...s, entry.id]))
      setTimeout(() => setFlashIds(s => { const n = new Set(s); n.delete(entry.id); return n }), 800)
      setEntries(prev => [entry, ...prev.slice(0, 499)])
      tableRef.current?.scrollTo({ top: 0, behavior: "smooth" })
    })
    setConnected(true)
    return () => unsub()
  }, [tradingCode])

  const handleSearch = () => {
    const code = inputCode.trim().toUpperCase()
    if (code) { pinSymbol(code); emitSymbol(code) }
  }

  const pinSymbol = (code: string) => {
    if (!pinnedSymbols.includes(code)) setPinnedSymbols(p => [...p, code])
    setActiveSymbolTab(code)
    setTradingCode(code)
    setInputCode(code)
  }
  const unpinSymbol = (code: string) => {
    const next = pinnedSymbols.filter(s => s !== code)
    if (next.length === 0) return
    setPinnedSymbols(next)
    if (activeSymbolTab === code) {
      const newActive = next[next.length - 1]
      setActiveSymbolTab(newActive)
      setTradingCode(newActive)
      setInputCode(newActive)
    }
  }
  const patchFilter = (patch: Partial<Filters>) => setFilters(f => ({ ...f, ...patch }))
  const resetFilters = () => setFilters(DEFAULT_FILTERS)
  const activeFilterCount = countActiveFilters(filters)

  // ── Client-side filtering (micro-level) ────────────────────────────────────
  const filtered = useMemo(() => {
    return entries.filter(e => {
      // Aggressor
      if (filters.aggressor !== "all" && e.aggressor !== filters.aggressor) return false
      // Price range
      if (filters.priceMin && e.price < Number(filters.priceMin)) return false
      if (filters.priceMax && e.price > Number(filters.priceMax)) return false
      // Volume range
      if (filters.volMin && e.volume < Number(filters.volMin)) return false
      if (filters.volMax && e.volume > Number(filters.volMax)) return false
      // Value range
      if (filters.valueMin && e.value < Number(filters.valueMin)) return false
      if (filters.valueMax && e.value > Number(filters.valueMax)) return false
      // Time range
      if (filters.timeFrom || filters.timeTo) {
        const t = new Date(e.executedAt)
        const hhmm = `${String(t.getHours()).padStart(2,"0")}:${String(t.getMinutes()).padStart(2,"0")}`
        if (filters.timeFrom && hhmm < filters.timeFrom) return false
        if (filters.timeTo   && hhmm > filters.timeTo)   return false
      }
      // Direction
      if (filters.direction === "up"   && e.priceChange <= 0) return false
      if (filters.direction === "down" && e.priceChange >= 0) return false
      if (filters.direction === "flat" && e.priceChange !== 0) return false
      // Large trades
      if (filters.largeOnly && e.value < Number(filters.largeThreshold || 100000)) return false
      return true
    })
  }, [entries, filters])

  // ── Stats ──────────────────────────────────────────────────────────────────
  const buyCount  = filtered.filter(e => e.aggressor === 1).length
  const sellCount = filtered.filter(e => e.aggressor === -1).length
  const totalVol  = filtered.reduce((s, e) => s + e.volume, 0)
  const totalVal  = filtered.reduce((s, e) => s + e.value, 0)
  const avgPrice  = filtered.length > 0 ? filtered.reduce((s, e) => s + e.price, 0) / filtered.length : 0
  const maxVol    = filtered.length > 0 ? Math.max(...filtered.map(e => e.volume)) : 0

  const cols = showId
    ? "68px 1fr 4.5rem 4rem 4rem 4rem"
    : "68px 4.5rem 4rem 4rem 4rem"

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <div style={{ width: 3, height: 14, borderRadius: 2, background: "linear-gradient(180deg, #22d3ee, #3b82f6)" }} />
          <span style={{ fontSize: 10, fontWeight: 800, color: "var(--t-text1)", fontFamily: mono, letterSpacing: "0.06em" }}>T&S</span>
          <span style={{ fontSize: 11, fontWeight: 800, color: "#22d3ee", fontFamily: mono }}>{tradingCode}</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
          {/* Filter toggle */}
          <button onClick={() => setShowFilters(v => !v)} style={{
            padding: "2px 8px", fontSize: 8, fontWeight: 700, fontFamily: mono,
            background: showFilters || activeFilterCount > 0 ? "rgba(34,211,238,0.12)" : "transparent",
            border: `1px solid ${showFilters || activeFilterCount > 0 ? "#22d3ee" : "var(--t-border)"}`,
            borderRadius: 4, color: showFilters || activeFilterCount > 0 ? "#22d3ee" : "var(--t-text3)", cursor: "pointer",
            display: "flex", alignItems: "center", gap: 4,
          }}>
            ⚙ FILTER
            {activeFilterCount > 0 && (
              <span style={{ background: "#22d3ee", color: "#000", borderRadius: "50%", width: 14, height: 14, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 8, fontWeight: 800 }}>{activeFilterCount}</span>
            )}
          </button>
          {/* Match ID toggle */}
          <button onClick={() => setShowId(v => !v)} title="Toggle Trade Match ID" style={{
            padding: "2px 6px", fontSize: 8, fontWeight: 700, fontFamily: mono,
            background: showId ? "rgba(34,211,238,0.12)" : "transparent",
            border: `1px solid ${showId ? "#22d3ee" : "var(--t-border)"}`,
            borderRadius: 4, color: showId ? "#22d3ee" : "var(--t-text3)", cursor: "pointer",
          }}>ID</button>
          {/* Live indicator */}
          <div style={{ display: "flex", alignItems: "center", gap: 3 }}>
            <div style={{ width: 6, height: 6, borderRadius: "50%", background: connected ? "var(--t-buy)" : "var(--t-text3)", animation: connected ? "oms-pulse 2s infinite" : "none" }} />
            <span style={{ fontSize: 8, fontFamily: mono, color: connected ? "var(--t-buy)" : "var(--t-text3)" }}>LIVE</span>
          </div>
        </div>
      </div>

      {/* ── Symbol search bar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-bg)" }}>
        <input
          value={inputCode}
          onChange={e => setInputCode(e.target.value.toUpperCase())}
          onKeyDown={e => e.key === "Enter" && handleSearch()}
          placeholder="Code…"
          style={{ width: 80, background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono, fontWeight: 700 }}
          onFocus={e => e.currentTarget.style.borderColor = "#22d3ee"}
          onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
        />
        <button onClick={handleSearch} style={{ padding: "4px 10px", fontSize: 9, fontWeight: 800, fontFamily: mono, background: "rgba(34,211,238,0.1)", border: "1px solid rgba(34,211,238,0.3)", borderRadius: 5, color: "#22d3ee", cursor: "pointer" }}>GO</button>
        {/* Quick aggressor shortcuts */}
        <div style={{ display: "flex", gap: 3, marginLeft: "auto" }}>
          {([["all","ALL"], [1,"▲ B"], [-1,"▼ S"]] as const).map(([v, l]) => (
            <button key={String(v)} onClick={() => patchFilter({ aggressor: v as any })} style={{
              padding: "3px 6px", fontSize: 8, fontWeight: 700, fontFamily: mono,
              background: filters.aggressor === v ? (v === 1 ? "rgba(0,230,118,0.12)" : v === -1 ? "rgba(255,23,68,0.12)" : "rgba(255,255,255,0.08)") : "transparent",
              border: `1px solid ${filters.aggressor === v ? (v === 1 ? "rgba(0,230,118,0.3)" : v === -1 ? "rgba(255,23,68,0.3)" : "var(--t-border)") : "var(--t-border)"}`,
              borderRadius: 3,
              color: filters.aggressor === v ? (v === 1 ? "var(--t-buy)" : v === -1 ? "var(--t-sell)" : "var(--t-text1)") : "var(--t-text3)",
              cursor: "pointer",
            }}>{l}</button>
          ))}
        </div>
      </div>


      {/* ── Pinned Symbol Tabs ── */}
      {pinnedSymbols.length > 0 && (
        <div style={{ display: "flex", gap: 2, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)", overflowX: "auto" }}>
          {pinnedSymbols.map(sym => (
            <div key={sym} style={{ display: "flex", alignItems: "center", gap: 2,
              background: activeSymbolTab === sym ? "rgba(34,211,238,0.12)" : "var(--t-hover)",
              border: `1px solid ${activeSymbolTab === sym ? "rgba(34,211,238,0.4)" : "var(--t-border)"}`,
              borderRadius: 5, padding: "2px 6px", cursor: "pointer", flexShrink: 0 }}
              onClick={() => { setActiveSymbolTab(sym); setTradingCode(sym); setInputCode(sym) }}>
              <span style={{ fontSize: 10, fontWeight: 700, fontFamily: mono,
                color: activeSymbolTab === sym ? "#22d3ee" : "var(--t-text2)" }}>{sym}</span>
              {pinnedSymbols.length > 1 && (
                <span onClick={e => { e.stopPropagation(); unpinSymbol(sym) }}
                  style={{ fontSize: 8, color: "var(--t-text3)", cursor: "pointer", marginLeft: 2,
                    lineHeight: 1, padding: "0 1px" }}>✕</span>
              )}
            </div>
          ))}
          <div style={{ display: "flex", alignItems: "center", marginLeft: "auto", flexShrink: 0 }}>
            <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
              {pinnedSymbols.length}/6
            </span>
          </div>
        </div>
      )}

      {/* ── Collapsible Filter Panel ── */}
      {showFilters && (
        <FilterPanel filters={filters} onChange={patchFilter} onReset={resetFilters} />
      )}

      {/* ── Stats Bar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "3px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)", overflowX: "auto" }}>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)", flexShrink: 0 }}>
          Vol: <span style={{ color: "var(--t-text2)", fontWeight: 700 }}>{fmtVol(totalVol)}</span>
        </span>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)", flexShrink: 0 }}>
          Val: <span style={{ color: "var(--t-text2)", fontWeight: 700 }}>{fmtVal(totalVal)}</span>
        </span>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-buy)", flexShrink: 0 }}>▲ {buyCount}</span>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-sell)", flexShrink: 0 }}>▼ {sellCount}</span>
        {avgPrice > 0 && (
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)", flexShrink: 0 }}>
            Avg: <span style={{ color: "var(--t-text2)" }}>৳{avgPrice.toFixed(2)}</span>
          </span>
        )}
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)", marginLeft: "auto", flexShrink: 0 }}>
          {filtered.length}/{entries.length}
        </span>
      </div>

      {/* ── Column Headers ── */}
      <div style={{ display: "grid", gridTemplateColumns: cols, padding: "3px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-bg)" }}>
        {["TIME", ...(showId ? ["MATCH ID"] : []), "PRICE", "VOL", "VALUE", "SIDE"].map(h => (
          <span key={h} style={{ fontSize: 8, fontWeight: 700, fontFamily: mono, color: "var(--t-text3)", letterSpacing: "0.06em", textAlign: ["PRICE","VOL","VALUE"].includes(h) ? "right" : h === "SIDE" ? "center" : "left", display: "block" }}>{h}</span>
        ))}
      </div>

      {/* ── Rows ── */}
      <div ref={tableRef} style={{ flex: 1, overflowY: "auto" }}>
        {loading && entries.length === 0 && (
          <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: 60, color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>Loading…</div>
        )}
        {!loading && filtered.length === 0 && entries.length > 0 && (
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: 60, gap: 4, color: "var(--t-text3)" }}>
            <span style={{ fontSize: 10, fontFamily: mono }}>No trades match filters</span>
            <button onClick={resetFilters} style={{ fontSize: 9, fontFamily: mono, color: "#22d3ee", background: "none", border: "none", cursor: "pointer" }}>Reset filters</button>
          </div>
        )}
        {filtered.map((e, idx) => {
          const isUp    = e.priceChange > 0
          const isDown  = e.priceChange < 0
          const isFlash = flashIds.has(e.id)
          const isBuy   = e.aggressor === 1
          const isSell  = e.aggressor === -1
          const isLarge = filters.largeOnly || e.value >= Number(filters.largeThreshold || 100000)
          const volPct  = maxVol > 0 ? e.volume / maxVol : 0

          return (
            <div key={e.id} style={{
              display: "grid", gridTemplateColumns: cols,
              alignItems: "center", padding: "4px 8px",
              borderBottom: "1px solid var(--t-border)",
              background: isFlash
                ? "rgba(34,211,238,0.06)"
                : isLarge && !filters.largeOnly
                  ? "rgba(245,158,11,0.04)"
                  : idx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
              transition: "background 0.5s",
              borderLeft: isLarge && !filters.largeOnly ? "2px solid rgba(245,158,11,0.4)" : "2px solid transparent",
            }}>
              {/* Time */}
              <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>{fmtTime(e.executedAt)}</span>

              {/* Match ID */}
              {showId && (
                <span style={{ fontSize: 8, fontFamily: mono, color: "var(--t-text3)", overflow: "hidden", textOverflow: "ellipsis" }} title={e.tradeMatchId}>
                  {e.tradeMatchId?.split("-")[1] ?? "—"}
                </span>
              )}

              {/* Price */}
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: 11, fontWeight: 800, fontFamily: mono, color: isUp ? "var(--t-buy)" : isDown ? "var(--t-sell)" : "var(--t-text1)" }}>
                  {e.price.toFixed(2)}
                  {isUp && <span style={{ fontSize: 7, marginLeft: 1 }}>▲</span>}
                  {isDown && <span style={{ fontSize: 7, marginLeft: 1 }}>▼</span>}
                </span>
              </div>

              {/* Volume with bar */}
              <div style={{ textAlign: "right", position: "relative" }}>
                <div style={{ position: "absolute", right: 0, top: 0, bottom: 0, width: `${volPct * 100}%`, background: isBuy ? "rgba(0,230,118,0.06)" : isSell ? "rgba(255,23,68,0.06)" : "rgba(255,255,255,0.03)", borderRadius: 2 }} />
                <span style={{ fontSize: 10, fontFamily: mono, color: "var(--t-text2)", position: "relative" }}>{fmtVol(e.volume)}</span>
              </div>

              {/* Value */}
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: 9, fontFamily: mono, color: isLarge ? "#f59e0b" : "var(--t-text3)" }}>
                  {fmtVal(e.value)}
                </span>
              </div>

              {/* Aggressor */}
              <div style={{ display: "flex", justifyContent: "center" }}>
                {isBuy ? (
                  <span style={{ fontSize: 8, fontWeight: 800, fontFamily: mono, padding: "1px 5px", borderRadius: 3, background: "rgba(0,230,118,0.1)", color: "var(--t-buy)", border: "1px solid rgba(0,230,118,0.2)" }}>▲ B</span>
                ) : isSell ? (
                  <span style={{ fontSize: 8, fontWeight: 800, fontFamily: mono, padding: "1px 5px", borderRadius: 3, background: "rgba(255,23,68,0.1)", color: "var(--t-sell)", border: "1px solid rgba(255,23,68,0.2)" }}>▼ S</span>
                ) : (
                  <span style={{ fontSize: 8, fontFamily: mono, color: "var(--t-text3)" }}>—</span>
                )}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

export default TimeAndSalesWidget
