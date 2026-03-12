// @ts-nocheck
import { useState, useEffect } from "react"
import { useMarketData } from "@/hooks/useMarketData"
import { subscribeMarket } from "@/hooks/useSignalR"

const BASE = { dsex:6248.30, dses:1312.45, ds30:2187.60, cseAll:18420.50, cse30:9841.20 }

const INDEX_DEFS = [
  { key:"dsex",   code:"DSEX",   name:"DSE General"   },
  { key:"dses",   code:"DSES",   name:"DSE Shariah"   },
  { key:"ds30",   code:"DS30",   name:"DSE 30"        },
  { key:"cseAll", code:"CSEALL", name:"CSE All Share"  },
  { key:"cse30",  code:"CSE30",  name:"CSE 30"        },
]

export function IndexSummaryWidget() {
  const { stocks: _s, connected } = useMarketData()
  const stocks = _s ?? []
  const [indices, setIndices] = useState(
    INDEX_DEFS.map(d => ({ ...d, value:BASE[d.key], base:BASE[d.key], change:0, changePct:0, up:true }))
  )

  // Real SignalR index updates from backend
  useEffect(() => {
    return subscribeMarket("IndexUpdate", (data: any) => {
      setIndices(prev => prev.map(idx => {
        const val = data[idx.key]
        if (val === undefined) return idx
        const chg    = val - idx.base
        const chgPct = (chg / idx.base) * 100
        return { ...idx, value:val, change:chg, changePct:chgPct, up:chg >= 0 }
      }))
    })
  }, [])

  const advancers = stocks.filter(s => (s.changePercent ?? 0) > 0).length
  const decliners = stocks.filter(s => (s.changePercent ?? 0) < 0).length
  const unchanged = stocks.length - advancers - decliners

  return (
    <div style={{ height:"100%", display:"flex", flexDirection:"column", background:"#0D1320", overflow:"hidden" }}>
      <div style={{ padding:"5px 8px", borderBottom:"1px solid rgba(255,255,255,0.06)", display:"flex", justifyContent:"space-between", alignItems:"center", flexShrink:0 }}>
        <span style={{ color:"rgba(255,255,255,0.5)", fontSize:10, fontFamily:"'Space Mono',monospace" }}>MARKET INDICES</span>
        <span style={{ color: connected ? "#00D4AA" : "#FF6B6B", fontSize:9, fontFamily:"'Space Mono',monospace" }}>{connected ? "● LIVE" : "○ offline"}</span>
      </div>

      <div style={{ flex:1, overflowY:"auto" }}>
        {indices.map(idx => (
          <div key={idx.code} style={{ padding:"10px 12px", borderBottom:"1px solid rgba(255,255,255,0.04)", display:"flex", justifyContent:"space-between", alignItems:"center" }}>
            <div>
              <div style={{ color:"#fff", fontSize:12, fontFamily:"'Space Mono',monospace", fontWeight:700 }}>{idx.code}</div>
              <div style={{ color:"rgba(255,255,255,0.3)", fontSize:10 }}>{idx.name}</div>
            </div>
            <div style={{ textAlign:"right" }}>
              <div style={{ color:"#fff", fontSize:13, fontFamily:"'Space Mono',monospace", fontWeight:700 }}>{idx.value.toFixed(2)}</div>
              <div style={{ color: idx.up ? "#00D4AA" : "#FF6B6B", fontSize:11, fontFamily:"'Space Mono',monospace" }}>
                {idx.up ? "▲" : "▼"} {Math.abs(idx.change).toFixed(2)} ({Math.abs(idx.changePct).toFixed(2)}%)
              </div>
            </div>
          </div>
        ))}
      </div>

      <div style={{ borderTop:"1px solid rgba(255,255,255,0.05)", padding:"6px 10px", flexShrink:0 }}>
        <div style={{ color:"rgba(255,255,255,0.3)", fontSize:9, fontFamily:"'Space Mono',monospace", marginBottom:4 }}>MARKET BREADTH · {stocks.length} stocks</div>
        <div style={{ display:"flex", height:6, borderRadius:3, overflow:"hidden", gap:1 }}>
          <div style={{ flex:advancers, background:"#00D4AA", borderRadius:"3px 0 0 3px" }} />
          <div style={{ flex:unchanged, background:"rgba(255,255,255,0.2)" }} />
          <div style={{ flex:decliners, background:"#FF6B6B", borderRadius:"0 3px 3px 0" }} />
        </div>
        <div style={{ display:"flex", justifyContent:"space-between", marginTop:3 }}>
          <span style={{ color:"#00D4AA", fontSize:9, fontFamily:"'Space Mono',monospace" }}>▲ {advancers}</span>
          <span style={{ color:"rgba(255,255,255,0.3)", fontSize:9, fontFamily:"'Space Mono',monospace" }}>— {unchanged}</span>
          <span style={{ color:"#FF6B6B", fontSize:9, fontFamily:"'Space Mono',monospace" }}>▼ {decliners}</span>
        </div>
      </div>
    </div>
  )
}
