// @ts-nocheck
// src/components/widgets/ExecutionListWidget.tsx
// Day 63 redesign — matches OMS design language: JetBrains Mono, var(--t-*) tokens,
// sortable columns, status badges, stats footer, color group sync

import { useMemo, useState } from "react"
import { useOrders } from "@/hooks/useOrders"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

const mono = "'JetBrains Mono', monospace"

// ─── Helpers ──────────────────────────────────────────────────────────────────
function fmtTime(d: string) {
  return new Date(d).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}
function fmtVal(v: number) {
  if (v >= 1e7) return `৳${(v / 1e7).toFixed(2)}Cr`
  if (v >= 1e5) return `৳${(v / 1e5).toFixed(1)}L`
  return `৳${v.toLocaleString()}`
}

type SortKey = "createdAt" | "tradingCode" | "orderType" | "quantity" | "price" | "value"
type DateFilter = "Today" | "All"

const COLS: { key: SortKey; label: string; w: number; align: "left" | "right" | "center" }[] = [
  { key: "tradingCode", label: "SYMBOL",  w: 80,  align: "left"   },
  { key: "orderType",   label: "SIDE",    w: 44,  align: "center" },
  { key: "quantity",    label: "QTY",     w: 56,  align: "right"  },
  { key: "price",       label: "PRICE",   w: 68,  align: "right"  },
  { key: "value",       label: "VALUE",   w: 80,  align: "right"  },
  { key: "createdAt",   label: "TIME",    w: 72,  align: "right"  },
]

// ─── Component ────────────────────────────────────────────────────────────────
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
  const [dateF,   setDateF]   = useState<DateFilter>("Today")
  const [sortKey, setSortKey] = useState<SortKey>("createdAt")
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc")

  const activeLinked = _linked ?? linkedSymbol ?? null

  const executions = useMemo(() => {
    const now = new Date()
    return orders.filter(o => {
      if (o.status !== 3 && o.status !== 2) return false
      if (search && !o.tradingCode?.toUpperCase().includes(search.toUpperCase())) return false
      if (dateF === "Today") {
        return new Date(o.createdAt).toDateString() === now.toDateString()
      }
      return true
    })
  }, [orders, search, dateF])

  const sorted = useMemo(() => {
    return [...executions].sort((a, b) => {
      let av: any, bv: any
      if (sortKey === "value") {
        av = (a.filledQuantity || a.quantity) * (a.price ?? 0)
        bv = (b.filledQuantity || b.quantity) * (b.price ?? 0)
      } else if (sortKey === "createdAt") {
        av = new Date(a.createdAt).getTime()
        bv = new Date(b.createdAt).getTime()
      } else {
        av = (a as any)[sortKey]
        bv = (b as any)[sortKey]
      }
      const cmp = av < bv ? -1 : av > bv ? 1 : 0
      return sortDir === "asc" ? cmp : -cmp
    })
  }, [executions, sortKey, sortDir])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) setSortDir(d => d === "asc" ? "desc" : "asc")
    else { setSortKey(key); setSortDir("desc") }
  }

  const handleRowClick = (code: string) => {
    emitSymbol(code)
    onSymbolClick?.(code)
  }

  const totalBought = executions.filter(o => o.orderType === 0).reduce((a, o) => a + ((o.filledQuantity || o.quantity) * (o.price ?? 0)), 0)
  const totalSold   = executions.filter(o => o.orderType === 1).reduce((a, o) => a + ((o.filledQuantity || o.quantity) * (o.price ?? 0)), 0)
  const buyCount    = executions.filter(o => o.orderType === 0).length
  const sellCount   = executions.filter(o => o.orderType === 1).length

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Toolbar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {/* Search */}
        <div style={{ position: "relative", flex: 1 }}>
          <span style={{ position: "absolute", left: 7, top: "50%", transform: "translateY(-50%)", color: "var(--t-text3)", fontSize: 10 }}>⌕</span>
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Symbol…"
            style={{
              width: "100%", boxSizing: "border-box",
              background: "var(--t-hover)", border: "1px solid var(--t-border)",
              borderRadius: 5, padding: "4px 8px 4px 22px",
              color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono,
            }}
            onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
        </div>

        {/* Date filter */}
        <div style={{ display: "flex", gap: 2 }}>
          {(["Today", "All"] as DateFilter[]).map(d => (
            <button key={d} onClick={() => setDateF(d)} style={{
              padding: "3px 8px", fontSize: 9, fontWeight: 700, fontFamily: mono,
              background: dateF === d ? "rgba(var(--t-accent-rgb,0,212,170),0.12)" : "transparent",
              border: `1px solid ${dateF === d ? "var(--t-accent)" : "var(--t-border)"}`,
              borderRadius: 4, color: dateF === d ? "var(--t-accent)" : "var(--t-text3)", cursor: "pointer",
            }}>{d}</button>
          ))}
        </div>

        {/* Count badge */}
        <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, flexShrink: 0 }}>
          {sorted.length} fills
        </span>
      </div>

      {/* ── Column Headers ── */}
      <div style={{
        display: "grid",
        gridTemplateColumns: COLS.map(c => `${c.w}px`).join(" "),
        gap: 0, padding: "4px 8px",
        borderBottom: "1px solid var(--t-border)",
        background: "var(--t-bg)", flexShrink: 0,
      }}>
        {COLS.map(col => (
          <button key={col.key} onClick={() => handleSort(col.key)} style={{
            background: "none", border: "none", cursor: "pointer", padding: "0 2px",
            textAlign: col.align as any,
            color: sortKey === col.key ? "var(--t-accent)" : "var(--t-text3)",
            fontSize: 9, fontWeight: 700, fontFamily: mono, letterSpacing: "0.06em",
            display: "flex", alignItems: "center",
            justifyContent: col.align === "right" ? "flex-end" : col.align === "center" ? "center" : "flex-start",
            gap: 2,
          }}>
            {col.label}
            {sortKey === col.key && (
              <span style={{ fontSize: 7 }}>{sortDir === "asc" ? "▲" : "▼"}</span>
            )}
          </button>
        ))}
      </div>

      {/* ── Rows ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {sorted.length === 0 ? (
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: "100%", gap: 6, color: "var(--t-text3)" }}>
            <span style={{ fontSize: 24, opacity: 0.3 }}>✅</span>
            <span style={{ fontSize: 11, fontFamily: mono }}>No executions</span>
            <span style={{ fontSize: 9 }}>{dateF === "Today" ? "Switch to 'All' to see history" : "No filled orders found"}</span>
          </div>
        ) : sorted.map((o, idx) => {
          const val     = (o.filledQuantity || o.quantity) * (o.price ?? 0)
          const isBuy   = o.orderType === 0
          const isActive = activeLinked === o.tradingCode
          const isPartial = o.status === 2

          return (
            <div
              key={o.id ?? o.orderId ?? idx}
              onClick={() => handleRowClick(o.tradingCode)}
              style={{
                display: "grid",
                gridTemplateColumns: COLS.map(c => `${c.w}px`).join(" "),
                gap: 0, padding: "5px 8px",
                borderBottom: "1px solid var(--t-border)",
                background: isActive
                  ? "rgba(0,212,170,0.04)"
                  : idx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
                cursor: "pointer",
                transition: "background 0.08s",
                borderLeft: isActive ? "2px solid var(--t-accent)" : "2px solid transparent",
              }}
              onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = "var(--t-hover)" }}
              onMouseLeave={e => { e.currentTarget.style.background = isActive ? "rgba(0,212,170,0.04)" : idx % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)" }}
            >
              {/* Symbol */}
              <span style={{ color: "var(--t-accent)", fontWeight: 700, fontFamily: mono, fontSize: 11, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                {o.tradingCode || `#${o.stockId}`}
              </span>

              {/* Side */}
              <div style={{ display: "flex", justifyContent: "center" }}>
                <span style={{
                  fontSize: 9, fontWeight: 800, fontFamily: mono,
                  padding: "1px 5px", borderRadius: 3,
                  background: isBuy ? "rgba(0,230,118,0.12)" : "rgba(255,23,68,0.12)",
                  color: isBuy ? "var(--t-buy)" : "var(--t-sell)",
                  border: `1px solid ${isBuy ? "rgba(0,230,118,0.2)" : "rgba(255,23,68,0.2)"}`,
                }}>
                  {isBuy ? "BUY" : "SELL"}
                </span>
              </div>

              {/* Qty */}
              <div style={{ textAlign: "right" }}>
                <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 10 }}>
                  {(o.filledQuantity || o.quantity).toLocaleString()}
                  {isPartial && <span style={{ color: "var(--t-sell)", fontSize: 8, marginLeft: 2 }}>~</span>}
                </span>
              </div>

              {/* Price */}
              <div style={{ textAlign: "right" }}>
                <span style={{ color: "var(--t-text1)", fontFamily: mono, fontSize: 10 }}>
                  ৳{(o.price ?? 0).toFixed(2)}
                </span>
              </div>

              {/* Value */}
              <div style={{ textAlign: "right" }}>
                <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 10 }}>
                  {fmtVal(val)}
                </span>
              </div>

              {/* Time */}
              <div style={{ textAlign: "right" }}>
                <span style={{ color: "var(--t-text3)", fontFamily: mono, fontSize: 9 }}>
                  {fmtTime(o.createdAt)}
                </span>
              </div>
            </div>
          )
        })}
      </div>

      {/* ── Stats Footer ── */}
      <div style={{
        borderTop: "1px solid var(--t-border)", padding: "5px 8px",
        display: "flex", gap: 12, flexShrink: 0, background: "var(--t-panel)",
        alignItems: "center",
      }}>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
          ▲ <span style={{ color: "var(--t-buy)", fontWeight: 700 }}>{buyCount}</span>
          <span style={{ marginLeft: 4, color: "var(--t-text3)" }}>{fmtVal(totalBought)}</span>
        </span>
        <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
          ▼ <span style={{ color: "var(--t-sell)", fontWeight: 700 }}>{sellCount}</span>
          <span style={{ marginLeft: 4, color: "var(--t-text3)" }}>{fmtVal(totalSold)}</span>
        </span>
        <span style={{ marginLeft: "auto", fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
          Net: <span style={{ color: totalBought - totalSold >= 0 ? "var(--t-buy)" : "var(--t-sell)", fontWeight: 700 }}>
            {fmtVal(Math.abs(totalBought - totalSold))}
          </span>
        </span>
      </div>
    </div>
  )
}
