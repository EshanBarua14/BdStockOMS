// @ts-nocheck
import { useState, useMemo } from "react"
import { useOrders } from "@/hooks/useOrders"

const STATUS_COLORS = {
  Pending: "#F59E0B", Open: "#3B82F6", PartiallyFilled: "#8B5CF6",
  Filled: "#00D4AA", Cancelled: "rgba(255,255,255,0.25)",
  Rejected: "#FF6B6B", Expired: "rgba(255,255,255,0.2)",
}

export function OrderBookWidget({ linkedSymbol, onSymbolClick }) {
  const { orders, loading, cancelOrder } = useOrders()
  const [filter, setFilter]   = useState("Open")
  const [search, setSearch]   = useState("")
  const [sideF, setSideF]     = useState("All")
  const [cancelling, setCancelling] = useState(null)

  const filtered = useMemo(() => orders.filter(o => {
    if (filter !== "All" && o.status !== filter) return false
    if (sideF !== "All" && o.side !== sideF) return false
    if (search && !o.symbol?.toUpperCase().includes(search.toUpperCase())) return false
    return true
  }), [orders, filter, sideF, search])

  const handleCancel = async (orderId) => {
    setCancelling(orderId)
    await cancelOrder(orderId, "User cancelled")
    setCancelling(null)
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "#0D1320", overflow: "hidden" }}>
      {/* Header + filters */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", flexDirection: "column", gap: 5, flexShrink: 0 }}>
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Filter symbol…"
            style={{ flex: 1, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "4px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
          {["All","Buy","Sell"].map(s => (
            <button key={s} onClick={() => setSideF(s)} style={{ padding: "4px 8px", background: sideF === s ? (s === "Buy" ? "rgba(0,212,170,0.2)" : s === "Sell" ? "rgba(255,107,107,0.2)" : "rgba(255,255,255,0.1)") : "none", border: `1px solid ${sideF === s ? "rgba(255,255,255,0.2)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: sideF === s ? "#fff" : "rgba(255,255,255,0.35)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{s}</button>
          ))}
        </div>
        <div style={{ display: "flex", gap: 3 }}>
          {["All","Open","Pending","Filled","Cancelled"].map(s => (
            <button key={s} onClick={() => setFilter(s)} style={{ padding: "3px 7px", background: filter === s ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${filter === s ? "rgba(255,255,255,0.15)" : "transparent"}`, borderRadius: 4, color: filter === s ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{s}</button>
          ))}
        </div>
      </div>

      {/* Column headers */}
      <div style={{ display: "grid", gridTemplateColumns: "80px 40px 60px 70px 70px 60px 60px", gap: 4, padding: "4px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", flexShrink: 0 }}>
        {["SYMBOL","SIDE","TYPE","QTY","PRICE","STATUS",""].map(h => (
          <span key={h} style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace", letterSpacing: "0.06em" }}>{h}</span>
        ))}
      </div>

      {/* Rows */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading
          ? <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: 16 }}>Loading…</div>
          : filtered.length === 0
            ? <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: 16, fontFamily: "'Space Mono',monospace" }}>No orders</div>
            : filtered.map(o => (
                <div key={o.orderId} onClick={() => onSymbolClick?.(o.symbol)}
                  style={{ display: "grid", gridTemplateColumns: "80px 40px 60px 70px 70px 60px 60px", gap: 4, padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.03)", cursor: "pointer", background: linkedSymbol === o.symbol ? "rgba(0,212,170,0.04)" : "transparent" }}>
                  <span style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{o.symbol}</span>
                  <span style={{ color: o.side === "Buy" ? "#00D4AA" : "#FF6B6B", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.side}</span>
                  <span style={{ color: "rgba(255,255,255,0.5)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.type}</span>
                  <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.quantity}</span>
                  <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>৳{o.price?.toFixed(2) ?? "—"}</span>
                  <span style={{ color: STATUS_COLORS[o.status] ?? "#fff", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{o.status}</span>
                  {(o.status === "Open" || o.status === "Pending") && (
                    <button onClick={e => { e.stopPropagation(); handleCancel(o.orderId) }}
                      disabled={cancelling === o.orderId}
                      style={{ background: "rgba(255,107,107,0.15)", border: "1px solid rgba(255,107,107,0.3)", borderRadius: 3, color: "#FF6B6B", fontSize: 9, cursor: "pointer", padding: "2px 4px", fontFamily: "'Space Mono',monospace" }}>
                      {cancelling === o.orderId ? "…" : "CXL"}
                    </button>
                  )}
                </div>
              ))
        }
      </div>

      {/* Footer stats */}
      <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "4px 8px", display: "flex", gap: 12, flexShrink: 0 }}>
        {[["Total", orders.length], ["Open", orders.filter(o => o.status === "Open").length], ["Filled", orders.filter(o => o.status === "Filled").length]].map(([l, v]) => (
          <span key={l} style={{ color: "rgba(255,255,255,0.3)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{l}: <span style={{ color: "#fff" }}>{v}</span></span>
        ))}
      </div>
    </div>
  )
}
