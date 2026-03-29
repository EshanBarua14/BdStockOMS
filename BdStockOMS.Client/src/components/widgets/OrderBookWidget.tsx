// @ts-nocheck
// src/components/widgets/OrderBookWidget.tsx
import { useState, useMemo } from "react"
import { OrderStatusBadge, OrderSideBadge } from "./OrderBook/OrderStatusBadge"
import { useOrders, ORDER_STATUS, ORDER_CAT_LABEL } from "@/hooks/useOrders"

const STATUS_LABELS: Record<string, string[]> = {
  "All":       [],
  "Open":      ["Open","Submitted"],
  "Pending":   ["Pending","Queued","Waiting"],
  "Filled":    ["Filled","Completed"],
  "Cancelled": ["Cancelled","Rejected","Deleted","CancelRequested"],
}

export function OrderBookWidget({ linkedSymbol, onSymbolClick }: any) {
  const { orders, loading, cancel } = useOrders()
  const [filter, setFilter]         = useState("All")
  const [search, setSearch]         = useState("")
  const [sideF,  setSideF]          = useState("All")
  const [cancelling, setCancelling] = useState<number | null>(null)

  const filtered = useMemo(() => orders.filter(o => {
    const allowed = STATUS_LABELS[filter]
    const statusStr = String(o.status)
    if (allowed.length > 0 && !allowed.includes(statusStr)) return false
    if (sideF === "Buy"  && o.orderType !== 0) return false
    if (sideF === "Sell" && o.orderType !== 1) return false
    const code = o.tradingCode ?? ""
    if (search && !code.toUpperCase().includes(search.toUpperCase())) return false
    return true
  }), [orders, filter, sideF, search])

  const handleCancel = async (orderId: number) => {
    setCancelling(orderId)
    await cancel(orderId)
    setCancelling(null)
  }

  const counts = {
    total:     orders.length,
    pending:   orders.filter(o => ["Pending","Queued","Waiting"].includes(String(o.status))).length,
    open:      orders.filter(o => ["Open","Submitted"].includes(String(o.status))).length,
    filled:    orders.filter(o => ["Filled","Completed"].includes(String(o.status))).length,
    cancelled: orders.filter(o => ["Cancelled","Rejected","Deleted"].includes(String(o.status))).length,
  }

  const s = {
    wrap:    { height:"100%", display:"flex", flexDirection:"column" as const, background:"var(--t-surface)", overflow:"hidden", fontFamily:"var(--font-mono,'JetBrains Mono',monospace)" },
    filterBar: { padding:"6px 8px 0", borderBottom:"1px solid var(--t-border)", flexShrink:0, background:"var(--t-panel)" },
    row1:    { display:"flex", gap:4, alignItems:"center", marginBottom:5 },
    input:   { flex:1, background:"var(--t-hover)", border:"1px solid var(--t-border)", borderRadius:4, padding:"3px 8px", color:"var(--t-text1)", fontSize:11, outline:"none" },
    tabBar:  { display:"flex", gap:2, paddingBottom:5 },
    colHead: { display:"grid", gridTemplateColumns:"80px 48px 56px 60px 76px 76px 44px", gap:4, padding:"4px 8px", borderBottom:"1px solid var(--t-border)", flexShrink:0, background:"var(--t-panel)" },
    colTxt:  { color:"var(--t-text3)", fontSize:9, letterSpacing:"0.07em" },
    foot:    { borderTop:"1px solid var(--t-border)", padding:"4px 8px", display:"flex", gap:10, flexShrink:0, background:"var(--t-panel)", flexWrap:"wrap" as const },
    footLbl: { color:"var(--t-text3)", fontSize:9 },
  }

  const sideBtn = (label: string) => {
    const active = sideF === label
    const isBuy  = label === "Buy"
    const isSell = label === "Sell"
    return (
      <button key={label} onClick={() => setSideF(label)} style={{
        padding:"3px 10px", borderRadius:4, fontSize:10, fontWeight: active ? 700 : 400, cursor:"pointer",
        border: active
          ? `1px solid ${isBuy ? "rgba(0,212,170,0.4)" : isSell ? "rgba(255,107,107,0.4)" : "var(--t-border)"}`
          : "1px solid transparent",
        background: active
          ? (isBuy ? "rgba(0,212,170,0.12)" : isSell ? "rgba(255,107,107,0.12)" : "var(--t-hover)")
          : "transparent",
        color: active
          ? (isBuy ? "var(--t-buy)" : isSell ? "var(--t-sell)" : "var(--t-text1)")
          : "var(--t-text3)",
      }}>{label}</button>
    )
  }

  const tabBtn = (label: string) => {
    const active = filter === label
    return (
      <button key={label} onClick={() => setFilter(label)} style={{
        padding:"2px 9px", borderRadius:3, fontSize:10, cursor:"pointer",
        background: active ? "var(--t-hover)" : "transparent",
        border: active ? "1px solid var(--t-border)" : "1px solid transparent",
        color: active ? "var(--t-text1)" : "var(--t-text3)",
        fontWeight: active ? 500 : 400,
      }}>{label}</button>
    )
  }

  return (
    <div style={s.wrap}>

      {/* Filter bar */}
      <div style={s.filterBar}>
        <div style={s.row1}>
          <input
            value={search} onChange={e => setSearch(e.target.value)}
            placeholder="Filter symbol…" style={s.input}
            onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
          {["All","Buy","Sell"].map(sideBtn)}
        </div>
        <div style={s.tabBar}>
          {["All","Open","Pending","Filled","Cancelled"].map(tabBtn)}
        </div>
      </div>

      {/* Column headers */}
      <div style={s.colHead}>
        {["SYMBOL","SIDE","TYPE","QTY","PRICE","STATUS",""].map(h => (
          <span key={h} style={s.colTxt}>{h}</span>
        ))}
      </div>

      {/* Rows */}
      <div style={{ flex:1, overflowY:"auto" }}>
        {loading ? (
          <div style={{ textAlign:"center", color:"var(--t-text3)", fontSize:11, padding:20 }}>Loading…</div>
        ) : filtered.length === 0 ? (
          <div style={{ textAlign:"center", color:"var(--t-text3)", fontSize:11, padding:20 }}>No orders</div>
        ) : filtered.map(o => {
          const isLinked = linkedSymbol && linkedSymbol === o.tradingCode
          return (
            <div key={o.id}
              onClick={() => onSymbolClick?.(o.tradingCode)}
              style={{
                display:"grid", gridTemplateColumns:"80px 48px 56px 60px 76px 76px 44px",
                gap:4, padding:"5px 8px",
                borderBottom:"1px solid var(--t-border)",
                cursor:"pointer",
                background: isLinked ? "rgba(0,212,170,0.04)" : "transparent",
                alignItems:"center",
              }}
              onMouseEnter={e => { if (!isLinked) e.currentTarget.style.background = "var(--t-hover)" }}
              onMouseLeave={e => e.currentTarget.style.background = isLinked ? "rgba(0,212,170,0.04)" : "transparent"}
            >
              <span style={{ color:"var(--t-text1)", fontSize:11, fontWeight:700, overflow:"hidden", textOverflow:"ellipsis", whiteSpace:"nowrap" }}>
                {o.tradingCode || `#${o.stockId}`}
              </span>
              <OrderSideBadge side={o.orderType} />
              <span style={{ color:"var(--t-text3)", fontSize:10 }}>
                {ORDER_CAT_LABEL[o.orderCategory] ?? "—"}
              </span>
              <span style={{ color:"var(--t-text2)", fontSize:10 }}>
                {o.quantity?.toLocaleString()}
              </span>
              <span style={{ color:"var(--t-text2)", fontSize:10 }}>
                {o.limitPrice != null ? `৳${o.limitPrice.toFixed(2)}` : o.priceAtOrder != null ? `৳${o.priceAtOrder.toFixed(2)}` : "MKT"}
              </span>
              <OrderStatusBadge status={o.status} />
              <div>
                {(["Pending","Open","Queued","Submitted","Waiting"].includes(String(o.status))) && (
                  <button
                    onClick={e => { e.stopPropagation(); handleCancel(o.id) }}
                    disabled={cancelling === o.id}
                    style={{
                      background:"rgba(255,107,107,0.1)", border:"1px solid rgba(255,107,107,0.3)",
                      borderRadius:3, color:"var(--t-sell)", fontSize:9, cursor:"pointer",
                      padding:"2px 5px", fontWeight:700, opacity: cancelling === o.id ? 0.5 : 1,
                    }}>
                    {cancelling === o.id ? "…" : "CXL"}
                  </button>
                )}
              </div>
            </div>
          )
        })}
      </div>

      {/* Footer */}
      <div style={s.foot}>
        {[
          ["Total",     counts.total,     "var(--t-text1)"],
          ["Pending",   counts.pending,   "#f59e0b"],
          ["Open",      counts.open,      "#38bdf8"],
          ["Filled",    counts.filled,    "#10b981"],
          ["Cancelled", counts.cancelled, "var(--t-text3)"],
        ].map(([l, v, c]) => (
          <span key={l as string} style={s.footLbl}>
            {l}: <b style={{ color: c as string }}>{v as number}</b>
          </span>
        ))}
      </div>
    </div>
  )
}
