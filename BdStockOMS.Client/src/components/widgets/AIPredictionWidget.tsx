import { useLinkedSymbol } from '@/hooks/useColorGroupSync';
// @ts-nocheck
import { useState, useEffect, useMemo } from "react"
import { useMarketData } from "@/hooks/useMarketData"

function momentumSignal(stocks, symbol) {
  const s = stocks.find(x => x.tradingCode === symbol)
  if (!s) return null
  const chg      = s.changePercent ?? 0
  const vol      = s.volume ?? 0
  const avgVol   = stocks.reduce((a, x) => a + (x.volume ?? 0), 0) / (stocks.length || 1)
  const volRatio = vol / (avgVol || 1)
  const price    = s.lastTradePrice ?? 0
  const high     = s.highPrice ?? price
  const low      = s.lowPrice  ?? price
  const range    = high - low
  const pos      = range > 0 ? (price - low) / range : 0.5

  let score = 50
  score += chg * 4
  score += (volRatio - 1) * 8
  score += (pos - 0.5) * 20
  score = Math.max(5, Math.min(95, score))

  const signal = score >= 65 ? "BUY" : score <= 35 ? "SELL" : "HOLD"
  const conf   = Math.round(Math.abs(score - 50) * 2)

  return {
    score: Math.round(score), signal, confidence: conf,
    factors: [
      { name: "Momentum",     val: chg,              score: Math.min(100, 50 + chg * 8) },
      { name: "Volume",       val: volRatio.toFixed(2) + "x", score: Math.min(100, 50 + (volRatio - 1) * 20) },
      { name: "Price Pos.",   val: (pos * 100).toFixed(0) + "%", score: pos * 100 },
      { name: "Change%",      val: chg.toFixed(2) + "%", score: Math.min(100, 50 + chg * 5) },
    ]
  }
}

const SIG_COLORS = { BUY: "#00D4AA", SELL: "#FF6B6B", HOLD: "#F59E0B" }

export function AIPredictionWidget() {
  const { stocks: _s } = useMarketData()
  const stocks = _s ?? []
  const [symbol, setSymbol] = useState("")
  const [pred, setPred]     = useState(null)
  const [scanning, setScanning] = useState(false)
  const [topPicks, setTopPicks] = useState([])

  const stock = stocks.find(s => s.tradingCode === symbol)

  useEffect(() => {
    if (!symbol || !stock) { setPred(null); return }
    setPred(momentumSignal(stocks, symbol))
    const id = setInterval(() => setPred(momentumSignal(stocks, symbol)), 5000)
    return () => clearInterval(id)
  }, [symbol, stock?.lastTradePrice])

  const scanAll = () => {
    setScanning(true)
    setTimeout(() => {
      const picks = stocks
        .map(s => ({ ...s, sig: momentumSignal(stocks, s.tradingCode) }))
        .filter(s => s.sig?.signal === "BUY" && s.sig.confidence > 50)
        .sort((a, b) => b.sig.score - a.sig.score)
        .slice(0, 5)
      setTopPicks(picks)
      setScanning(false)
    }, 800)
  }

  const arcPath = (pct) => {
    const r = 50, cx = 60, cy = 65
    const start = Math.PI, end = Math.PI + (pct / 100) * Math.PI
    const x1 = cx + r * Math.cos(start), y1 = cy + r * Math.sin(start)
    const x2 = cx + r * Math.cos(end),   y2 = cy + r * Math.sin(end)
    return `M ${x1} ${y1} A ${r} ${r} 0 ${pct > 50 ? 1 : 0} 1 ${x2} ${y2}`
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <span style={{ color: "#8B5CF6", fontSize: 10, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>⬡ AI SIGNAL</span>
        <input value={symbol} onChange={e => setSymbol(e.target.value.toUpperCase())} placeholder="Symbol…"
          style={{ flex: 1, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "4px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        <button onClick={scanAll} disabled={scanning} style={{ padding: "4px 8px", background: "rgba(139,92,246,0.15)", border: "1px solid rgba(139,92,246,0.3)", borderRadius: 5, color: "#8B5CF6", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>
          {scanning ? "Scanning…" : "Scan All"}
        </button>
      </div>

      <div style={{ flex: 1, overflowY: "auto", padding: "8px 10px" }}>
        {pred ? (
          <div style={{ display: "flex", flexDirection: "column", gap: 10 }}>
            {/* Signal gauge */}
            <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
              <svg width="120" height="70" viewBox="0 0 120 70">
                <path d={arcPath(100)} fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="8" strokeLinecap="round" />
                <path d={arcPath(pred.score)} fill="none" stroke={SIG_COLORS[pred.signal]} strokeWidth="8" strokeLinecap="round" style={{ transition: "all 0.5s" }} />
                <text x="60" y="58" textAnchor="middle" fill={SIG_COLORS[pred.signal]} fontSize="20" fontFamily="Space Mono" fontWeight="bold">{pred.signal}</text>
                <text x="60" y="70" textAnchor="middle" fill="rgba(255,255,255,0.3)" fontSize="9" fontFamily="Space Mono">{pred.score}/100</text>
              </svg>
              <div>
                <div style={{ color: SIG_COLORS[pred.signal], fontSize: 22, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{pred.signal}</div>
                <div style={{ color: "rgba(255,255,255,0.4)", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>Confidence: {pred.confidence}%</div>
                <div style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, marginTop: 2 }}>Momentum Model</div>
              </div>
            </div>

            {/* Factor bars */}
            <div style={{ display: "flex", flexDirection: "column", gap: 5 }}>
              {pred.factors.map(f => (
                <div key={f.name}>
                  <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 2 }}>
                    <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{f.name}</span>
                    <span style={{ color: "#fff", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{f.val}</span>
                  </div>
                  <div style={{ height: 4, background: "rgba(255,255,255,0.06)", borderRadius: 2, overflow: "hidden" }}>
                    <div style={{ width: `${Math.max(0, Math.min(100, f.score))}%`, height: "100%", background: f.score >= 60 ? "#00D4AA" : f.score <= 40 ? "#FF6B6B" : "#F59E0B", borderRadius: 2, transition: "width 0.4s" }} />
                  </div>
                </div>
              ))}
            </div>

            <div style={{ background: "rgba(139,92,246,0.08)", border: "1px solid rgba(139,92,246,0.15)", borderRadius: 6, padding: "6px 8px", color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>
              ⚠ Momentum model only. Not financial advice.
            </div>
          </div>
        ) : (
          <div>
            <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: "12px 0", fontFamily: "'Space Mono',monospace" }}>Enter symbol for AI signal</div>
            {topPicks.length > 0 && (
              <div>
                <div style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace", marginBottom: 6 }}>TOP BUY SIGNALS</div>
                {topPicks.map(p => (
                  <div key={p.tradingCode} onClick={() => setSymbol(p.tradingCode)}
                    style={{ display: "flex", justifyContent: "space-between", padding: "5px 0", borderBottom: "1px solid rgba(255,255,255,0.04)", cursor: "pointer" }}>
                    <span style={{ color: "#00D4AA", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>{p.tradingCode}</span>
                    <span style={{ color: "rgba(255,255,255,0.5)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>Score: {p.sig.score}</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
