// @ts-nocheck
// src/components/widgets/OrderBookWidget.tsx
// Fixed Day 61 audit:
// - Field names corrected: o.symbol→o.tradingCode, o.side→derived from o.orderType,
//   o.type→ORDER_CAT_LABEL[o.orderCategory], o.price→o.limitPrice, o.orderId→o.id
// - Status filter uses numeric status codes not strings
// - Cancel uses correct numeric id

import { useState, useMemo } from "react"
import { useOrders, ORDER_STATUS, ORDER_TYPE_LABEL, ORDER_CAT_LABEL } from "@/hooks/useOrders"

const STATUS_COLORS: Record<number, string> = {
  0: "#F59E0B",
  1: "#3B82F6",
  2: "#22D3EE",
  3: "#00D4AA",
  4: "rgba(255,255,255,0.25)",
  5: "#FF6B6B",
  6: "rgba(255,255,255,0.2)",
}

const STATUS_LABELS: Record<string, number> = {
  "All": -1, "Open": 1, "Pending": 0, "Filled": 3, "Cancelled": 4
}

export function OrderBookWidget({ linkedSymbol, onSymbolClick }: any) {
  const { orders, loading, cancel } = useOrders()
  const [filter, setFilter] = useState("All")
  const [search, setSearch]     = useState("")
  const [sideF,  setSideF]      = useState("All")
  const [cancelling, setCancelling] = useState<number | null>(null)

  const filtered = useMemo(() => orders.filter(o => {
    const statusNum = STATUS_LABELS[filter]
    if (statusNum !== -1 && o.status !== statusNum) return false
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

  const mono = "'JetBrains Mono', monospace"

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Filters ── */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid var(--t-border)", display: "flex", flexDirection: "column", gap: 5, flexShrink: 0, background: "var(--t-panel)" }}>
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Filter symbol…"
            style={{ flex: 1, background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono }}
            onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
          {["All", "Buy", "Sell"].map(s => (
            <button key={s} onClick={() => setSideF(s)} style={{
              padding: "4px 8px",
              background: sideF === s
                ? (s === "Buy" ? "rgba(0,212,170,0.15)" : s === "Sell" ? "rgba(255,107,107,0.15)" : "rgba(255,255,255,0.08)")
                : "none",
              border: `1px solid ${sideF === s ? "var(--t-border)" : "transparent"}`,
              borderRadius: 4,
              color: sideF === s
                ? (s === "Buy" ? "var(--t-buy)" : s === "Sell" ? "var(--t-sell)" : "var(--t-text1)")
                : "var(--t-text3)",
              fontSize: 10, cursor: "pointer", fontFamily: mono, fontWeight: sideF === s ? 700 : 400,
            }}>{s}</button>
          ))}
        </div>
        <div style={{ display: "flex", gap: 3 }}>
          {["All", "Open", "Pending", "Filled", "Cancelled"].map(s => (
            <button key={s} onClick={() => setFilter(s)} style={{
              padding: "3px 7px",
              background: filter === s ? "var(--t-hover)" : "none",
              border: `1px solid ${filter === s ? "var(--t-border)" : "transparent"}`,
              borderRadius: 4,
              color: filter === s ? "var(--t-text1)" : "var(--t-text3)",
              fontSize: 10, cursor: "pointer", fontFamily: mono,
            }}>{s}</button>
          ))}
        </div>
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "grid", gridTemplateColumns: "80px 44px 60px 64px 72px 64px 50px", gap: 4, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {["SYMBOL", "SIDE", "TYPE", "QTY", "PRICE", "STATUS", ""].map(h => (
          <span key={h} style={{ color: "var(--t-text3)", fontSize: 9, fontFamily: mono, letterSpacing: "0.06em" }}>{h}</span>
        ))}
      </div>

      {/* ── Rows ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading ? (
          <div style={{ textAlign: "center", color: "var(--t-text3)", fontSize: 11, padding: 16, fontFamily: mono }}>Loading…</div>
        ) : filtered.length === 0 ? (
          <div style={{ textAlign: "center", color: "var(--t-text3)", fontSize: 11, padding: 16, fontFamily: mono }}>No orders</div>
        ) : filtered.map(o => {
          const isBuy = o.orderType === 0
          const statusInfo = ORDER_STATUS[o.status] ?? { label: String(o.status), color: "text-zinc-500" } ?? { label: String(o.status), color: "text-zinc-500" }
          const isLinked = linkedSymbol && linkedSymbol === o.tradingCode
          return (
            <div key={o.id}
              onClick={() => onSymbolClick?.(o.tradingCode)}
              style={{
                display: "grid", gridTemplateColumns: "80px 44px 60px 64px 72px 64px 50px",
                gap: 4, padding: "5px 8px",
                borderBottom: "1px solid var(--t-border)",
                cursor: "pointer",
                background: isLinked ? "rgba(0,212,170,0.04)" : "transparent",
                transition: "background 0.08s",
              }}
              onMouseEnter={e => { if (!isLinked) e.currentTarget.style.background = "var(--t-hover)" }}
              onMouseLeave={e => e.currentTarget.style.background = isLinked ? "rgba(0,212,170,0.04)" : "transparent"}
            >
              {/* Symbol */}
              <span style={{ color: "var(--t-text1)", fontSize: 11, fontFamily: mono, fontWeight: 700, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                {o.tradingCode || `#${o.stockId}`}
              </span>
              {/* Side */}
              <span style={{ color: isBuy ? "var(--t-buy)" : "var(--t-sell)", fontSize: 10, fontFamily: mono, fontWeight: 700 }}>
                {isBuy ? "BUY" : "SELL"}
              </span>
              {/* Type */}
              <span style={{ color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>
                {ORDER_CAT_LABEL[o.orderCategory] ?? "—"}
              </span>
              {/* Qty */}
              <span style={{ color: "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {o.quantity?.toLocaleString()}
              </span>
              {/* Price */}
              <span style={{ color: "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {o.limitPrice != null ? `৳${o.limitPrice.toFixed(2)}` : "MKT"}
              </span>
              {/* Status */}
              <span style={{ color: STATUS_COLORS[o.status] ?? "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {statusInfo?.label ?? "Unknown"}
              </span>
              {/* Cancel button — only for Pending(0) or Open(1) */}
              <div>
                {(o.status === 0 || o.status === 1) && (
                  <button
                    onClick={e => { e.stopPropagation(); handleCancel(o.id) }}
                    disabled={cancelling === o.id}
                    style={{
                      background: "rgba(255,107,107,0.12)", border: "1px solid rgba(255,107,107,0.25)",
                      borderRadius: 3, color: "var(--t-sell)", fontSize: 9, cursor: "pointer",
                      padding: "2px 5px", fontFamily: mono, fontWeight: 700,
                      opacity: cancelling === o.id ? 0.5 : 1,
                    }}>
                    {cancelling === o.id ? "…" : "CXL"}
                  </button>
                )}
              </div>
            </div>
          )
        })}
      </div>

      {/* ── Footer stats ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", padding: "4px 8px", display: "flex", gap: 12, flexShrink: 0, background: "var(--t-panel)" }}>
        {[
          ["Total",     orders.length],
          ["Pending",   orders.filter(o => o.status === 0).length],
          ["Open",      orders.filter(o => o.status === 1).length],
          ["Filled",    orders.filter(o => o.status === 3).length],
          ["Cancelled", orders.filter(o => o.status === 4).length],
        ].map(([l, v]) => (
          <span key={l} style={{ color: "var(--t-text3)", fontSize: 9, fontFamily: mono }}>
            {l}: <span style={{ color: "var(--t-text1)", fontWeight: 700 }}>{v}</span>
          </span>
        ))}
      </div>
    </div>
  )
}
