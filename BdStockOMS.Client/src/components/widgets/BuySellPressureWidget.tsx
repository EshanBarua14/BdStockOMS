// @ts-nocheck
import { useState, useEffect } from "react"
import { useMarketData } from "@/hooks/useMarketData"
import { subscribeMarket } from "@/hooks/useSignalR"

export function BuySellPressureWidget({ linkedSymbol }) {
  const { stocks } = useMarketData()
  const [symbol, setSymbol] = useState(linkedSymbol ?? "")
  const [history, setHistory] = useState([])
  const [live, setLive] = useState(null)  // { buyPressure, sellPressure }

  useEffect(() => { if (linkedSymbol) setSymbol(linkedSymbol) }, [linkedSymbol])

  // Seed from stock data
  const stock = stocks.find(s => s.tradingCode === symbol)
  useEffect(() => {
    if (!stock) return
    const bp = stock.buyPressure ?? (50 + (stock.changePercent ?? 0) * 3)
    setLive({ buyPressure: Math.min(90, Math.max(10, bp)), sellPressure: Math.min(90, Math.max(10, 100 - bp)) })
  }, [stock?.tradingCode, stock?.lastTradePrice])

  // Real SignalR pressure updates
  useEffect(() => {
    return subscribeMarket("PressureUpdate", (list: any[]) => {
      const entry = list.find(p => p.tradingCode === symbol || stocks.find(s => s.id === p.stockId)?.tradingCode === symbol)
      if (!entry) return
      const bp = entry.buyPressure
      setLive({ buyPressure: bp, sellPressure: entry.sellPressure ?? 100 - bp })
      setHistory(h => [...h.slice(-29), { buy: bp, sell: entry.sellPressure ?? 100 - bp, t: Date.now() }])
    })
  }, [symbol, stocks])

  // Also update from BulkPriceUpdate which includes buyPressure field
  useEffect(() => {
    return subscribeMarket("BulkPriceUpdate", (updates: any[]) => {
      const u = updates.find(x => x.tradingCode === symbol)
      if (!u?.buyPressure) return
      const bp = u.buyPressure
      setLive({ buyPressure: bp, sellPressure: u.sellPressure ?? 100 - bp })
      setHistory(h => [...h.slice(-29), { buy: bp, sell: u.sellPressure ?? 100 - bp, t: Date.now() }])
    })
  }, [symbol])

  const latest = live ?? { buyPressure: 50, sellPressure: 50 }
  const buyPct  = latest.buyPressure.toFixed(1)
  const sellPct = latest.sellPressure.toFixed(1)
  const maxVal  = Math.max(...history.map(h => Math.max(h.buy, h.sell)), 100)
  const barH    = 80

  return (
    <div style={{ height:"100%", display:"flex", flexDirection:"column", background:"#0D1320", overflow:"hidden" }}>
      <div style={{ padding:"5px 8px", borderBottom:"1px solid rgba(255,255,255,0.06)", display:"flex", gap:8, alignItems:"center", flexShrink:0 }}>
        <input value={symbol} onChange={e=>setSymbol(e.target.value.toUpperCase())} placeholder="Symbol…"
          style={{ width:80, background:"rgba(255,255,255,0.04)", border:"1px solid rgba(255,255,255,0.08)", borderRadius:5, padding:"4px 8px", color:"#fff", fontSize:11, outline:"none", fontFamily:"'Space Mono',monospace" }} />
        <span style={{ color:"rgba(255,255,255,0.3)", fontSize:10, fontFamily:"'Space Mono',monospace", marginLeft:"auto" }}>BUY/SELL PRESSURE</span>
        <span style={{ color:"#00D4AA", fontSize:9, fontFamily:"'Space Mono',monospace" }}>● LIVE</span>
      </div>

      <div style={{ padding:"12px 16px", display:"flex", flexDirection:"column", gap:8, flexShrink:0 }}>
        <div style={{ display:"flex", justifyContent:"space-between", alignItems:"center" }}>
          <span style={{ color:"#00D4AA", fontSize:18, fontFamily:"'Space Mono',monospace", fontWeight:700 }}>{buyPct}%</span>
          <span style={{ color:"rgba(255,255,255,0.3)", fontSize:11, fontFamily:"'Space Mono',monospace" }}>BUY vs SELL</span>
          <span style={{ color:"#FF6B6B", fontSize:18, fontFamily:"'Space Mono',monospace", fontWeight:700 }}>{sellPct}%</span>
        </div>
        <div style={{ height:10, borderRadius:5, overflow:"hidden", background:"rgba(255,255,255,0.05)", display:"flex" }}>
          <div style={{ width:`${latest.buyPressure}%`, background:"linear-gradient(90deg,#00B894,#00D4AA)", transition:"width 0.6s ease", borderRadius:"5px 0 0 5px" }} />
          <div style={{ flex:1, background:"linear-gradient(90deg,#FF6B6B,#E84393)", borderRadius:"0 5px 5px 0" }} />
        </div>
        <div style={{ display:"flex", justifyContent:"space-between" }}>
          <span style={{ color:"#00D4AA", fontSize:10, fontFamily:"'Space Mono',monospace" }}>● BUY</span>
          <span style={{ color:"#FF6B6B", fontSize:10, fontFamily:"'Space Mono',monospace" }}>SELL ●</span>
        </div>
      </div>

      <div style={{ flex:1, padding:"4px 8px", display:"flex", alignItems:"flex-end", gap:2, overflow:"hidden" }}>
        {history.map((h,i) => (
          <div key={i} style={{ flex:1, display:"flex", flexDirection:"column", gap:1, alignItems:"stretch" }}>
            <div style={{ height:`${(h.buy/maxVal)*barH}px`, background:`rgba(0,212,170,${0.3+(h.buy/100)*0.5})`, borderRadius:"2px 2px 0 0", transition:"height 0.3s" }} />
            <div style={{ height:`${(h.sell/maxVal)*barH*0.6}px`, background:`rgba(255,107,107,${0.3+(h.sell/100)*0.4})`, borderRadius:"0 0 2px 2px" }} />
          </div>
        ))}
        {history.length === 0 && stock && (
          <div style={{ margin:"auto", color:"rgba(255,255,255,0.2)", fontSize:11, fontFamily:"'Space Mono',monospace" }}>Waiting for live data…</div>
        )}
        {!stock && <span style={{ color:"rgba(255,255,255,0.2)", fontSize:11, margin:"auto", fontFamily:"'Space Mono',monospace" }}>Select a symbol</span>}
      </div>

      <div style={{ padding:"5px 8px", borderTop:"1px solid rgba(255,255,255,0.05)", flexShrink:0 }}>
        <span style={{ fontSize:10, fontFamily:"'Space Mono',monospace", color: latest.buyPressure > 60 ? "#00D4AA" : latest.buyPressure < 40 ? "#FF6B6B" : "rgba(255,255,255,0.4)" }}>
          {latest.buyPressure > 65 ? "● Strong buying pressure" : latest.buyPressure < 35 ? "● Strong selling pressure" : latest.buyPressure > 55 ? "● Mild buying" : latest.buyPressure < 45 ? "● Mild selling" : "● Balanced market"}
        </span>
      </div>
    </div>
  )
}
