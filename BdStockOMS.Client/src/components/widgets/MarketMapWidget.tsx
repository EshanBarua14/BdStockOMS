// @ts-nocheck
import { useMemo, useState } from "react"
import { useMarketData } from "@/hooks/useMarketData"

export function MarketMapWidget({ onSymbolClick }) {
  const { stocks } = useMarketData()
  const [colorBy, setColorBy] = useState("change")
  const [sizeBy,  setSizeBy]  = useState("volume")
  const [sector,  setSector]  = useState("All")

  const sectors = useMemo(() => ["All", ...new Set(stocks.map(s => s.category).filter(Boolean))], [stocks])

  const tiles = useMemo(() => {
    let s = stocks.filter(x => x.lastTradePrice > 0)
    if (sector !== "All") s = s.filter(x => x.category === sector)
    const maxVol = Math.max(...s.map(x => x.volume ?? 1), 1)
    return s.map(x => {
      const chg  = x.changePercent ?? 0
      const size = Math.max(0.3, (x.volume ?? 0) / maxVol)
      const r = chg >= 3 ? 0 : chg >= 0 ? Math.round(chg / 3 * 30) : Math.round(Math.min(1, Math.abs(chg) / 5) * 200)
      const g = chg >= 0 ? Math.round(Math.min(1, chg / 5) * 212) : 0
      const b = chg < 0 ? Math.round(Math.abs(chg) / 5 * 50) : 170
      const bg = chg > 0.1 ? `rgba(0,${g},${b},0.85)` : chg < -0.1 ? `rgba(${r},0,${b * 0.3},0.85)` : "rgba(50,50,70,0.85)"
      return { ...x, size, bg, chg }
    }).sort((a, b) => b.size - a.size)
  }, [stocks, sector, colorBy, sizeBy])

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "#0A0F1C", overflow: "hidden" }}>
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <select value={sector} onChange={e => setSector(e.target.value)} style={{ background: "#0D1320", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 4, color: "rgba(255,255,255,0.6)", fontSize: 10, padding: "3px 6px", cursor: "pointer", maxWidth: 120 }}>
          {sectors.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <div style={{ marginLeft: "auto", display: "flex", gap: 4 }}>
          {[["change","% Chg"],["volume","Vol"]].map(([v,l]) => (
            <button key={v} onClick={() => setColorBy(v)} style={{ padding: "3px 6px", background: colorBy === v ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${colorBy === v ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: colorBy === v ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 9, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{l}</button>
          ))}
        </div>
      </div>

      <div style={{ flex: 1, overflow: "auto", padding: 4 }}>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 2, alignContent: "flex-start" }}>
          {tiles.map(s => (
            <div key={s.id} onClick={() => onSymbolClick?.(s.tradingCode)}
              style={{ background: s.bg, borderRadius: 3, display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", cursor: "pointer", overflow: "hidden", transition: "transform 0.1s", padding: "3px 2px", minWidth: 48, width: `calc(${Math.max(5, s.size * 14)}% - 4px)`, minHeight: 44, boxSizing: "border-box" }}
              onMouseEnter={e => e.currentTarget.style.transform = "scale(1.04)"}
              onMouseLeave={e => e.currentTarget.style.transform = "scale(1)"}>
              <span style={{ color: "rgba(255,255,255,0.9)", fontSize: Math.max(8, Math.min(11, s.size * 12)), fontFamily: "'Space Mono',monospace", fontWeight: 700, textAlign: "center", lineHeight: 1.2 }}>{s.tradingCode}</span>
              <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{s.chg >= 0 ? "+" : ""}{s.chg.toFixed(1)}%</span>
            </div>
          ))}
        </div>
      </div>

      {/* Legend */}
      <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "4px 8px", display: "flex", gap: 12, alignItems: "center", flexShrink: 0 }}>
        {[["rgba(0,180,100,0.9)","Strong Buy"],["rgba(0,100,50,0.9)","Gain"],["rgba(50,50,70,0.9)","Flat"],["rgba(150,0,20,0.9)","Loss"],["rgba(200,0,30,0.9)","Strong Loss"]].map(([c,l]) => (
          <span key={l} style={{ display: "flex", alignItems: "center", gap: 3, fontSize: 9, color: "rgba(255,255,255,0.3)", fontFamily: "'Space Mono',monospace" }}>
            <span style={{ width: 8, height: 8, borderRadius: 2, background: c, display: "inline-block" }} />{l}
          </span>
        ))}
      </div>
    </div>
  )
}
