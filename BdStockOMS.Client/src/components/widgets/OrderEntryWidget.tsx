// @ts-nocheck
// src/components/widgets/OrderEntryWidget.tsx
// Day 63 redesign — styled exactly like OrderBookWidget
// Same filter bar, grid layout, row style, footer stats

import { useState, useCallback } from "react"
import { createPortal } from "react-dom"
import { useOrders } from "@/hooks/useOrders"
import { useMarketData } from "@/hooks/useMarketData"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

const mono = "'JetBrains Mono', monospace"
const ORDER_TYPES = ["Limit", "Market", "Stop"]

// ─── Confirm Portal ───────────────────────────────────────────────────────────
function ConfirmModal({ side, symbol, qty, price, orderType, onConfirm, onCancel, placing }: any) {
  const isBuy  = side === "Buy"
  const color  = isBuy ? "var(--t-buy)" : "var(--t-sell)"
  const total  = orderType !== "Market" && qty && price ? Number(qty) * Number(price) : null
  return createPortal(
    <>
      <div onClick={onCancel} style={{ position: "fixed", inset: 0, zIndex: 9997, background: "rgba(0,0,0,0.6)", backdropFilter: "blur(4px)" }} />
      <div style={{ position: "fixed", top: "50%", left: "50%", zIndex: 9998, transform: "translate(-50%, -50%)", background: "var(--t-elevated)", border: `1px solid ${color}30`, borderRadius: 12, padding: "20px 24px", minWidth: 300, maxWidth: 380, boxShadow: "0 24px 64px rgba(0,0,0,0.6)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 16 }}>
          <div style={{ width: 32, height: 32, borderRadius: 8, background: `${color}15`, border: `1px solid ${color}30`, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 14, fontWeight: 800, color, fontFamily: mono }}>{isBuy ? "B" : "S"}</div>
          <div>
            <div style={{ fontSize: 13, fontWeight: 800, color: "var(--t-text1)", fontFamily: mono }}>Confirm Order</div>
            <div style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{orderType} · Enter to confirm · Esc to cancel</div>
          </div>
        </div>
        <div style={{ background: "var(--t-panel)", border: "1px solid var(--t-border)", borderRadius: 8, padding: "12px 14px", marginBottom: 16 }}>
          {[["Symbol", symbol], ["Side", side], ["Type", orderType], ["Qty", Number(qty || 0).toLocaleString()], ...(orderType !== "Market" && price ? [["Price", `৳${Number(price).toFixed(2)}`]] : []), ...(total ? [["Total", `৳${total.toLocaleString()}`]] : [])].map(([label, value]) => (
            <div key={label} style={{ display: "flex", justifyContent: "space-between", marginBottom: 6 }}>
              <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{label}</span>
              <span style={{ fontSize: 11, fontWeight: 700, fontFamily: mono, color: label === "Side" ? color : label === "Symbol" ? "var(--t-accent)" : "var(--t-text1)" }}>{value}</span>
            </div>
          ))}
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={onCancel} style={{ flex: 1, padding: "9px", fontSize: 11, fontWeight: 700, fontFamily: mono, background: "transparent", border: "1px solid var(--t-border)", borderRadius: 7, color: "var(--t-text2)", cursor: "pointer" }}>Cancel</button>
          <button onClick={onConfirm} disabled={placing} style={{ flex: 2, padding: "9px", fontSize: 12, fontWeight: 800, fontFamily: mono, background: color, border: "none", borderRadius: 7, color: isBuy ? "#000" : "#fff", cursor: "pointer", opacity: placing ? 0.6 : 1 }}>
            {placing ? "Placing…" : `${side} ${symbol}`}
          </button>
        </div>
      </div>
    </>,
    document.body
  )
}

// ─── Main Widget ──────────────────────────────────────────────────────────────
export function OrderEntryWidget({ colorGroup }: { colorGroup?: string | null }) {
  const { placeOrder, placing } = useOrders()
  const { stocks: _s }          = useMarketData()
  const stocks                  = _s ?? []
  const [_linked]               = useLinkedSymbol(colorGroup ?? null)

  const [side,      setSide]      = useState<"Buy" | "Sell">("Buy")
  const [symbol,    setSymbol]    = useState("")
  const [searchQ,   setSearchQ]   = useState("")
  const [showDrop,  setShowDrop]  = useState(false)
  const [orderType, setOrderType] = useState("Limit")
  const [qty,       setQty]       = useState("")
  const [price,     setPrice]     = useState("")
  const [stop,      setStop]      = useState("")
  const [confirm,   setConfirm]   = useState(false)
  const [warn,      setWarn]      = useState<string | null>(null)
  const [msg,       setMsg]       = useState<{ ok: boolean; text: string } | null>(null)

  useState(() => { if (_linked) setSymbol(_linked) })

  const filtered  = stocks.filter(s => s.tradingCode?.toUpperCase().includes((searchQ || symbol).toUpperCase())).slice(0, 8)
  const liveStock = stocks.find(s => s.tradingCode === symbol)
  const isBuy     = side === "Buy"
  const sideColor = isBuy ? "var(--t-buy)" : "var(--t-sell)"
  const orderVal  = qty && price && orderType !== "Market" ? Number(qty) * Number(price) : null

  const validate = useCallback(() => {
    setWarn(null)
    if (!symbol)                                                   { setWarn("Select a symbol"); return }
    if (!qty || Number(qty) <= 0)                                  { setWarn("Enter quantity"); return }
    if (orderType !== "Market" && (!price || Number(price) <= 0)) { setWarn("Enter price"); return }
    setConfirm(true)
  }, [symbol, qty, price, orderType])

  const submit = useCallback(async () => {
    setConfirm(false)
    const stock = stocks.find(s => s.tradingCode === symbol)
    const res   = await placeOrder({ stockId: stock?.id ?? stock?.stockId ?? 0, orderType: isBuy ? 0 : 1, orderCategory: 0, quantity: Number(qty), limitPrice: orderType !== "Market" ? Number(price) : undefined, stopPrice: stop ? Number(stop) : undefined })
    if (res?.success !== false) {
      setMsg({ ok: true, text: `✓ ${side} ${qty} × ${symbol} placed` })
      setQty(""); setPrice(""); setStop(""); setSymbol(""); setSearchQ("")
    } else {
      setMsg({ ok: false, text: res?.error ?? "Order failed" })
    }
    setTimeout(() => setMsg(null), 4000)
  }, [symbol, side, qty, price, stop, orderType, isBuy, stocks, placeOrder])

  const selectSymbol = (code: string, ltp?: number) => {
    setSymbol(code); setSearchQ(""); setShowDrop(false)
    if (ltp && orderType !== "Market") setPrice(ltp.toFixed(2))
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Filter bar — identical structure to OrderBookWidget ── */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid var(--t-border)", display: "flex", flexDirection: "column", gap: 5, flexShrink: 0, background: "var(--t-panel)" }}>
        {/* Row 1: symbol search + side buttons */}
        <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
          <div style={{ flex: 1, position: "relative" }}>
            <input
              value={searchQ || symbol}
              onChange={e => { setSearchQ(e.target.value); setSymbol(""); setShowDrop(true) }}
              onFocus={e => { setShowDrop(true); e.currentTarget.style.borderColor = "var(--t-accent)"; }}
              onBlur={e => { setTimeout(() => setShowDrop(false), 150); e.currentTarget.style.borderColor = "var(--t-border)"; }}
              placeholder="Symbol e.g. BRACBANK"
              style={{ width: "100%", boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono }}
            />
            {showDrop && filtered.length > 0 && (
              <div style={{ position: "absolute", top: "100%", left: 0, right: 0, background: "var(--t-elevated)", border: "1px solid var(--t-border)", borderRadius: 7, zIndex: 50, boxShadow: "0 8px 24px rgba(0,0,0,0.5)", marginTop: 2 }}>
                {filtered.map(s => (
                  <button key={s.tradingCode} onMouseDown={() => selectSymbol(s.tradingCode, s.lastTradePrice)}
                    style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%", padding: "6px 10px", background: "none", border: "none", borderBottom: "1px solid var(--t-border)", cursor: "pointer" }}
                    onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
                    onMouseLeave={e => e.currentTarget.style.background = "none"}
                  >
                    <span style={{ color: "var(--t-accent)", fontWeight: 700, fontFamily: mono, fontSize: 11 }}>{s.tradingCode}</span>
                    <span style={{ color: "var(--t-text3)", fontFamily: mono, fontSize: 10 }}>৳{(s.lastTradePrice ?? 0).toFixed(2)}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
          {(["Buy", "Sell"] as const).map(s => (
            <button key={s} onClick={() => setSide(s)} style={{ padding: "4px 10px", background: side === s ? (s === "Buy" ? "rgba(0,230,118,0.15)" : "rgba(255,23,68,0.15)") : "none", border: `1px solid ${side === s ? "var(--t-border)" : "transparent"}`, borderRadius: 4, color: side === s ? (s === "Buy" ? "var(--t-buy)" : "var(--t-sell)") : "var(--t-text3)", fontSize: 10, cursor: "pointer", fontFamily: mono, fontWeight: side === s ? 700 : 400 }}>{s}</button>
          ))}
        </div>
        {/* Row 2: order type tabs */}
        <div style={{ display: "flex", gap: 3 }}>
          {ORDER_TYPES.map(t => (
            <button key={t} onClick={() => setOrderType(t)} style={{ padding: "3px 7px", background: orderType === t ? "var(--t-hover)" : "none", border: `1px solid ${orderType === t ? "var(--t-border)" : "transparent"}`, borderRadius: 4, color: orderType === t ? "var(--t-text1)" : "var(--t-text3)", fontSize: 10, cursor: "pointer", fontFamily: mono }}>{t}</button>
          ))}
        </div>
      </div>

      {/* ── Live price strip ── */}
      {liveStock && (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
          {[
            ["LTP",  `৳${liveStock.lastTradePrice?.toFixed(2) ?? "—"}`, "var(--t-accent)"],
            ["CHG%", `${(liveStock.changePercent ?? 0) >= 0 ? "+" : ""}${(liveStock.changePercent ?? 0).toFixed(2)}%`, (liveStock.changePercent ?? 0) >= 0 ? "var(--t-buy)" : "var(--t-sell)"],
            ["HIGH", `৳${liveStock.highPrice?.toFixed(2) ?? "—"}`,  "var(--t-buy)"],
            ["LOW",  `৳${liveStock.lowPrice?.toFixed(2)  ?? "—"}`,  "var(--t-sell)"],
          ].map(([l, v, c]) => (
            <div key={l}>
              <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono }}>{l}</div>
              <div style={{ fontSize: 10, fontWeight: 700, color: c, fontFamily: mono }}>{v}</div>
            </div>
          ))}
        </div>
      )}

      {/* ── Column headers ── */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 72px 80px", gap: 4, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        {["QTY", "PRICE ৳", "VALUE"].map(h => (
          <span key={h} style={{ color: "var(--t-text3)", fontSize: 9, fontFamily: mono, letterSpacing: "0.06em" }}>{h}</span>
        ))}
      </div>

      {/* ── Input row ── */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 72px 80px", gap: 4, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        <input type="number" value={qty} onChange={e => setQty(e.target.value)} min="1" placeholder="0"
          style={{ background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "5px 6px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono, fontWeight: 700, width: "100%", boxSizing: "border-box" }}
          onFocus={e => e.currentTarget.style.borderColor = sideColor}
          onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
        />
        {orderType !== "Market" ? (
          <input type="number" value={price} onChange={e => setPrice(e.target.value)} min="0" step="0.01" placeholder="0.00"
            style={{ background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "5px 6px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono, fontWeight: 700, width: "100%", boxSizing: "border-box" }}
            onFocus={e => e.currentTarget.style.borderColor = sideColor}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
        ) : (
          <div style={{ display: "flex", alignItems: "center", justifyContent: "center", fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>MKT</div>
        )}
        <div style={{ display: "flex", alignItems: "center", fontSize: 10, color: orderVal ? sideColor : "var(--t-text3)", fontFamily: mono, fontWeight: 700 }}>
          {orderVal ? `৳${orderVal >= 1e5 ? `${(orderVal / 1e5).toFixed(1)}L` : orderVal.toLocaleString()}` : "—"}
        </div>
      </div>

      {/* Stop price */}
      {orderType === "Stop" && (
        <div style={{ padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
          <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 3, letterSpacing: "0.06em" }}>STOP PRICE ৳</div>
          <input type="number" value={stop} onChange={e => setStop(e.target.value)}
            style={{ width: "100%", boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 4, padding: "5px 8px", color: "var(--t-text1)", fontSize: 11, outline: "none", fontFamily: mono }}
            onFocus={e => e.currentTarget.style.borderColor = sideColor}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
        </div>
      )}

      {/* Alerts */}
      <div style={{ flex: 1, padding: "5px 8px", overflowY: "auto", display: "flex", flexDirection: "column", gap: 4 }}>
        {warn && <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 5, padding: "5px 8px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>⚠ {warn}</div>}
        {msg  && <div style={{ background: msg.ok ? "rgba(0,230,118,0.08)" : "rgba(255,23,68,0.08)", border: `1px solid ${msg.ok ? "rgba(0,230,118,0.25)" : "rgba(255,23,68,0.25)"}`, borderRadius: 5, padding: "5px 8px", fontSize: 10, color: msg.ok ? "var(--t-buy)" : "var(--t-sell)", fontFamily: mono }}>{msg.text}</div>}
        {liveStock && price && orderType !== "Market" && (() => {
          const p = Number(price), hi = liveStock.circuitBreakerHigh ?? liveStock.highPrice, lo = liveStock.circuitBreakerLow ?? liveStock.lowPrice
          if (hi && p > hi * 1.1) return <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 5, padding: "5px 8px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>⚠ Above circuit breaker high</div>
          if (lo && p < lo * 0.9) return <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 5, padding: "5px 8px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>⚠ Below circuit breaker low</div>
          return null
        })()}
      </div>

      {/* ── Footer submit (matches OrderBookWidget footer) ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", padding: "5px 8px", display: "flex", gap: 8, flexShrink: 0, background: "var(--t-panel)" }}>
        <button onClick={validate} disabled={!symbol || !qty || placing} style={{ flex: 1, padding: "8px", fontSize: 11, fontWeight: 800, fontFamily: mono, background: !symbol || !qty ? "var(--t-hover)" : sideColor, border: "none", borderRadius: 5, color: !symbol || !qty ? "var(--t-text3)" : isBuy ? "#000" : "#fff", cursor: !symbol || !qty ? "not-allowed" : "pointer", letterSpacing: "0.06em", opacity: placing ? 0.7 : 1 }}>
          {placing ? "PLACING…" : `PLACE ${side.toUpperCase()}${symbol ? ` — ${symbol}` : ""}`}
        </button>
      </div>

      {confirm && <ConfirmModal side={side} symbol={symbol} qty={qty} price={price} orderType={orderType} placing={placing} onConfirm={submit} onCancel={() => setConfirm(false)} />}
    </div>
  )
}
