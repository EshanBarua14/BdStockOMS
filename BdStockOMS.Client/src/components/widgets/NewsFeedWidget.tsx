// @ts-nocheck
import { useState, useEffect } from "react"
import { apiClient } from "@/api/client"
import { subscribeMarket } from "@/hooks/useSignalR"

const IMP_COLORS = { high:"#FF6B6B", medium:"#F59E0B", low:"rgba(255,255,255,0.3)" }

export function NewsFeedWidget({ onSymbolClick }) {
  const [news, setNews]     = useState([])
  const [filter, setFilter] = useState("All")
  const [search, setSearch] = useState("")

  // Load initial news from REST
  useEffect(() => {
    apiClient.get("/News?count=20").then(r => {
      setNews(r.data ?? [])
    }).catch(() => {})
  }, [])

  // Live news from SignalR
  useEffect(() => {
    return subscribeMarket("NewsUpdate", (item: any) => {
      setNews(prev => [{ ...item, isNew: true }, ...prev.slice(0, 49)])
      // Clear "new" flag after 5s
      setTimeout(() => {
        setNews(prev => prev.map(n => n.id === item.id ? { ...n, isNew: false } : n))
      }, 5000)
    })
  }, [])

  const filtered = news.filter(n => {
    if (filter !== "All" && n.importance !== filter.toLowerCase()) return false
    if (search && !n.title?.toLowerCase().includes(search.toLowerCase()) && !n.tag?.toLowerCase().includes(search.toLowerCase())) return false
    return true
  })

  return (
    <div style={{ height:"100%", display:"flex", flexDirection:"column", background:"#0D1320", overflow:"hidden" }}>
      <div style={{ padding:"5px 8px", borderBottom:"1px solid rgba(255,255,255,0.06)", display:"flex", gap:6, alignItems:"center", flexShrink:0 }}>
        <input value={search} onChange={e=>setSearch(e.target.value)} placeholder="Search news…"
          style={{ flex:1, background:"rgba(255,255,255,0.04)", border:"1px solid rgba(255,255,255,0.08)", borderRadius:5, padding:"4px 8px", color:"#fff", fontSize:11, outline:"none" }} />
        <span style={{ color:"#00D4AA", fontSize:9, fontFamily:"'Space Mono',monospace" }}>● LIVE</span>
      </div>
      <div style={{ padding:"4px 8px", borderBottom:"1px solid rgba(255,255,255,0.04)", display:"flex", gap:4, flexShrink:0 }}>
        {["All","High","Medium","Low"].map(f => (
          <button key={f} onClick={()=>setFilter(f)} style={{ padding:"3px 7px", background:filter===f?"rgba(255,255,255,0.08)":"none", border:`1px solid ${filter===f?"rgba(255,255,255,0.15)":"transparent"}`, borderRadius:4, color:filter===f?"#fff":"rgba(255,255,255,0.3)", fontSize:9, cursor:"pointer", fontFamily:"'Space Mono',monospace" }}>{f}</button>
        ))}
      </div>

      <div style={{ flex:1, overflowY:"auto" }}>
        {filtered.length === 0
          ? <div style={{ textAlign:"center", color:"rgba(255,255,255,0.2)", fontSize:11, padding:16, fontFamily:"'Space Mono',monospace" }}>No news</div>
          : filtered.map((n,i) => (
            <div key={n.id ?? i} onClick={()=>onSymbolClick?.(n.tag)}
              style={{ padding:"8px 10px", borderBottom:"1px solid rgba(255,255,255,0.03)", cursor:"pointer", background: n.isNew ? "rgba(0,212,170,0.04)" : "transparent", transition:"background 0.5s", display:"flex", gap:8, alignItems:"flex-start" }}>
              <span style={{ width:5, height:5, borderRadius:"50%", background:IMP_COLORS[n.importance]??"#fff", flexShrink:0, marginTop:5 }} />
              <div style={{ flex:1 }}>
                {n.isNew && <span style={{ color:"#00D4AA", fontSize:9, fontFamily:"'Space Mono',monospace", marginRight:4 }}>NEW</span>}
                <span style={{ color:"#fff", fontSize:11, lineHeight:1.4 }}>{n.title}</span>
                <div style={{ display:"flex", gap:6, marginTop:4, alignItems:"center" }}>
                  <span style={{ background:"rgba(0,212,170,0.1)", color:"#00D4AA", fontSize:9, padding:"1px 5px", borderRadius:3, fontFamily:"'Space Mono',monospace" }}>{n.tag}</span>
                  <span style={{ color:"rgba(255,255,255,0.2)", fontSize:9, fontFamily:"'Space Mono',monospace" }}>{n.time}</span>
                </div>
              </div>
            </div>
          ))
        }
      </div>
    </div>
  )
}
