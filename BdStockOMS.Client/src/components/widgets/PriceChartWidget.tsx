// @ts-nocheck
import { useState, useEffect, useMemo } from "react"
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, ReferenceLine } from "recharts"
import { useMarketData } from "@/hooks/useMarketData"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

export function PriceChartWidget({ linkedSymbol, onSymbolClick, colorGroup }: { linkedSymbol?: string; onSymbolClick?: (c: string) => void; colorGroup?: string | null }) {
  const { stocks: _s } = useMarketData()
  const stocks = _s ?? []
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)
  const [symbol, setSymbol] = useState(linkedSymbol ?? _linked ?? "GP")
  const [chartType, setChartType] = useState("line")
  const [period, setPeriod]   = useState("1D")
  const [history, setHistory] = useState([])

  useEffect(() => { if (_linked) setSymbol(_linked); else if (linkedSymbol) setSymbol(linkedSymbol) }, [_linked, linkedSymbol])

  const stock = stocks.find(s => s.tradingCode === symbol)

  useEffect(() => {
    if (!stock) return
    const base = stock.lastTradePrice
    const pts = 60
    setHistory(Array.from({ length: pts }, (_, i) => {
      const noise = (Math.random() - 0.5) * base * 0.02
      const trend = ((i / pts) - 0.5) * base * (stock.changePercent ?? 0) * 0.02
      const p = Math.max(0.01, base + trend + noise)
      return {
        t: new Date(Date.now() - (pts - i) * 60000).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        price: parseFloat(p.toFixed(2)),
        vol: Math.round(Math.random() * 5000 + 500),
      }
    }))
  }, [stock?.tradingCode])

  useEffect(() => {
    if (!stock || history.length === 0) return
    const id = setInterval(() => {
      const last = history[history.length - 1]?.price ?? stock.lastTradePrice
      const noise = (Math.random() - 0.5) * last * 0.004
      const newPt = { t: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }), price: parseFloat((last + noise).toFixed(2)), vol: Math.round(Math.random() * 3000 + 200) }
      setHistory(h => [...h.slice(-59), newPt])
    }, 3000)
    return () => clearInterval(id)
  }, [stock?.tradingCode, history.length])

  const up     = stock ? (stock.changePercent ?? 0) >= 0 : true
  const color  = up ? "#00D4AA" : "#FF6B6B"
  const minP   = useMemo(() => Math.min(...history.map(h => h.price)), [history])
  const maxP   = useMemo(() => Math.max(...history.map(h => h.price)), [history])
  const firstP = history[0]?.price

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      {/* Toolbar */}
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <input value={symbol} onChange={e => setSymbol(e.target.value.toUpperCase())} placeholder="Symbol…"
          style={{ width: 80, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "4px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        {stock && (
          <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
            <span style={{ color: "#fff", fontSize: 13, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>৳{stock.lastTradePrice?.toFixed(2)}</span>
            <span style={{ color, fontSize: 11, fontFamily: "'Space Mono',monospace" }}>{up ? "+" : ""}{stock.changePercent?.toFixed(2)}%</span>
          </div>
        )}
        <div style={{ marginLeft: "auto", display: "flex", gap: 3 }}>
          {["1D","1W","1M"].map(p => (
            <button key={p} onClick={() => setPeriod(p)} style={{ padding: "3px 6px", background: period === p ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${period === p ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: period === p ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{p}</button>
          ))}
          {["line","area"].map(t => (
            <button key={t} onClick={() => setChartType(t)} style={{ padding: "3px 6px", background: chartType === t ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${chartType === t ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: chartType === t ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{t}</button>
          ))}
        </div>
      </div>

      {/* Chart */}
      <div style={{ flex: 1, minHeight: 0 }}>
        {history.length === 0
          ? <div style={{ height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.2)", fontSize: 12, fontFamily: "'Space Mono',monospace" }}>Select a symbol to chart</div>
          : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={history} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                <XAxis dataKey="t" tick={{ fill: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "Space Mono" }} interval="preserveStartEnd" axisLine={false} tickLine={false} />
                <YAxis domain={[minP * 0.999, maxP * 1.001]} tick={{ fill: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "Space Mono" }} width={50} axisLine={false} tickLine={false} tickFormatter={v => `৳${v.toFixed(1)}`} />
                <Tooltip contentStyle={{ background: "var(--t-surface)", border: `1px solid ${color}44`, borderRadius: 6, fontSize: 11, fontFamily: "Space Mono" }} labelStyle={{ color: "rgba(255,255,255,0.4)" }} itemStyle={{ color }} formatter={v => [`৳${v}`, "Price"]} />
                {firstP && <ReferenceLine y={firstP} stroke="rgba(255,255,255,0.1)" strokeDasharray="3 3" />}
                <Line type="monotone" dataKey="price" stroke={color} strokeWidth={1.5} dot={false} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          )
        }
      </div>

      {/* OHLCV strip */}
      {stock && (
        <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "4px 8px", display: "flex", gap: 12, flexShrink: 0 }}>
          {[["O", stock.closePrice], ["H", stock.highPrice], ["L", stock.lowPrice], ["V", `${((stock.volume ?? 0) / 1000).toFixed(0)}K`]].map(([l, v]) => (
            <span key={l} style={{ fontSize: 10, fontFamily: "'Space Mono',monospace" }}>
              <span style={{ color: "rgba(255,255,255,0.3)" }}>{l} </span>
              <span style={{ color: "#fff" }}>{typeof v === "number" ? `৳${v?.toFixed(2)}` : v}</span>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
