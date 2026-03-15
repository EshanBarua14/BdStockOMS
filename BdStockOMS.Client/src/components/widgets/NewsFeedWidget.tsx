const DEMO_NEWS = [
  { id:1, title:"DSE index rises 1.2% on banking sector gains", category:"Market", source:"Daily Star", publishedAt: new Date(Date.now()-1800000).toISOString(), summary:"The Dhaka Stock Exchange rose sharply today led by banking stocks." },
  { id:2, title:"BATBC reports 15% profit growth in Q3 2026", category:"Corporate", source:"Financial Express", publishedAt: new Date(Date.now()-3600000).toISOString(), summary:"British American Tobacco Bangladesh posted strong quarterly results." },
  { id:3, title:"Bangladesh Bank cuts policy rate by 25bps", category:"Economy", source:"Prothom Alo", publishedAt: new Date(Date.now()-7200000).toISOString(), summary:"Central bank eases monetary policy to support growth." },
  { id:4, title:"GP subscriber base crosses 86 million", category:"Corporate", source:"BD News24", publishedAt: new Date(Date.now()-10800000).toISOString(), summary:"Grameenphone reports record subscriber numbers for Q1." },
  { id:5, title:"CSE launches new SME board listing requirements", category:"Regulation", source:"BSS", publishedAt: new Date(Date.now()-14400000).toISOString(), summary:"Chittagong Stock Exchange updates SME listing criteria." },
  { id:6, title:"BSEC approves 3 new mutual fund listings", category:"Regulation", source:"UNB", publishedAt: new Date(Date.now()-18000000).toISOString(), summary:"Securities regulator greenlights new fund products." },
]
// @ts-nocheck
import { useState, useEffect } from "react"
import { apiClient } from "@/api/client"
import { subscribeMarket } from "@/hooks/useSignalR"

const IMP_COLORS = { high:"#FF6B6B", medium:"#F59E0B", low:"rgba(255,255,255,0.3)" }

export function NewsFeedWidget({ onSymbolClick }: any) {
  const [news, setNews]     = useState<any[]>([])
  const [filter, setFilter] = useState("All")
  const [search, setSearch] = useState("")

  // Load initial news from REST
  useEffect(() => {
    apiClient.get("/news?count=20").then(r => {
      setNews(r.data?.length ? r.data : DEMO_NEWS)
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
              <span style={{ width:5, height:5, borderRadius:"50%", background:(IMP_COLORS as any)[n.importance]??"#fff", flexShrink:0, marginTop:5 }} />
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
