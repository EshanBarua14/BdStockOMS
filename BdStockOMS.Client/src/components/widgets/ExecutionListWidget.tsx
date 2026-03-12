// @ts-nocheck
import { useMemo, useState } from "react"
import { useOrders } from "@/hooks/useOrders"

export function ExecutionListWidget({ linkedSymbol, onSymbolClick }) {
  const { orders } = useOrders()
  const [search, setSearch] = useState("")
  const [dateF, setDateF]   = useState("Today")

  const executions = useMemo(() => {
    const now = new Date()
    return orders.filter(o => {
      if (o.status !== "Filled") return false
      if (search && !o.symbol?.toUpperCase().includes(search.toUpperCase())) return false
      if (dateF === "Today") {
        const d = new Date(o.updatedAt ?? o.createdAt)
        return d.toDateString() === now.toDateString()
      }
      return true
    })
  }, [orders, search, dateF])

  const totalBought = executions.filter(o => o.side === "Buy").reduce((a, o) => a + (o.quantity * (o.price ?? 0)), 0)
  const totalSold   = executions.filter(o => o.side === "Sell").reduce((a, o) => a + (o.quantity * (o.price ?? 0)), 0)

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "#0D1320", overflow: "hidden" }}>
      <div style={{ padding: "6px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Symbol…"
          style={{ flex: 1, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "4px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        {["Today","All"].map(d => (
          <button key={d} onClick={() => setDateF(d)} style={{ padding: "4px 8px", background: dateF === d ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${dateF === d ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: dateF === d ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{d}</button>
        ))}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "80px 40px 60px 70px 80px 1fr", gap: 4, padding: "4px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", flexShrink: 0 }}>
        {["SYMBOL","SIDE","QTY","PRICE","VALUE","TIME"].map(h => (
          <span key={h} style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{h}</span>
        ))}
      </div>

      <div style={{ flex: 1, overflowY: "auto" }}>
        {executions.length === 0
          ? <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: 16, fontFamily: "'Space Mono',monospace" }}>No executions</div>
          : executions.map(o => {
            const val = (o.filledQuantity || o.quantity) * (o.price ?? 0)
            const t   = new Date(o.updatedAt ?? o.createdAt)
            return (
              <div key={o.orderId} onClick={() => onSymbolClick?.(o.symbol)}
                style={{ display: "grid", gridTemplateColumns: "80px 40px 60px 70px 80px 1fr", gap: 4, padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.03)", cursor: "pointer", background: linkedSymbol === o.symbol ? "rgba(0,212,170,0.04)" : "transparent" }}>
                <span style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{o.symbol}</span>
                <span style={{ color: o.side === "Buy" ? "#00D4AA" : "#FF6B6B", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.side}</span>
                <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.filledQuantity || o.quantity}</span>
                <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>৳{o.price?.toFixed(2) ?? "—"}</span>
                <span style={{ color: "#fff", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>৳{val.toLocaleString()}</span>
                <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{t.toLocaleTimeString()}</span>
              </div>
            )
          })
        }
      </div>

      <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "4px 8px", display: "flex", gap: 16, flexShrink: 0 }}>
        <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>Bought: <span style={{ color: "#00D4AA" }}>৳{totalBought.toLocaleString()}</span></span>
        <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>Sold: <span style={{ color: "#FF6B6B" }}>৳{totalSold.toLocaleString()}</span></span>
        <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>Count: <span style={{ color: "#fff" }}>{executions.length}</span></span>
      </div>
    </div>
  )
}
