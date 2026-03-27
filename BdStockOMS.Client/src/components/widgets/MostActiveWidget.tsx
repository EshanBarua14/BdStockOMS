// @ts-nocheck
import { useLinkedSymbol } from '@/hooks/useColorGroupSync';
// @ts-nocheck
// src/components/widgets/MostActiveWidget.tsx
// Day 61 — Most Active: parameter-based ranking (Value/Volume/Trades/Change%)
// Matches XFL reference widget #12. Live data from useMarketData, demo fallback.
// Integrates with BuySellConsole on click.

import { useMemo, useState } from "react"
import { useMarketData } from "@/hooks/useMarketData"
import { BuySellConsoleEvents } from "@/components/trading/BuySellConsole"

const mono = "'JetBrains Mono', monospace"

// ─── Demo data (same pool as WatchlistWidget for consistency) ─────────────────
const DEMO_STOCKS = [
  { tradingCode:"GP",         exchange:"DSE", category:"A", lastPrice:380.50,  changePercent:0.61,  volume:18234000, value:6941.2, trades:24310 },
  { tradingCode:"BATBC",      exchange:"DSE", category:"A", lastPrice:615.92,  changePercent:-0.66, volume:4321000,  value:2661.4, trades:8720  },
  { tradingCode:"BERGERPBL",  exchange:"DSE", category:"A", lastPrice:1131.95, changePercent:1.00,  volume:982000,   value:1111.5, trades:3210  },
  { tradingCode:"BRACBANK",   exchange:"DSE", category:"A", lastPrice:48.30,   changePercent:1.68,  volume:32410000, value:1564.4, trades:18760 },
  { tradingCode:"DUTCHBANGL", exchange:"DSE", category:"A", lastPrice:182.40,  changePercent:-0.87, volume:6543000,  value:1193.4, trades:5430  },
  { tradingCode:"SQURPHARMA", exchange:"DSE", category:"A", lastPrice:242.10,  changePercent:1.42,  volume:8765000,  value:2122.7, trades:9870  },
  { tradingCode:"ISLAMIBANK", exchange:"DSE", category:"A", lastPrice:35.60,   changePercent:1.14,  volume:41230000, value:1467.8, trades:22100 },
  { tradingCode:"RENATA",     exchange:"DSE", category:"A", lastPrice:1243.00, changePercent:-0.68, volume:764000,   value:950.2,  trades:2100  },
  { tradingCode:"CITYBANK",   exchange:"CSE", category:"A", lastPrice:28.40,   changePercent:-1.05, volume:23410000, value:665.0,  trades:12870 },
  { tradingCode:"NBL",        exchange:"CSE", category:"A", lastPrice:14.20,   changePercent:0.71,  volume:54320000, value:771.3,  trades:26540 },
  { tradingCode:"MARICO",     exchange:"CSE", category:"A", lastPrice:98.70,   changePercent:1.23,  volume:2345000,  value:231.5,  trades:3210  },
  { tradingCode:"BXPHARMA",   exchange:"CSE", category:"A", lastPrice:67.80,   changePercent:3.19,  volume:4321000,  value:292.9,  trades:4320  },
  { tradingCode:"OLYMPIC",    exchange:"DSE", category:"A", lastPrice:176.50,  changePercent:2.10,  volume:5432000,  value:958.7,  trades:6540  },
  { tradingCode:"BRAC",       exchange:"DSE", category:"A", lastPrice:52.30,   changePercent:-1.42, volume:29870000, value:1562.2, trades:15430 },
  { tradingCode:"ACI",        exchange:"DSE", category:"A", lastPrice:432.10,  changePercent:0.87,  volume:876000,   value:378.5,  trades:1870  },
  { tradingCode:"LHBL",       exchange:"DSE", category:"B", lastPrice:23.40,   changePercent:4.91,  volume:87650000, value:2050.8, trades:34310 },
  { tradingCode:"SAPORTL",    exchange:"CSE", category:"B", lastPrice:16.80,   changePercent:-3.22, volume:12340000, value:207.3,  trades:8760  },
  { tradingCode:"NAVANPPL",   exchange:"DSE", category:"B", lastPrice:8.70,    changePercent:5.45,  volume:54320000, value:472.6,  trades:21200 },
  { tradingCode:"DESCO",      exchange:"DSE", category:"A", lastPrice:87.30,   changePercent:-0.34, volume:3214000,  value:280.6,  trades:2340  },
  { tradingCode:"TITASGAS",   exchange:"DSE", category:"A", lastPrice:43.20,   changePercent:0.47,  volume:18760000, value:810.4,  trades:9870  },
]

type Param = "value" | "volume" | "trades" | "changePercent"

const PARAMS: { key: Param; label: string; icon: string }[] = [
  { key: "value",         label: "Value",   icon: "৳" },
  { key: "volume",        label: "Volume",  icon: "📦" },
  { key: "trades",        label: "Trades",  icon: "🔄" },
  { key: "changePercent", label: "Change%", icon: "%" },
]

function fmtVol(v: number) {
  return v >= 1e6 ? `${(v / 1e6).toFixed(1)}M` : v >= 1e3 ? `${(v / 1e3).toFixed(0)}K` : String(v || "—")
}
function fmtVal(v: number) {
  return v >= 1000 ? `${(v / 1000).toFixed(2)}B` : `${v.toFixed(1)}M`
}

export function MostActiveWidget({ onSymbolClick }: { onSymbolClick?: (sym: string) => void }) {
  const { ticksArray, connected } = useMarketData()
  const [param, setParam]   = useState<Param>("value")
  const [count, setCount]   = useState(10)
  const [exch, setExch]     = useState<"All" | "DSE" | "CSE">("All")

  const ranked = useMemo(() => {
    const base = ticksArray.length > 0 ? ticksArray.map(s => ({
      tradingCode:   s.tradingCode,
      exchange:      s.exchange ?? "",
      category:      s.category ?? "A",
      lastPrice:     s.lastPrice ?? 0,
      changePercent: s.changePercent ?? 0,
      volume:        s.volume ?? 0,
      value:         (s.turnover ?? 0) / 1e6,
      trades:        s.tradeCount ?? 0,
    })) : DEMO_STOCKS

    let filtered = base
    if (exch !== "All") filtered = filtered.filter(s => s.exchange === exch)

    return [...filtered]
      .filter(s => s.volume > 0)
      .sort((a, b) => {
        if (param === "changePercent") return Math.abs(b.changePercent) - Math.abs(a.changePercent)
        return (b[param] as number) - (a[param] as number)
      })
      .slice(0, count)
  }, [ticksArray, param, count, exch])

  // Max value for bar scaling
  const maxVal = useMemo(() => {
    if (ranked.length === 0) return 1
    if (param === "changePercent") return Math.max(...ranked.map(s => Math.abs(s.changePercent)), 1)
    return Math.max(...ranked.map(s => s[param] as number), 1)
  }, [ranked, param])

  const catColor = (cat: string) => {
    const map: Record<string, string> = { A: "#00e676", B: "#ffd740", G: "#60a5fa", N: "#a78bfa", Z: "#ff1744" }
    return map[cat] ?? "var(--t-text3)"
  }

  const renderValue = (s: any) => {
    switch (param) {
      case "value":         return <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 11 }}>{fmtVal(s.value)}</span>
      case "volume":        return <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 11 }}>{fmtVol(s.volume)}</span>
      case "trades":        return <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 11 }}>{fmtVol(s.trades)}</span>
      case "changePercent": {
        const up = s.changePercent >= 0
        return <span style={{ color: up ? "var(--t-buy)" : "var(--t-sell)", fontFamily: mono, fontSize: 11, fontWeight: 700 }}>
          {up ? "▲" : "▼"}{Math.abs(s.changePercent).toFixed(2)}%
        </span>
      }
    }
  }

  const getBarVal = (s: any): number => {
    if (param === "changePercent") return Math.abs(s.changePercent)
    return s[param] as number
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Param tabs ── */}
      <div style={{ display: "flex", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        {PARAMS.map(p => (
          <button key={p.key} onClick={() => setParam(p.key)} style={{
            flex: 1, padding: "7px 0", background: "none", border: "none",
            borderBottom: `2px solid ${param === p.key ? "var(--t-accent)" : "transparent"}`,
            color: param === p.key ? "var(--t-accent)" : "var(--t-text3)",
            fontSize: 10, cursor: "pointer", fontFamily: mono, fontWeight: param === p.key ? 700 : 400,
            transition: "all 0.1s",
          }}>
            <span style={{ fontSize: 11 }}>{p.icon}</span> {p.label}
          </button>
        ))}
      </div>

      {/* ── Toolbar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 5, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {(["All","DSE","CSE"] as const).map(e => (
          <button key={e} onClick={() => setExch(e)} style={{
            padding: "2px 8px", fontSize: 10, fontFamily: mono, cursor: "pointer", borderRadius: 4,
            border: `1px solid ${exch === e ? "var(--t-accent)" : "var(--t-border)"}`,
            background: exch === e ? "var(--t-accent)" : "transparent",
            color: exch === e ? "#000" : "var(--t-text3)", fontWeight: exch === e ? 700 : 400,
          }}>{e}</button>
        ))}
        <div style={{ flex: 1 }} />
        <select value={count} onChange={e => setCount(Number(e.target.value))} style={{
          background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4,
          color: "var(--t-text2)", fontSize: 10, padding: "2px 4px", cursor: "pointer", fontFamily: mono,
        }}>
          {[10, 15, 20].map(n => <option key={n} value={n}>Top {n}</option>)}
        </select>
        <span style={{ color: connected ? "var(--t-buy)" : "var(--t-sell)", fontSize: 9, fontFamily: mono }}>
          {connected ? "● LIVE" : "○ DEMO"}
        </span>
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "flex", background: "var(--t-panel)", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        <div style={{ width: 24, minWidth: 24, padding: "4px 6px" }}>
          <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>#</span>
        </div>
        <div style={{ flex: 1, padding: "4px 6px" }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>SYMBOL</span>
        </div>
        <div style={{ width: 44, padding: "4px 6px" }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>EXCH</span>
        </div>
        <div style={{ width: 80, padding: "4px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-accent)", fontFamily: mono }}>
            {PARAMS.find(p => p.key === param)?.label.toUpperCase()}
          </span>
        </div>
        <div style={{ width: 80, padding: "4px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>LTP</span>
        </div>
      </div>

      {/* ── Ranked rows ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {ranked.map((s, i) => {
          const barW = Math.min(100, (getBarVal(s) / maxVal) * 100)
          const up = s.changePercent >= 0
          return (
            <div key={s.tradingCode}
              onClick={() => onSymbolClick?.(s.tradingCode)}
              onContextMenu={e => { e.preventDefault(); BuySellConsoleEvents.open("BUY", s.tradingCode) }}
              style={{
                display: "flex", alignItems: "center",
                borderBottom: "1px solid var(--t-border)",
                cursor: "pointer", position: "relative", overflow: "hidden",
                transition: "background 0.08s",
              }}
              onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
              onMouseLeave={e => e.currentTarget.style.background = "transparent"}
            >
              {/* Progress bar background */}
              <div style={{
                position: "absolute", left: 0, top: 0, bottom: 0, width: `${barW}%`,
                background: param === "changePercent"
                  ? (up ? "rgba(0,212,170,0.06)" : "rgba(255,107,107,0.06)")
                  : "rgba(var(--t-accent-rgb,99,102,241),0.05)",
                pointerEvents: "none", transition: "width 0.3s ease",
              }} />

              {/* Rank */}
              <div style={{ width: 24, minWidth: 24, padding: "6px 4px 6px 8px" }}>
                <span style={{ fontSize: 10, color: i < 3 ? "var(--t-accent)" : "var(--t-text3)", fontFamily: mono, fontWeight: i < 3 ? 700 : 400 }}>
                  {i + 1}
                </span>
              </div>

              {/* Symbol + company */}
              <div style={{ flex: 1, padding: "6px 6px" }}>
                <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
                  <span style={{ fontSize: 11, fontWeight: 700, color: "var(--t-text1)", fontFamily: mono }}>{s.tradingCode}</span>
                  <span style={{ fontSize: 8, color: catColor(s.category), fontWeight: 700, fontFamily: mono }}>{s.category}</span>
                </div>
                {/* Mini progress bar per row */}
                <div style={{ height: 2, background: "var(--t-border)", borderRadius: 1, marginTop: 3, overflow: "hidden" }}>
                  <div style={{
                    width: `${barW}%`, height: "100%",
                    background: param === "changePercent"
                      ? (up ? "var(--t-buy)" : "var(--t-sell)")
                      : "var(--t-accent)",
                    borderRadius: 1, transition: "width 0.3s ease",
                  }} />
                </div>
              </div>

              {/* Exchange */}
              <div style={{ width: 44, padding: "6px 6px", textAlign: "center" }}>
                <span style={{ fontSize: 9, color: s.exchange === "DSE" ? "#60a5fa" : "#a78bfa", fontFamily: mono }}>{s.exchange}</span>
              </div>

              {/* Primary metric */}
              <div style={{ width: 80, padding: "6px 6px", textAlign: "right" }}>
                {renderValue(s)}
              </div>

              {/* LTP */}
              <div style={{ width: 80, padding: "6px 8px 6px 0", textAlign: "right" }}>
                <div style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>৳{s.lastPrice.toFixed(2)}</div>
                <div style={{ fontSize: 9, color: up ? "var(--t-buy)" : "var(--t-sell)", fontFamily: mono }}>
                  {up ? "▲" : "▼"}{Math.abs(s.changePercent).toFixed(2)}%
                </div>
              </div>
            </div>
          )
        })}

        {ranked.length === 0 && (
          <div style={{ textAlign: "center", color: "var(--t-text3)", fontSize: 11, padding: "32px 0", fontFamily: mono }}>
            No data available
          </div>
        )}
      </div>

      {/* ── Footer ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", padding: "3px 8px", display: "flex", justifyContent: "space-between", flexShrink: 0, background: "var(--t-panel)" }}>
        <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
          Ranked by {PARAMS.find(p => p.key === param)?.label} · {ranked.length} stocks
        </span>
        <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>right-click to trade</span>
      </div>
    </div>
  )
}
