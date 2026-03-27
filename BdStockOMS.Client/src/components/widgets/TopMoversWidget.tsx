// @ts-nocheck
import { useLinkedSymbol } from '@/hooks/useColorGroupSync';
// @ts-nocheck
import { useMemo, useState } from "react"
import { useMarketData } from "@/hooks/useMarketData"

export function TopMoversWidget({ onSymbolClick, colorGroup }: { onSymbolClick?: (s: string) => void; colorGroup?: string | null }) {
  const { stocks: _s, connected } = useMarketData()
  const [_linked2, emitSymbol] = useLinkedSymbol(colorGroup ?? null)
  const stocks = _s ?? []
  const [tab, setTab]   = useState("gainers")
  const [count, setCount] = useState(10)
  const [exch, setExch]   = useState("All")

  const filtered = useMemo(() => {
    let s = stocks.filter(x => x.lastTradePrice > 0)
    if (exch !== "All") s = s.filter(x => x.exchange === exch)
    const sorted = [...s].sort((a, b) => tab === "gainers"
      ? (b.changePercent ?? 0) - (a.changePercent ?? 0)
      : (a.changePercent ?? 0) - (b.changePercent ?? 0))
    return sorted.slice(0, count)
  }, [stocks, tab, count, exch])

  const exchanges = ["All", ...new Set(stocks.map(s => s.exchange).filter(Boolean))]

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      <div style={{ display: "flex", borderBottom: "1px solid rgba(255,255,255,0.06)", flexShrink: 0 }}>
        {[["gainers","▲ Gainers","#00D4AA"],["losers","▼ Losers","#FF6B6B"]].map(([t, l, c]) => (
          <button key={t} onClick={() => setTab(t)} style={{ flex: 1, padding: "7px 0", background: "none", border: "none", borderBottom: `2px solid ${tab === t ? c : "transparent"}`, color: tab === t ? c : "rgba(255,255,255,0.35)", fontSize: 11, cursor: "pointer", fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{l}</button>
        ))}
      </div>

      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        {exchanges.slice(0,4).map(e => (
          <button key={e} onClick={() => setExch(e)} style={{ padding: "3px 7px", background: exch === e ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${exch === e ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: exch === e ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{e}</button>
        ))}
        <select value={count} onChange={e => setCount(Number(e.target.value))} style={{ marginLeft: "auto", background: "var(--t-surface)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 4, color: "rgba(255,255,255,0.5)", fontSize: 10, padding: "2px 4px", cursor: "pointer" }}>
          {[5,10,15,20].map(n => <option key={n} value={n}>Top {n}</option>)}
        </select>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "24px 1fr 80px 70px 60px", gap: 4, padding: "4px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", flexShrink: 0 }}>
        {["#","SYMBOL","PRICE","CHG%","VOL"].map(h => (
          <span key={h} style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{h}</span>
        ))}
      </div>

      <div style={{ flex: 1, overflowY: "auto" }}>
        {filtered.map((s, i) => {
          const up = (s.changePercent ?? 0) >= 0
          const barW = Math.min(100, Math.abs(s.changePercent ?? 0) * 10)
          return (
            <div key={s.id} onClick={() => { emitSymbol(s.tradingCode); onSymbolClick?.(s.tradingCode) }}
              style={{ display: "grid", gridTemplateColumns: "24px 1fr 80px 70px 60px", gap: 4, padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.03)", cursor: "pointer", position: "relative", overflow: "hidden" }}>
              {/* background bar */}
              <div style={{ position: "absolute", left: 0, top: 0, bottom: 0, width: `${barW}%`, background: up ? "rgba(0,212,170,0.04)" : "rgba(255,107,107,0.04)", pointerEvents: "none" }} />
              <span style={{ color: "rgba(255,255,255,0.25)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{i+1}</span>
              <div>
                <div style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{s.tradingCode}</div>
                <div style={{ color: "rgba(255,255,255,0.25)", fontSize: 9 }}>{s.companyName?.slice(0,18)}</div>
              </div>
              <span style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>৳{(s.lastTradePrice ?? 0).toFixed(2)}</span>
              <span style={{ color: up ? "#00D4AA" : "#FF6B6B", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{up ? "+" : ""}{(s.changePercent ?? 0).toFixed(2)}%</span>
              <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{((s.volume ?? 0)/1000).toFixed(0)}K</span>
            </div>
          )
        })}
      </div>

      <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "3px 8px", display: "flex", justifyContent: "space-between", flexShrink: 0 }}>
        <span style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{stocks.length} stocks</span>
        <span style={{ color: connected ? "#00D4AA" : "#FF6B6B", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{connected ? "● LIVE" : "○ offline"}</span>
      </div>
    </div>
  )
}
