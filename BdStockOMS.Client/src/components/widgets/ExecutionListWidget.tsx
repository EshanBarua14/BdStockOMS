// @ts-nocheck
// src/components/widgets/ExecutionListWidget.tsx
// Day 63 redesign — styled exactly like OrderBookWidget
// Same filter bar, grid columns, row style, footer stats

import { useMemo, useState } from "react"
import { useOrders, ORDER_STATUS } from "@/hooks/useOrders"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

const mono = "'JetBrains Mono', monospace"

const SIDE_COLORS: Record<number, string> = {
  0: "var(--t-buy)",   // Buy
  1: "var(--t-sell)",  // Sell
}

const STATUS_COLORS: Record<number, string> = {
  2: "#8B5CF6",   // PartiallyFilled
  3: "#00D4AA",   // Filled
}

function fmtTime(d: string) {
  return new Date(d).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}
function fmtVal(v: number) {
  if (v >= 1e7) return `৳${(v / 1e7).toFixed(2)}Cr`
  if (v >= 1e5) return `৳${(v / 1e5).toFixed(1)}L`
  if (v >= 1e3) return `৳${(v / 1e3).toFixed(0)}K`
  return `৳${v.toLocaleString()}`
}

type DateFilter = "Today" | "All"
type SortKey = "createdAt" | "tradingCode" | "orderType" | "quantity" | "price" | "value"

export function ExecutionListWidget({
  linkedSymbol, onSymbolClick, colorGroup
}: {
  linkedSymbol?: string
  onSymbolClick?: (sym: string) => void
  colorGroup?: string | null
}) {
  const { orders: _o } = useOrders()
  const orders = _o ?? []
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)

  const [search,  setSearch]  = useState("")
  const [sideF,   setSideF]   = useState("All")
  const [dateF,   setDateF]   = useState<DateFilter>("Today")
  const [sortKey, setSortKey] = useState<SortKey>("createdAt")
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc")
  const [cancelling] = useState<number | null>(null)

  const activeLinked = _linked ?? linkedSymbol ?? null

  const executions = useMemo(() => {
    const now = new Date()
    return orders.filter(o => {
      if (o.status !== 3 && o.status !== 2) return false
      if (sideF === "Buy"  && o.orderType !== 0) return false
      if (sideF === "Sell" && o.orderType !== 1) return false
      if (search && !o.tradingCode?.toUpperCase().includes(search.toUpperCase())) return false
      if (dateF === "Today") return new Date(o.createdAt).toDateString() === now.toDateString()
      return true
    })
  }, [orders, sideF, search, dateF])

  const sorted = useMemo(() => {
    return [...executions].sort((a, b) => {
      let av: any, bv: any
      if (sortKey === "value") {
        av = (a.filledQuantity || a.quantity) * (a.price ?? 0)
        bv = (b.filledQuantity || b.quantity) * (b.price ?? 0)
      } else if (sortKey === "createdAt") {
        av = new Date(a.createdAt).getTime(); bv = new Date(b.createdAt).getTime()
      } else {
        av = (a as any)[sortKey]; bv = (b as any)[sortKey]
      }
      const cmp = av < bv ? -1 : av > bv ? 1 : 0
      return sortDir === "asc" ? cmp : -cmp
    })
  }, [executions, sortKey, sortDir])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) setSortDir(d => d === "asc" ? "desc" : "asc")
    else { setSortKey(key); setSortDir("desc") }
  }

  const handleRowClick = (code: string) => { emitSymbol(code); onSymbolClick?.(code) }

  const totalBought = executions.filter(o => o.orderType === 0).reduce((a, o) => a + ((o.filledQuantity || o.quantity) * (o.price ?? 0)), 0)
  const totalSold   = executions.filter(o => o.orderType === 1).reduce((a, o) => a + ((o.filledQuantity || o.quantity) * (o.price ?? 0)), 0)

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Filter bar — identical structure to OrderBookWidget ── */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid var(--t-border)", display: "flex", flexDirection: "column", gap: 5, flexShrink: 0, background: "var(--t-panel)" }}>
        {/* Row 1: search + side filter */}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Filter symbol…"
            style={{ flex: 1, background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono }}
            onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
          {["All", "Buy", "Sell"].map(s => (
            <button key={s} onClick={() => setSideF(s)} style={{
              padding: "4px 8px",
              background: sideF === s ? (s === "Buy" ? "rgba(0,212,170,0.15)" : s === "Sell" ? "rgba(255,107,107,0.15)" : "rgba(255,255,255,0.08)") : "none",
              border: `1px solid ${sideF === s ? "var(--t-border)" : "transparent"}`,
              borderRadius: 4,
              color: sideF === s ? (s === "Buy" ? "var(--t-buy)" : s === "Sell" ? "var(--t-sell)" : "var(--t-text1)") : "var(--t-text3)",
              fontSize: 10, cursor: "pointer", fontFamily: mono, fontWeight: sideF === s ? 700 : 400,
            }}>{s}</button>
          ))}
        </div>
        {/* Row 2: date filter tabs */}
        <div style={{ display: "flex", gap: 3 }}>
          {(["Today", "All"] as DateFilter[]).map(d => (
            <button key={d} onClick={() => setDateF(d)} style={{
              padding: "3px 7px",
              background: dateF === d ? "var(--t-hover)" : "none",
              border: `1px solid ${dateF === d ? "var(--t-border)" : "transparent"}`,
              borderRadius: 4, color: dateF === d ? "var(--t-text1)" : "var(--t-text3)",
              fontSize: 10, cursor: "pointer", fontFamily: mono,
            }}>{d}</button>
          ))}
          <span style={{ marginLeft: "auto", fontSize: 9, color: "var(--t-text3)", fontFamily: mono, alignSelf: "center" }}>
            {sorted.length} fills
          </span>
        </div>
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "grid", gridTemplateColumns: "80px 44px 60px 64px 72px 64px", gap: 4, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {[
          { key: "tradingCode", label: "SYMBOL"  },
          { key: "orderType",   label: "SIDE"    },
          { key: "quantity",    label: "QTY"     },
          { key: "price",       label: "PRICE"   },
          { key: "value",       label: "VALUE"   },
          { key: "createdAt",   label: "TIME"    },
        ].map(col => (
          <button key={col.key} onClick={() => handleSort(col.key as SortKey)} style={{
            background: "none", border: "none", cursor: "pointer", padding: 0, textAlign: "left",
            color: sortKey === col.key ? "var(--t-accent)" : "var(--t-text3)",
            fontSize: 9, fontFamily: mono, letterSpacing: "0.06em", display: "flex", alignItems: "center", gap: 2,
          }}>
            {col.label}
            {sortKey === col.key && <span style={{ fontSize: 7 }}>{sortDir === "asc" ? "▲" : "▼"}</span>}
          </button>
        ))}
      </div>

      {/* ── Rows ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {sorted.length === 0 ? (
          <div style={{ textAlign: "center", color: "var(--t-text3)", fontSize: 11, padding: 16, fontFamily: mono }}>
            {dateF === "Today" ? "No fills today — switch to 'All'" : "No executions found"}
          </div>
        ) : sorted.map((o, idx) => {
          const val       = (o.filledQuantity || o.quantity) * (o.price ?? 0)
          const isBuy     = o.orderType === 0
          const isLinked  = activeLinked && activeLinked === o.tradingCode
          const isPartial = o.status === 2
          const statusInfo = ORDER_STATUS[o.status]

          return (
            <div key={o.id ?? o.orderId ?? idx}
              onClick={() => handleRowClick(o.tradingCode)}
              style={{
                display: "grid", gridTemplateColumns: "80px 44px 60px 64px 72px 64px",
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
              {/* Qty */}
              <span style={{ color: "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {(o.filledQuantity || o.quantity)?.toLocaleString()}{isPartial && <span style={{ color: "var(--t-sell)", fontSize: 8, marginLeft: 2 }}>~</span>}
              </span>
              {/* Price */}
              <span style={{ color: "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {o.price != null ? `৳${o.price.toFixed(2)}` : "—"}
              </span>
              {/* Value */}
              <span style={{ color: STATUS_COLORS[o.status] ?? "var(--t-text2)", fontSize: 10, fontFamily: mono }}>
                {fmtVal(val)}
              </span>
              {/* Time */}
              <span style={{ color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>
                {fmtTime(o.createdAt)}
              </span>
            </div>
          )
        })}
      </div>

      {/* ── Footer stats — identical structure to OrderBookWidget ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", padding: "4px 8px", display: "flex", gap: 12, flexShrink: 0, background: "var(--t-panel)" }}>
        {[
          ["Fills",  sorted.length],
          ["Bought", totalBought >= 1e5 ? `${(totalBought / 1e5).toFixed(1)}L` : totalBought.toLocaleString()],
          ["Sold",   totalSold   >= 1e5 ? `${(totalSold   / 1e5).toFixed(1)}L` : totalSold.toLocaleString()],
          ["Net",    Math.abs(totalBought - totalSold) >= 1e5 ? `${((totalBought - totalSold) / 1e5).toFixed(1)}L` : (totalBought - totalSold).toLocaleString()],
        ].map(([l, v]) => (
          <span key={l} style={{ color: "var(--t-text3)", fontSize: 9, fontFamily: mono }}>
            {l}: <span style={{ color: l === "Bought" ? "var(--t-buy)" : l === "Sold" ? "var(--t-sell)" : "var(--t-text1)", fontWeight: 700 }}>{v}</span>
          </span>
        ))}
      </div>
    </div>
  )
}
