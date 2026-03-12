// @ts-nocheck
import { useState, useEffect } from "react"
import { watchlistApi } from "@/api/watchlist"
import { marketApi } from "@/api/market"
import { useMarketData } from "@/hooks/useMarketData"

export function WatchlistWidget() {
  const { stocks } = useMarketData()
  const [lists, setLists]       = useState([])
  const [active, setActive]     = useState(0)
  const [search, setSearch]     = useState("")
  const [results, setResults]   = useState([])
  const [newName, setNewName]   = useState("")
  const [creating, setCreating] = useState(false)

  useEffect(() => {
    watchlistApi.getAll().then(d => { setLists(d ?? []); if (d?.length) setActive(d[0].id) }).catch(() => {})
  }, [])

  const activeList = lists.find(l => l.id === active)

  const handleSearch = async (q) => {
    setSearch(q)
    if (q.length < 2) { setResults([]); return }
    try { const d = await marketApi.searchStocks(q); setResults(d?.slice(0,6) ?? []) } catch {}
  }

  const addStock = async (stockId) => {
    if (!active) return
    try { await watchlistApi.addStock(active, stockId); const d = await watchlistApi.getAll(); setLists(d ?? []) } catch {}
    setSearch(""); setResults([])
  }

  const removeStock = async (stockId) => {
    if (!active) return
    try { await watchlistApi.removeStock(active, stockId); const d = await watchlistApi.getAll(); setLists(d ?? []) } catch {}
  }

  const createList = async () => {
    if (!newName.trim()) return
    try { await watchlistApi.create(newName.trim()); const d = await watchlistApi.getAll(); setLists(d ?? []); setNewName(""); setCreating(false) } catch {}
  }

  const livePrice = (code) => stocks.find(s => s.tradingCode === code)

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "#0D1320", overflow: "hidden" }}>
      {/* Tabs */}
      <div style={{ display: "flex", borderBottom: "1px solid rgba(255,255,255,0.06)", overflowX: "auto", flexShrink: 0 }}>
        {lists.map(l => (
          <button key={l.id} onClick={() => setActive(l.id)} style={{ padding: "7px 12px", background: "none", border: "none", borderBottom: `2px solid ${active === l.id ? "#00D4AA" : "transparent"}`, color: active === l.id ? "#00D4AA" : "rgba(255,255,255,0.4)", fontSize: 11, cursor: "pointer", whiteSpace: "nowrap", fontFamily: "'Space Mono',monospace" }}>{l.name}</button>
        ))}
        {creating
          ? <div style={{ display: "flex", alignItems: "center", gap: 4, padding: "4px 8px" }}>
              <input autoFocus value={newName} onChange={e => setNewName(e.target.value)} onKeyDown={e => e.key === "Enter" && createList()} placeholder="List name" style={{ background: "rgba(255,255,255,0.06)", border: "1px solid rgba(0,212,170,0.3)", borderRadius: 4, padding: "3px 6px", color: "#fff", fontSize: 11, width: 80, outline: "none" }} />
              <button onClick={createList} style={{ background: "#00D4AA", border: "none", borderRadius: 4, color: "#000", fontSize: 11, padding: "3px 6px", cursor: "pointer" }}>+</button>
              <button onClick={() => setCreating(false)} style={{ background: "none", border: "none", color: "rgba(255,255,255,0.3)", cursor: "pointer", fontSize: 11 }}>✕</button>
            </div>
          : <button onClick={() => setCreating(true)} style={{ padding: "7px 10px", background: "none", border: "none", color: "rgba(255,255,255,0.25)", fontSize: 14, cursor: "pointer" }}>+</button>
        }
      </div>
      {/* Search */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", position: "relative", flexShrink: 0 }}>
        <input value={search} onChange={e => handleSearch(e.target.value)} placeholder="Add stock…" style={{ width: "100%", boxSizing: "border-box", background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "5px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        {results.length > 0 && (
          <div style={{ position: "absolute", top: "100%", left: 8, right: 8, background: "#151D2E", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 6, zIndex: 10 }}>
            {results.map(r => (
              <button key={r.id} onClick={() => addStock(r.id)} style={{ display: "block", width: "100%", padding: "6px 10px", background: "none", border: "none", color: "#fff", fontSize: 11, cursor: "pointer", textAlign: "left", fontFamily: "'Space Mono',monospace" }}>
                <span style={{ color: "#00D4AA" }}>{r.tradingCode}</span> <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 10 }}>{r.companyName}</span>
              </button>
            ))}
          </div>
        )}
      </div>
      {/* Stock list */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {(activeList?.stocks ?? []).length === 0
          ? <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: "20px 0", fontFamily: "'Space Mono',monospace" }}>No stocks — search above to add</div>
          : (activeList?.stocks ?? []).map(s => {
              const live = livePrice(s.tradingCode) ?? s
              const up = (live.change ?? live.changePercent ?? 0) >= 0
              return (
                <div key={s.id ?? s.stockId} style={{ display: "flex", alignItems: "center", padding: "6px 10px", borderBottom: "1px solid rgba(255,255,255,0.03)", gap: 8 }}>
                  <div style={{ flex: 1 }}>
                    <div style={{ color: "#fff", fontSize: 11, fontWeight: 700, fontFamily: "'Space Mono',monospace" }}>{s.tradingCode}</div>
                    <div style={{ color: "rgba(255,255,255,0.3)", fontSize: 10 }}>{s.companyName}</div>
                  </div>
                  <div style={{ textAlign: "right" }}>
                    <div style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>৳{(live.lastTradePrice ?? 0).toFixed(2)}</div>
                    <div style={{ color: up ? "#00D4AA" : "#FF6B6B", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{up ? "▲" : "▼"}{Math.abs(live.changePercent ?? 0).toFixed(2)}%</div>
                  </div>
                  <button onClick={() => removeStock(s.stockId ?? s.id)} style={{ background: "none", border: "none", color: "rgba(255,255,255,0.2)", cursor: "pointer", fontSize: 14, padding: "0 2px" }}>×</button>
                </div>
              )
            })
        }
      </div>
    </div>
  )
}
