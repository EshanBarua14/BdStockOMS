// @ts-nocheck
// src/components/widgets/TimeAndSalesWidget.tsx
// Day 63 redesign — matches OMS design language: JetBrains Mono, var(--t-*) tokens,
// TradeMatchId toggle, Aggressor badge, flash animation, stats bar, color group sync

import { useState, useEffect, useRef, useCallback } from "react"
import { subscribeMarket } from "@/hooks/useSignalR"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"
import { apiClient } from "@/api/client"

const mono = "'JetBrains Mono', monospace"

// ─── Types ────────────────────────────────────────────────────────────────────
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

// ─── Helpers ──────────────────────────────────────────────────────────────────
function fmtTime(d: string) {
  return new Date(d).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}
function fmtVol(v: number) {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(2)}M`
  if (v >= 1_000)     return `${(v / 1_000).toFixed(1)}K`
  return v.toLocaleString()
}

// ─── Component ────────────────────────────────────────────────────────────────
export function TimeAndSalesWidget({
  defaultTradingCode = "BRACBANK",
  colorGroup,
}: {
  defaultTradingCode?: string
  colorGroup?: string | null
}) {
  const [tradingCode, setTradingCode] = useState(defaultTradingCode)
  const [_linked, emitSymbol]         = useLinkedSymbol(colorGroup ?? null)
  const [inputCode,  setInputCode]    = useState(defaultTradingCode)
  const [entries,    setEntries]      = useState<TASEntry[]>([])
  const [loading,    setLoading]      = useState(false)
  const [showId,     setShowId]       = useState(false)
  const [aggrFilter, setAggrFilter]   = useState<AggressorSide | "all">("all")
  const [connected,  setConnected]    = useState(false)
  const [flashIds,   setFlashIds]     = useState<Set<number>>(new Set())
  const tableRef = useRef<HTMLDivElement>(null)

  // Sync from color group
  useEffect(() => { if (_linked) { setTradingCode(_linked); setInputCode(_linked) } }, [_linked])

  // Fetch history
  const fetchData = useCallback(async (code: string) => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ count: "80" })
      if (aggrFilter !== "all") params.set("aggressorFilter", String(aggrFilter))
      const res = await apiClient.get(`/api/timeandsales/${code}?${params}`).then(r => r.data as TASEntry[])
      setEntries(res)
    } catch { setEntries([]) }
    finally { setLoading(false) }
  }, [aggrFilter])

  useEffect(() => { fetchData(tradingCode) }, [tradingCode, fetchData])

  // SignalR live trades
  useEffect(() => {
    const unsub = subscribeMarket("ReceiveTimeAndSales", (entry: TASEntry) => {
      if (entry.tradingCode !== tradingCode) return
      if (aggrFilter !== "all" && entry.aggressor !== aggrFilter) return
      setFlashIds(s => new Set([...s, entry.id]))
      setTimeout(() => setFlashIds(s => { const n = new Set(s); n.delete(entry.id); return n }), 800)
      setEntries(prev => [entry, ...prev.slice(0, 199)])
      tableRef.current?.scrollTo({ top: 0, behavior: "smooth" })
    })
    setConnected(true)
    return () => unsub()
  }, [tradingCode, aggrFilter])

  const handleSearch = () => {
    const code = inputCode.trim().toUpperCase()
    if (code) { setTradingCode(code); emitSymbol(code) }
  }

  const filtered = aggrFilter === "all" ? entries : entries.filter(e => e.aggressor === aggrFilter)
  const buyCount  = entries.filter(e => e.aggressor === 1).length
  const sellCount = entries.filter(e => e.aggressor === -1).length
  const totalVol  = entries.reduce((s, e) => s + e.volume, 0)

  // Column grid
  const cols = showId
    ? "72px 1fr 5rem 4.5rem 4.5rem 4.5rem"
    : "72px 5rem 4.5rem 4.5rem 4.5rem"

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <div style={{ width: 3, height: 14, borderRadius: 2, background: "linear-gradient(180deg, #22d3ee, #3b82f6)" }} />
          <span style={{ fontSize: 10, fontWeight: 800, color: "var(--t-text1)", fontFamily: mono, letterSpacing: "0.06em" }}>T&S</span>
          <span style={{ fontSize: 11, fontWeight: 800, color: "#22d3ee", fontFamily: mono }}>{tradingCode}</span>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          {/* Toggle Match ID */}
          <button onClick={() => setShowId(v => !v)} title="Toggle Trade Match ID"
            style={{
              padding: "2px 7px", fontSize: 8, fontWeight: 700, fontFamily: mono,
              background: showId ? "rgba(34,211,238,0.12)" : "transparent",
              border: `1px solid ${showId ? "#22d3ee" : "var(--t-border)"}`,
              borderRadius: 4, color: showId ? "#22d3ee" : "var(--t-text3)", cursor: "pointer",
            }}>ID</button>
          {/* Live indicator */}
          <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <div style={{ width: 6, height: 6, borderRadius: "50%", background: connected ? "var(--t-buy)" : "var(--t-text3)", boxShadow: connected ? "0 0 6px var(--t-buy)" : "none", animation: connected ? "oms-pulse 2s infinite" : "none" }} />
            <span style={{ fontSize: 8, fontFamily: mono, color: connected ? "var(--t-buy)" : "var(--t-text3)" }}>LIVE</span>
          </div>
        </div>
      </div>

      {/* ── Search + Aggressor Filter ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-bg)" }}>
        <input
          value={inputCode}
          onChange={e => setInputCode(e.target.value.toUpperCase())}
          onKeyDown={e => e.key === "Enter" && handleSearch()}
          placeholder="Code…"
          style={{
            width: 80, background: "var(--t-hover)", border: "1px solid var(--t-border)",
            borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)",
            fontSize: 10, outline: "none", fontFamily: mono, fontWeight: 700,
          }}
          onFocus={e => e.currentTarget.style.borderColor = "#22d3ee"}
          onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
        />
        <button onClick={handleSearch} style={{
          padding: "4px 10px", fontSize: 9, fontWeight: 800, fontFamily: mono,
          background: "rgba(34,211,238,0.1)", border: "1px solid rgba(34,211,238,0.3)",
          borderRadius: 5, color: "#22d3ee", cursor: "pointer",
        }}>GO</button>

        <div style={{ display: "flex", gap: 3, marginLeft: "auto" }}>
          {(["all", 1, -1] as const).map(side => (
            <button key={String(side)} onClick={() => setAggrFilter(side)} style={{
              padding: "3px 7px", fontSize: 8, fontWeight: 700, fontFamily: mono,
              background: aggrFilter === side
                ? side === 1 ? "rgba(0,230,118,0.12)" : side === -1 ? "rgba(255,23,68,0.12)" : "rgba(255,255,255,0.08)"
                : "transparent",
              border: `1px solid ${aggrFilter === side
                ? side === 1 ? "rgba(0,230,118,0.3)" : side === -1 ? "rgba(255,23,68,0.3)" : "var(--t-border)"
                : "var(--t-border)"}`,
              borderRadius: 4,
              color: aggrFilter === side
                ? side === 1 ? "var(--t-buy)" : side === -1 ? "var(--t-sell)" : "var(--t-text1)"
                : "var(--t-text3)",
              cursor: "pointer",
            }}>
              {side === "all" ? "ALL" : side === 1 ? "▲ BUY" : "▼ SELL"}
            </button>
          ))}
        </div>
      </div>

      {/* ── Stats Bar ── */}
      {entries.length > 0 && (
        <div style={{ display: "flex", alignItems: "center", gap: 12, padding: "3px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
            Vol: <span style={{ color: "var(--t-text2)", fontWeight: 700 }}>{fmtVol(totalVol)}</span>
          </span>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-buy)" }}>
            ▲ {buyCount}
          </span>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-sell)" }}>
            ▼ {sellCount}
          </span>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)", marginLeft: "auto" }}>
            {filtered.length} trades
          </span>
        </div>
      )}

      {/* ── Column Headers ── */}
      <div style={{ display: "grid", gridTemplateColumns: cols, padding: "3px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-bg)" }}>
        {["TIME", ...(showId ? ["MATCH ID"] : []), "PRICE", "VOL", "VALUE", "SIDE"].map(h => (
          <span key={h} style={{ fontSize: 8, fontWeight: 700, fontFamily: mono, color: "var(--t-text3)", letterSpacing: "0.06em", textAlign: h === "PRICE" || h === "VOL" || h === "VALUE" ? "right" : h === "SIDE" ? "center" : "left", display: "block" }}>{h}</span>
        ))}
      </div>

      {/* ── Rows ── */}
      <div ref={tableRef} style={{ flex: 1, overflowY: "auto" }}>
        {loading && entries.length === 0 && (
          <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: 60, color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>Loading…</div>
        )}
        {filtered.map((e, idx) => {
          const isUp    = e.priceChange > 0
          const isDown  = e.priceChange < 0
          const isFlash = flashIds.has(e.id)
          const isBuy   = e.aggressor === 1
          const isSell  = e.aggressor === -1

          return (
            <div key={e.id} style={{
              display: "grid", gridTemplateColumns: cols,
              alignItems: "center", padding: "4px 8px",
              borderBottom: "1px solid var(--t-border)",
              background: isFlash ? "rgba(34,211,238,0.06)" : idx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
              transition: "background 0.5s",
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
                  {isUp && <span style={{ fontSize: 7, marginLeft: 2 }}>▲</span>}
                  {isDown && <span style={{ fontSize: 7, marginLeft: 2 }}>▼</span>}
                </span>
              </div>

              {/* Volume */}
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: 10, fontFamily: mono, color: "var(--t-text2)" }}>{fmtVol(e.volume)}</span>
              </div>

              {/* Value */}
              <div style={{ textAlign: "right" }}>
                <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
                  {(e.value / 1000).toFixed(1)}K
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
