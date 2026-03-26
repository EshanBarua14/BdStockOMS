// @ts-nocheck
import { useState, useEffect, useCallback } from "react"
import { apiClient } from "@/api/client"
import { subscribeMarket } from "@/hooks/useSignalR"
import { useMarketData } from "@/hooks/useMarketData"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

export function MarketDepthWidget({ linkedSymbol, onSymbolClick, colorGroup }: { linkedSymbol?: string; onSymbolClick?: (c: string) => void; colorGroup?: string | null }) {
  const { stocks: _s } = useMarketData()
  const stocks = _s ?? []
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)
  const [symbol, setSymbol] = useState(linkedSymbol ?? "")
  const [depth,  setDepth]  = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => { if (_linked) setSymbol(_linked); else if (linkedSymbol) setSymbol(linkedSymbol) }, [_linked, linkedSymbol])

  // REST load on symbol change
  const load = useCallback(async (sym) => {
    if (!sym) return
    setLoading(true)
    try {
      const r = await apiClient.get(`/api/marketdepth/${sym}`)
      setDepth(r.data)
    } catch {
      // Fallback: generate from last price
      const s = stocks.find(x => x.tradingCode === sym)
      if (s) {
        const p    = s.lastTradePrice
        const tick = Math.max(0.01, Math.round(p * 0.001 * 100) / 100)
        const bp   = (s.buyPressure ?? 50) / 100
        setDepth({
          lastPrice: p, spread: tick * 2,
          bids: Array.from({length:10},(_,i)=>({ price:+(p-(i+1)*tick).toFixed(2), qty:Math.round(3000*bp/(i*0.4+1)), orders:Math.ceil(Math.random()*15+1) })),
          asks: Array.from({length:10},(_,i)=>({ price:+(p+(i+1)*tick).toFixed(2), qty:Math.round(3000*(1-bp)/(i*0.4+1)), orders:Math.ceil(Math.random()*15+1) })),
        })
      }
    }
    setLoading(false)
  }, [stocks])

  useEffect(() => { load(symbol) }, [symbol])

  // Live SignalR depth updates
  useEffect(() => {
    return subscribeMarket("DepthUpdate", (d) => {
      if (d.tradingCode === symbol) setDepth(d)
    })
  }, [symbol])

  const stock = stocks.find(s => s.tradingCode === symbol)
  const maxBidQty = Math.max(...(depth?.bids ?? []).map(b => b.qty), 1)
  const maxAskQty = Math.max(...(depth?.asks ?? []).map(a => a.qty), 1)

  return (
    <div style={{ height:"100%", display:"flex", flexDirection:"column", background:"var(--t-surface)", overflow:"hidden" }}>
      <div style={{ padding:"5px 8px", borderBottom:"1px solid rgba(255,255,255,0.06)", display:"flex", gap:8, alignItems:"center", flexShrink:0 }}>
        <input value={symbol} onChange={e=>{setSymbol(e.target.value.toUpperCase())}} placeholder="Symbol…"
          style={{ width:80, background:"rgba(255,255,255,0.04)", border:"1px solid rgba(255,255,255,0.08)", borderRadius:5, padding:"4px 8px", color:"#fff", fontSize:11, outline:"none", fontFamily:"'Space Mono',monospace" }} />
        {stock && <span style={{ color:"#fff", fontSize:12, fontFamily:"'Space Mono',monospace", fontWeight:700 }}>৳{stock.lastTradePrice?.toFixed(2)}</span>}
        {depth && <span style={{ color:"rgba(255,255,255,0.3)", fontSize:10, fontFamily:"'Space Mono',monospace", marginLeft:"auto" }}>Spread: ৳{depth.spread?.toFixed(2)}</span>}
        <span style={{ color:"#00D4AA", fontSize:9, fontFamily:"'Space Mono',monospace" }}>● LIVE</span>
      </div>

      {/* Column headers */}
      <div style={{ display:"grid", gridTemplateColumns:"1fr 1fr 1fr 1fr 1fr 1fr", padding:"3px 8px", borderBottom:"1px solid rgba(255,255,255,0.04)", flexShrink:0 }}>
        {["Orders","Qty","Bid","Ask","Qty","Orders"].map((h,i) => (
          <span key={i} style={{ color:"rgba(255,255,255,0.25)", fontSize:9, fontFamily:"'Space Mono',monospace", textAlign: i<3 ? "left":"right" }}>{h}</span>
        ))}
      </div>

      <div style={{ flex:1, overflowY:"auto" }}>
        {loading && <div style={{ padding:16, color:"rgba(255,255,255,0.2)", fontSize:11, textAlign:"center", fontFamily:"'Space Mono',monospace" }}>Loading…</div>}
        {!loading && !depth && <div style={{ padding:16, color:"rgba(255,255,255,0.2)", fontSize:11, textAlign:"center", fontFamily:"'Space Mono',monospace" }}>Enter a symbol</div>}
        {!loading && depth && (depth.bids ?? []).map((bid, i) => {
          const ask = depth.asks?.[i]
          return (
            <div key={i} style={{ display:"grid", gridTemplateColumns:"1fr 1fr 1fr 1fr 1fr 1fr", padding:"3px 8px", borderBottom:"1px solid rgba(255,255,255,0.02)", position:"relative" }}>
              {/* Bid volume bar */}
              <div style={{ position:"absolute", left:0, top:0, height:"100%", width:`${(bid.qty/maxBidQty)*45}%`, background:"rgba(0,212,170,0.07)", pointerEvents:"none" }} />
              {/* Ask volume bar */}
              {ask && <div style={{ position:"absolute", right:0, top:0, height:"100%", width:`${(ask.qty/maxAskQty)*45}%`, background:"rgba(255,107,107,0.07)", pointerEvents:"none" }} />}
              <span style={{ color:"rgba(255,255,255,0.35)", fontSize:10, fontFamily:"'Space Mono',monospace", zIndex:1 }}>{bid.orders}</span>
              <span style={{ color:"rgba(0,212,170,0.8)", fontSize:10, fontFamily:"'Space Mono',monospace", zIndex:1 }}>{bid.qty?.toLocaleString()}</span>
              <span style={{ color:"#00D4AA", fontSize:10, fontFamily:"'Space Mono',monospace", fontWeight:700, zIndex:1 }}>{bid.price?.toFixed(2)}</span>
              {ask ? <>
                <span style={{ color:"#FF6B6B", fontSize:10, fontFamily:"'Space Mono',monospace", fontWeight:700, textAlign:"right", zIndex:1 }}>{ask.price?.toFixed(2)}</span>
                <span style={{ color:"rgba(255,107,107,0.8)", fontSize:10, fontFamily:"'Space Mono',monospace", textAlign:"right", zIndex:1 }}>{ask.qty?.toLocaleString()}</span>
                <span style={{ color:"rgba(255,255,255,0.35)", fontSize:10, fontFamily:"'Space Mono',monospace", textAlign:"right", zIndex:1 }}>{ask.orders}</span>
              </> : <><span/><span/><span/></>}
            </div>
          )
        })}
      </div>

      {depth && (
        <div style={{ borderTop:"1px solid rgba(255,255,255,0.05)", padding:"4px 8px", display:"flex", justifyContent:"space-between", flexShrink:0 }}>
          <span style={{ color:"#00D4AA", fontSize:10, fontFamily:"'Space Mono',monospace" }}>
            BID {depth.bids?.[0]?.price?.toFixed(2)}
          </span>
          <span style={{ color:"rgba(255,255,255,0.2)", fontSize:10, fontFamily:"'Space Mono',monospace" }}>
            ৳{depth.lastPrice?.toFixed(2)}
          </span>
          <span style={{ color:"#FF6B6B", fontSize:10, fontFamily:"'Space Mono',monospace" }}>
            ASK {depth.asks?.[0]?.price?.toFixed(2)}
          </span>
        </div>
      )}
    </div>
  )
}
