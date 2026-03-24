// @ts-nocheck
// src/components/widgets/TimeAndSalesWidget.tsx
// Day 61 — Time & Sales: chronological trade log, Buy/Sell pressure indicators, symbol filter
// Matches XFL reference widget #13. SignalR live trades, demo fallback, auto-scroll.

import { useState, useEffect, useRef, useMemo } from "react"
import { useMarketData } from "@/hooks/useMarketData"
import { subscribeMarket } from "@/hooks/useSignalR"
import { BuySellConsoleEvents } from "@/components/trading/BuySellConsole"

const mono = "'JetBrains Mono', monospace"

// ─── Demo trade data ──────────────────────────────────────────────────────────
function makeDemoTrades() {
  const syms = ["GP","BATBC","BRACBANK","SQURPHARMA","BERGERPBL","ISLAMIBANK","RENATA","DUTCHBANGL","CITYBANK","NBL"]
  const trades = []
  const now = Date.now()
  for (let i = 0; i < 60; i++) {
    const sym = syms[Math.floor(Math.random() * syms.length)]
    const price = 50 + Math.random() * 1200
    const qty = (Math.floor(Math.random() * 20) + 1) * 100
    const side = Math.random() > 0.5 ? "B" : "S"
    trades.push({
      id: i,
      time: new Date(now - i * 4000).toLocaleTimeString("en-BD", { hour12: false }),
      symbol: sym,
      exchange: Math.random() > 0.4 ? "DSE" : "CSE",
      price: parseFloat(price.toFixed(2)),
      qty,
      value: parseFloat((price * qty / 1e6).toFixed(3)),
      side,
    })
  }
  return trades
}

const DEMO_TRADES = makeDemoTrades()

export function TimeAndSalesWidget({ onSymbolClick }: { onSymbolClick?: (sym: string) => void }) {
  const { ticksArray, connected } = useMarketData()
  const [trades, setTrades] = useState<any[]>(DEMO_TRADES)
  const [symFilter, setSymFilter] = useState("")
  const [exch, setExch] = useState<"All" | "DSE" | "CSE">("All")
  const [sideFilter, setSideFilter] = useState<"All" | "B" | "S">("All")
  const [autoScroll, setAutoScroll] = useState(true)
  const [paused, setPaused] = useState(false)
  const listRef = useRef<HTMLDivElement>(null)
  const pausedRef = useRef(false)

  pausedRef.current = paused

  // Subscribe to SignalR TradeExecuted events
  useEffect(() => {
    return subscribeMarket("TradeExecuted", (data: any) => {
      if (pausedRef.current) return
      const trade = {
        id: Date.now() + Math.random(),
        time: new Date().toLocaleTimeString("en-BD", { hour12: false }),
        symbol:   data.tradingCode ?? data.symbol ?? "—",
        exchange: data.exchange ?? "DSE",
        price:    data.price ?? data.executionPrice ?? 0,
        qty:      data.quantity ?? data.qty ?? 0,
        value:    ((data.price ?? 0) * (data.quantity ?? 0) / 1e6).toFixed(3),
        side:     data.side ?? (Math.random() > 0.5 ? "B" : "S"),
      }
      setTrades(prev => [trade, ...prev].slice(0, 500))
    })
  }, [])

  // Simulate live trades from ticks when no SignalR trade events
  useEffect(() => {
    if (connected || paused) return
    const interval = setInterval(() => {
      const stocks = ticksArray.length > 0 ? ticksArray : []
      if (stocks.length === 0) return
      const s = stocks[Math.floor(Math.random() * stocks.length)]
      if (!s) return
      const qty = (Math.floor(Math.random() * 10) + 1) * 100
      setTrades(prev => [{
        id: Date.now() + Math.random(),
        time: new Date().toLocaleTimeString("en-BD", { hour12: false }),
        symbol: s.tradingCode,
        exchange: s.exchange ?? "DSE",
        price: s.lastPrice ?? 0,
        qty,
        value: parseFloat(((s.lastPrice ?? 0) * qty / 1e6).toFixed(3)),
        side: Math.random() > 0.5 ? "B" : "S",
      }, ...prev].slice(0, 500)])
    }, 2500)
    return () => clearInterval(interval)
  }, [connected, ticksArray, paused])

  // Auto scroll to top (newest)
  useEffect(() => {
    if (autoScroll && listRef.current) listRef.current.scrollTop = 0
  }, [trades, autoScroll])

  const filtered = useMemo(() => {
    let t = trades
    if (symFilter)       t = t.filter(x => x.symbol.includes(symFilter.toUpperCase()))
    if (exch !== "All")  t = t.filter(x => x.exchange === exch)
    if (sideFilter !== "All") t = t.filter(x => x.side === sideFilter)
    return t
  }, [trades, symFilter, exch, sideFilter])

  // Buy/Sell pressure from filtered trades
  const pressure = useMemo(() => {
    const total = filtered.length
    if (total === 0) return { buy: 50, sell: 50 }
    const buy = filtered.filter(t => t.side === "B").length
    return { buy: Math.round((buy / total) * 100), sell: Math.round(((total - buy) / total) * 100) }
  }, [filtered])

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Toolbar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 5, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)", flexWrap: "wrap" }}>
        {/* Symbol filter */}
        <input
          value={symFilter}
          onChange={e => setSymFilter(e.target.value)}
          placeholder="Symbol…"
          style={{ width: 72, background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "3px 6px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono }}
          onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
          onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
        />
        {/* Exchange tabs */}
        {(["All","DSE","CSE"] as const).map(e => (
          <button key={e} onClick={() => setExch(e)} style={{
            padding: "2px 7px", fontSize: 9, fontFamily: mono, cursor: "pointer", borderRadius: 4,
            border: `1px solid ${exch === e ? "var(--t-accent)" : "var(--t-border)"}`,
            background: exch === e ? "var(--t-accent)" : "transparent",
            color: exch === e ? "#000" : "var(--t-text3)", fontWeight: exch === e ? 700 : 400,
          }}>{e}</button>
        ))}
        {/* Side filter */}
        {([["All","ALL"],["B","BUY"],["S","SELL"]] as const).map(([val,label]) => (
          <button key={val} onClick={() => setSideFilter(val)} style={{
            padding: "2px 7px", fontSize: 9, fontFamily: mono, cursor: "pointer", borderRadius: 4,
            border: `1px solid ${sideFilter === val ? (val === "B" ? "var(--t-buy)" : val === "S" ? "var(--t-sell)" : "var(--t-accent)") : "var(--t-border)"}`,
            background: sideFilter === val ? (val === "B" ? "rgba(0,212,170,0.15)" : val === "S" ? "rgba(255,107,107,0.15)" : "var(--t-accent)") : "transparent",
            color: sideFilter === val ? (val === "B" ? "var(--t-buy)" : val === "S" ? "var(--t-sell)" : "#000") : "var(--t-text3)",
            fontWeight: sideFilter === val ? 700 : 400,
          }}>{label}</button>
        ))}
        <div style={{ flex: 1 }} />
        {/* Pause / auto-scroll */}
        <button onClick={() => setPaused(p => !p)} title={paused ? "Resume" : "Pause"} style={{
          padding: "2px 7px", fontSize: 9, fontFamily: mono, cursor: "pointer", borderRadius: 4,
          border: `1px solid ${paused ? "var(--t-sell)" : "var(--t-border)"}`,
          background: paused ? "rgba(255,107,107,0.1)" : "transparent",
          color: paused ? "var(--t-sell)" : "var(--t-text3)",
        }}>{paused ? "▶ Resume" : "⏸ Pause"}</button>
        <span style={{ color: connected ? "var(--t-buy)" : "var(--t-sell)", fontSize: 9, fontFamily: mono }}>
          {connected ? "● LIVE" : "○ DEMO"}
        </span>
      </div>

      {/* ── Pressure bar ── */}
      <div style={{ padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 3 }}>
          <span style={{ fontSize: 9, color: "var(--t-buy)", fontFamily: mono, fontWeight: 700 }}>BUY {pressure.buy}%</span>
          <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>{filtered.length} trades</span>
          <span style={{ fontSize: 9, color: "var(--t-sell)", fontFamily: mono, fontWeight: 700 }}>SELL {pressure.sell}%</span>
        </div>
        <div style={{ display: "flex", height: 5, borderRadius: 3, overflow: "hidden" }}>
          <div style={{ width: `${pressure.buy}%`, background: "var(--t-buy)", transition: "width 0.4s ease", borderRadius: "3px 0 0 3px" }} />
          <div style={{ flex: 1, background: "var(--t-sell)", borderRadius: "0 3px 3px 0" }} />
        </div>
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "flex", background: "var(--t-panel)", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        {[["TIME","52px"],["SYM","72px"],["EXCH","40px"],["SIDE","36px"],["PRICE","80px"],["QTY","64px"],["VAL(mn)","68px"]].map(([h,w]) => (
          <div key={h} style={{ width: w, minWidth: w, padding: "4px 6px" }}>
            <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>{h}</span>
          </div>
        ))}
      </div>

      {/* ── Trade rows ── */}
      <div ref={listRef} style={{ flex: 1, overflowY: "auto" }}
        onScroll={e => {
          const el = e.currentTarget
          setAutoScroll(el.scrollTop < 40)
        }}
      >
        {filtered.slice(0, 200).map((t, i) => {
          const isBuy = t.side === "B"
          return (
            <div key={t.id} style={{
              display: "flex", alignItems: "center",
              borderBottom: "1px solid var(--t-border)",
              background: i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
              transition: "background 0.08s",
            }}
              onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
              onMouseLeave={e => e.currentTarget.style.background = i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)"}
            >
              {/* Time */}
              <div style={{ width: "52px", minWidth: "52px", padding: "4px 6px" }}>
                <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>{t.time}</span>
              </div>
              {/* Symbol */}
              <div style={{ width: "72px", minWidth: "72px", padding: "4px 6px" }}>
                <span onClick={() => onSymbolClick?.(t.symbol)}
                  style={{ fontSize: 10, fontWeight: 700, color: "var(--t-accent)", fontFamily: mono, cursor: "pointer" }}
                  onContextMenu={e => { e.preventDefault(); BuySellConsoleEvents.open("BUY", t.symbol) }}
                >{t.symbol}</span>
              </div>
              {/* Exchange */}
              <div style={{ width: "40px", minWidth: "40px", padding: "4px 6px" }}>
                <span style={{ fontSize: 9, color: t.exchange === "DSE" ? "#60a5fa" : "#a78bfa", fontFamily: mono }}>{t.exchange}</span>
              </div>
              {/* Side */}
              <div style={{ width: "36px", minWidth: "36px", padding: "4px 6px" }}>
                <span style={{
                  fontSize: 9, fontWeight: 700, fontFamily: mono,
                  color: isBuy ? "var(--t-buy)" : "var(--t-sell)",
                  background: isBuy ? "rgba(0,212,170,0.1)" : "rgba(255,107,107,0.1)",
                  padding: "1px 4px", borderRadius: 3,
                }}>{isBuy ? "B" : "S"}</span>
              </div>
              {/* Price */}
              <div style={{ width: "80px", minWidth: "80px", padding: "4px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, fontWeight: 700, color: isBuy ? "var(--t-buy)" : "var(--t-sell)", fontFamily: mono }}>
                  ৳{t.price.toFixed(2)}
                </span>
              </div>
              {/* Qty */}
              <div style={{ width: "64px", minWidth: "64px", padding: "4px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{t.qty.toLocaleString()}</span>
              </div>
              {/* Value */}
              <div style={{ width: "68px", minWidth: "68px", padding: "4px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>{t.value}</span>
              </div>
            </div>
          )
        })}
      </div>

      {/* ── Footer ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", padding: "3px 8px", display: "flex", justifyContent: "space-between", flexShrink: 0, background: "var(--t-panel)" }}>
        <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
          Showing {Math.min(filtered.length, 200)} of {filtered.length}
        </span>
        <button onClick={() => setTrades(DEMO_TRADES)} style={{
          fontSize: 9, color: "var(--t-text3)", background: "none", border: "none", cursor: "pointer", fontFamily: mono,
        }}>↺ reset</button>
      </div>
    </div>
  )
}
