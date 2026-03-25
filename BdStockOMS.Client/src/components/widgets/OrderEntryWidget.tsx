// @ts-nocheck
// src/components/widgets/OrderEntryWidget.tsx
// Day 63 redesign — matches OMS design language: JetBrains Mono, var(--t-*) tokens,
// RMS validation, live price, circuit breaker display, portal confirm

import { useState, useCallback } from "react"
import { createPortal } from "react-dom"
import { useOrders } from "@/hooks/useOrders"
import { useMarketData } from "@/hooks/useMarketData"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

const mono = "'JetBrains Mono', monospace"

const ORDER_TYPES = ["Limit", "Market", "Stop"]
const ORDER_CATS  = [
  { value: 0, label: "Regular" },
  { value: 1, label: "Odd Lot" },
  { value: 2, label: "Block"   },
]

// ─── Confirm Portal ───────────────────────────────────────────────────────────
function ConfirmModal({ side, symbol, qty, price, orderType, onConfirm, onCancel, placing }: any) {
  const isBuy   = side === "Buy"
  const color   = isBuy ? "var(--t-buy)" : "var(--t-sell)"
  const total   = orderType !== "Market" ? Number(qty) * Number(price) : null

  return createPortal(
    <>
      <div onClick={onCancel} style={{ position: "fixed", inset: 0, zIndex: 9997, background: "rgba(0,0,0,0.6)", backdropFilter: "blur(4px)" }} />
      <div style={{
        position: "fixed", top: "50%", left: "50%", zIndex: 9998,
        transform: "translate(-50%, -50%)",
        background: "var(--t-elevated)", border: `1px solid ${color}30`,
        borderRadius: 12, padding: "20px 24px", minWidth: 300, maxWidth: 380,
        boxShadow: `0 24px 64px rgba(0,0,0,0.6), 0 0 0 1px ${color}15`,
      }}>
        {/* Header */}
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 16 }}>
          <div style={{ width: 32, height: 32, borderRadius: 8, background: `${color}15`, border: `1px solid ${color}30`, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 14, fontWeight: 800, color, fontFamily: mono }}>
            {isBuy ? "B" : "S"}
          </div>
          <div>
            <div style={{ fontSize: 13, fontWeight: 800, color: "var(--t-text1)", fontFamily: mono }}>Confirm Order</div>
            <div style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{orderType} · Press Enter to confirm</div>
          </div>
        </div>

        {/* Order details */}
        <div style={{ background: "var(--t-panel)", border: "1px solid var(--t-border)", borderRadius: 8, padding: "12px 14px", marginBottom: 16 }}>
          {[
            ["Symbol",  symbol],
            ["Side",    side],
            ["Type",    orderType],
            ["Qty",     Number(qty).toLocaleString()],
            ...(orderType !== "Market" ? [["Price", `৳${Number(price).toFixed(2)}`]] : []),
            ...(total ? [["Total", `৳${total.toLocaleString()}`]] : []),
          ].map(([label, value]) => (
            <div key={label} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6 }}>
              <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{label}</span>
              <span style={{ fontSize: 11, fontWeight: 700, color: label === "Side" ? color : label === "Symbol" ? "var(--t-accent)" : "var(--t-text1)", fontFamily: mono }}>{value}</span>
            </div>
          ))}
        </div>

        {/* Buttons */}
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={onCancel} style={{
            flex: 1, padding: "9px", fontSize: 11, fontWeight: 700, fontFamily: mono,
            background: "transparent", border: "1px solid var(--t-border)", borderRadius: 7,
            color: "var(--t-text2)", cursor: "pointer",
          }}>Cancel</button>
          <button onClick={onConfirm} disabled={placing} style={{
            flex: 2, padding: "9px", fontSize: 12, fontWeight: 800, fontFamily: mono,
            background: color, border: "none", borderRadius: 7,
            color: isBuy ? "#000" : "#fff", cursor: "pointer", opacity: placing ? 0.6 : 1,
          }}>
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
  const [symbol,    setSymbol]    = useState(_linked ?? "")
  const [searchQ,   setSearchQ]   = useState("")
  const [showDrop,  setShowDrop]  = useState(false)
  const [orderType, setOrderType] = useState("Limit")
  const [orderCat,  setOrderCat]  = useState(0)
  const [qty,       setQty]       = useState("")
  const [price,     setPrice]     = useState("")
  const [stop,      setStop]      = useState("")
  const [confirm,   setConfirm]   = useState(false)
  const [rmsWarn,   setRmsWarn]   = useState<string | null>(null)
  const [msg,       setMsg]       = useState<{ ok: boolean; text: string } | null>(null)

  // Sync from color group
  useState(() => { if (_linked) { setSymbol(_linked); setSearchQ("") } })

  const filtered   = stocks.filter(s => s.tradingCode?.includes((searchQ || symbol).toUpperCase())).slice(0, 8)
  const liveStock  = stocks.find(s => s.tradingCode === symbol)
  const isBuy      = side === "Buy"
  const sideColor  = isBuy ? "var(--t-buy)" : "var(--t-sell)"
  const orderValue = qty && price && orderType !== "Market" ? Number(qty) * Number(price) : null

  const validate = useCallback(() => {
    setRmsWarn(null)
    if (!symbol) { setRmsWarn("Select a symbol"); return }
    if (!qty || Number(qty) <= 0) { setRmsWarn("Enter a valid quantity"); return }
    if (orderType !== "Market" && (!price || Number(price) <= 0)) { setRmsWarn("Enter a valid price"); return }
    setConfirm(true)
  }, [symbol, qty, price, orderType])

  const submit = useCallback(async () => {
    setConfirm(false)
    const stock = stocks.find(s => s.tradingCode === symbol)
    const res = await placeOrder({
      stockId:      stock?.id ?? stock?.stockId ?? 0,
      orderType:    isBuy ? 0 : 1,
      orderCategory: orderCat,
      quantity:     Number(qty),
      limitPrice:   orderType !== "Market" ? Number(price) : undefined,
      stopPrice:    stop ? Number(stop) : undefined,
    })
    if (res?.success !== false) {
      setMsg({ ok: true, text: `✓ ${side} ${qty} × ${symbol} placed` })
      setQty(""); setPrice(""); setStop("")
    } else {
      setMsg({ ok: false, text: res?.error ?? "Order failed" })
    }
    setTimeout(() => setMsg(null), 4000)
  }, [symbol, side, qty, price, stop, orderType, orderCat, isBuy, stocks, placeOrder])

  const selectSymbol = (code: string, ltp?: number) => {
    setSymbol(code); setSearchQ(""); setShowDrop(false)
    if (ltp) setPrice(ltp.toFixed(2))
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Side Toggle Header ── */}
      <div style={{ display: "flex", flexShrink: 0 }}>
        {(["Buy", "Sell"] as const).map(s => (
          <button key={s} onClick={() => setSide(s)} style={{
            flex: 1, padding: "10px 0", fontSize: 12, fontWeight: 800, fontFamily: mono,
            border: "none", cursor: "pointer", transition: "all 0.12s",
            background: side === s
              ? (s === "Buy" ? "rgba(0,230,118,0.15)" : "rgba(255,23,68,0.15)")
              : "var(--t-panel)",
            color: side === s
              ? (s === "Buy" ? "var(--t-buy)" : "var(--t-sell)")
              : "var(--t-text3)",
            borderBottom: `2px solid ${side === s ? (s === "Buy" ? "var(--t-buy)" : "var(--t-sell)") : "var(--t-border)"}`,
          }}>{s.toUpperCase()}</button>
        ))}
      </div>

      {/* ── Form Body ── */}
      <div style={{ flex: 1, overflowY: "auto", padding: "10px 12px", display: "flex", flexDirection: "column", gap: 8 }}>

        {/* Symbol search */}
        <div>
          <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 4, letterSpacing: "0.08em" }}>SYMBOL</div>
          <div style={{ position: "relative" }}>
            <input
              value={searchQ || symbol}
              onChange={e => { setSearchQ(e.target.value); setSymbol(""); setShowDrop(true) }}
              onFocus={() => setShowDrop(true)}
              onBlur={() => setTimeout(() => setShowDrop(false), 150)}
              placeholder="e.g. BRACBANK"
              style={{
                width: "100%", boxSizing: "border-box",
                background: "var(--t-hover)", border: "1px solid var(--t-border)",
                borderRadius: 6, padding: "8px 10px",
                color: "var(--t-text1)", fontSize: 12, outline: "none", fontFamily: mono,
                fontWeight: 700,
              }}
              onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
              onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
            />
            {showDrop && filtered.length > 0 && (
              <div style={{
                position: "absolute", top: "100%", left: 0, right: 0,
                background: "var(--t-elevated)", border: "1px solid var(--t-border)",
                borderRadius: 7, zIndex: 20, boxShadow: "0 8px 24px rgba(0,0,0,0.5)", marginTop: 2,
              }}>
                {filtered.map(s => (
                  <button key={s.id ?? s.tradingCode} onMouseDown={() => selectSymbol(s.tradingCode, s.lastTradePrice)}
                    style={{
                      display: "flex", alignItems: "center", justifyContent: "space-between",
                      width: "100%", padding: "7px 12px", background: "none", border: "none",
                      cursor: "pointer", borderBottom: "1px solid var(--t-border)",
                    }}
                    onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
                    onMouseLeave={e => e.currentTarget.style.background = "none"}
                  >
                    <span style={{ color: "var(--t-accent)", fontWeight: 700, fontFamily: mono, fontSize: 11 }}>{s.tradingCode}</span>
                    <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                      <span style={{ color: "var(--t-text3)", fontSize: 9 }}>{s.category}</span>
                      <span style={{ color: "var(--t-text2)", fontFamily: mono, fontSize: 10 }}>৳{(s.lastTradePrice ?? 0).toFixed(2)}</span>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Live price strip */}
        {liveStock && (
          <div style={{
            display: "flex", justifyContent: "space-between", alignItems: "center",
            background: "var(--t-panel)", border: "1px solid var(--t-border)",
            borderRadius: 6, padding: "6px 10px",
          }}>
            <div style={{ display: "flex", gap: 12 }}>
              <div>
                <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono, marginBottom: 1 }}>LTP</div>
                <div style={{ fontSize: 13, fontWeight: 800, color: "var(--t-accent)", fontFamily: mono }}>৳{liveStock.lastTradePrice?.toFixed(2)}</div>
              </div>
              <div>
                <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono, marginBottom: 1 }}>CHG%</div>
                <div style={{ fontSize: 11, fontWeight: 700, fontFamily: mono, color: (liveStock.changePercent ?? 0) >= 0 ? "var(--t-buy)" : "var(--t-sell)" }}>
                  {(liveStock.changePercent ?? 0) >= 0 ? "+" : ""}{(liveStock.changePercent ?? 0).toFixed(2)}%
                </div>
              </div>
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <div style={{ textAlign: "right" }}>
                <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono }}>H</div>
                <div style={{ fontSize: 9, color: "var(--t-buy)", fontFamily: mono }}>৳{liveStock.highPrice?.toFixed(2) ?? "—"}</div>
              </div>
              <div style={{ textAlign: "right" }}>
                <div style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono }}>L</div>
                <div style={{ fontSize: 9, color: "var(--t-sell)", fontFamily: mono }}>৳{liveStock.lowPrice?.toFixed(2) ?? "—"}</div>
              </div>
            </div>
          </div>
        )}

        {/* Order type */}
        <div>
          <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 4, letterSpacing: "0.08em" }}>ORDER TYPE</div>
          <div style={{ display: "flex", gap: 4 }}>
            {ORDER_TYPES.map(t => (
              <button key={t} onClick={() => setOrderType(t)} style={{
                flex: 1, padding: "6px 0", fontSize: 10, fontWeight: 700, fontFamily: mono,
                background: orderType === t ? "var(--t-hover)" : "transparent",
                border: `1px solid ${orderType === t ? "var(--t-accent)" : "var(--t-border)"}`,
                borderRadius: 5, color: orderType === t ? "var(--t-accent)" : "var(--t-text3)", cursor: "pointer",
              }}>{t}</button>
            ))}
          </div>
        </div>

        {/* Qty + Price grid */}
        <div style={{ display: "grid", gridTemplateColumns: orderType === "Market" ? "1fr" : "1fr 1fr", gap: 8 }}>
          <div>
            <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 4, letterSpacing: "0.08em" }}>QUANTITY</div>
            <input type="number" value={qty} onChange={e => setQty(e.target.value)} min="1" style={{
              width: "100%", boxSizing: "border-box",
              background: "var(--t-hover)", border: "1px solid var(--t-border)",
              borderRadius: 6, padding: "8px 10px", color: "var(--t-text1)",
              fontSize: 12, outline: "none", fontFamily: mono, fontWeight: 700,
            }}
              onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
              onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
            />
          </div>
          {orderType !== "Market" && (
            <div>
              <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 4, letterSpacing: "0.08em" }}>PRICE ৳</div>
              <input type="number" value={price} onChange={e => setPrice(e.target.value)} min="0" step="0.01" style={{
                width: "100%", boxSizing: "border-box",
                background: "var(--t-hover)", border: "1px solid var(--t-border)",
                borderRadius: 6, padding: "8px 10px", color: "var(--t-text1)",
                fontSize: 12, outline: "none", fontFamily: mono, fontWeight: 700,
              }}
                onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
                onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
              />
            </div>
          )}
        </div>

        {/* Stop price */}
        {orderType === "Stop" && (
          <div>
            <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 4, letterSpacing: "0.08em" }}>STOP PRICE ৳</div>
            <input type="number" value={stop} onChange={e => setStop(e.target.value)} style={{
              width: "100%", boxSizing: "border-box",
              background: "var(--t-hover)", border: "1px solid var(--t-border)",
              borderRadius: 6, padding: "8px 10px", color: "var(--t-text1)",
              fontSize: 12, outline: "none", fontFamily: mono,
            }}
              onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
              onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
            />
          </div>
        )}

        {/* Order value display */}
        {orderValue && (
          <div style={{
            display: "flex", justifyContent: "space-between", alignItems: "center",
            background: `${sideColor}08`, border: `1px solid ${sideColor}20`,
            borderRadius: 6, padding: "6px 10px",
          }}>
            <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, letterSpacing: "0.06em" }}>ORDER VALUE</span>
            <span style={{ fontSize: 13, fontWeight: 800, color: sideColor, fontFamily: mono }}>
              ৳{orderValue.toLocaleString()}
            </span>
          </div>
        )}

        {/* Circuit breaker warning */}
        {liveStock && price && orderType !== "Market" && (
          (() => {
            const p = Number(price)
            const hi = liveStock.circuitBreakerHigh ?? liveStock.highPrice
            const lo = liveStock.circuitBreakerLow  ?? liveStock.lowPrice
            if (hi && p > hi * 1.1) return (
              <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 6, padding: "6px 10px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>
                ⚠ Price above circuit breaker high
              </div>
            )
            if (lo && p < lo * 0.9) return (
              <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 6, padding: "6px 10px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>
                ⚠ Price below circuit breaker low
              </div>
            )
            return null
          })()
        )}

        {/* RMS warning */}
        {rmsWarn && (
          <div style={{ background: "rgba(245,158,11,0.08)", border: "1px solid rgba(245,158,11,0.25)", borderRadius: 6, padding: "7px 10px", fontSize: 10, color: "#F59E0B", fontFamily: mono }}>
            ⚠ {rmsWarn}
          </div>
        )}

        {/* Success / error message */}
        {msg && (
          <div style={{
            background: msg.ok ? "rgba(0,230,118,0.08)" : "rgba(255,23,68,0.08)",
            border: `1px solid ${msg.ok ? "rgba(0,230,118,0.25)" : "rgba(255,23,68,0.25)"}`,
            borderRadius: 6, padding: "7px 10px", fontSize: 10,
            color: msg.ok ? "var(--t-buy)" : "var(--t-sell)", fontFamily: mono,
          }}>
            {msg.text}
          </div>
        )}
      </div>

      {/* ── Submit Button ── */}
      <div style={{ padding: "10px 12px", borderTop: "1px solid var(--t-border)", background: "var(--t-panel)", flexShrink: 0 }}>
        <button
          onClick={validate}
          disabled={!symbol || !qty || placing}
          style={{
            width: "100%", padding: "11px", fontSize: 13, fontWeight: 800, fontFamily: mono,
            background: !symbol || !qty ? "var(--t-hover)" : sideColor,
            border: "none", borderRadius: 8, cursor: !symbol || !qty ? "not-allowed" : "pointer",
            color: !symbol || !qty ? "var(--t-text3)" : isBuy ? "#000" : "#fff",
            transition: "all 0.12s", letterSpacing: "0.06em",
            opacity: placing ? 0.7 : 1,
          }}
        >
          {placing ? "PLACING…" : `${side.toUpperCase()} ${symbol || "—"}`}
        </button>
      </div>

      {/* Confirm modal via portal */}
      {confirm && (
        <ConfirmModal
          side={side} symbol={symbol} qty={qty} price={price}
          orderType={orderType} placing={placing}
          onConfirm={submit}
          onCancel={() => setConfirm(false)}
        />
      )}
    </div>
  )
}
