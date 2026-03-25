// @ts-nocheck
import { useState } from "react"
import { useOrders } from "@/hooks/useOrders"
import { rmsApi } from "@/api/rms"
import { useMarketData } from "@/hooks/useMarketData"

export function OrderEntryWidget() {
  const { placeOrder, placing } = useOrders()
  const { stocks: _s } = useMarketData()
  const stocks = _s ?? []
  const [side, setSide]       = useState("Buy")
  const [symbol, setSymbol]   = useState("")
  const [type, setType]       = useState("Limit")
  const [qty, setQty]         = useState("")
  const [price, setPrice]     = useState("")
  const [stop, setStop]       = useState("")
  const [confirm, setConfirm] = useState(false)
  const [rmsWarn, setRmsWarn] = useState(null)
  const [msg, setMsg]         = useState(null)
  const [searchQ, setSearchQ] = useState("")
  const [showDrop, setShowDrop] = useState(false)

  const filtered = stocks.filter(s => s.tradingCode?.includes(searchQ.toUpperCase())).slice(0, 6)
  const liveStock = stocks.find(s => s.tradingCode === symbol)

  const validate = async () => {
    setRmsWarn(null)
    try {
      const res = await rmsApi.validateOrder({ symbol, side, type, quantity: Number(qty), price: Number(price) })
      if (!res.isValid) { setRmsWarn(res.message ?? "RMS validation failed"); return }
    } catch {}
    setConfirm(true)
  }

  const submit = async () => {
    setConfirm(false)
    const res = await placeOrder({ symbol, side, type, quantity: Number(qty), price: type !== "Market" ? Number(price) : undefined, stopPrice: stop ? Number(stop) : undefined })
    setMsg(res.success ? { ok: true, text: `✓ ${side} ${qty} ${symbol} placed` } : { ok: false, text: res.error })
    setTimeout(() => setMsg(null), 4000)
  }

  const btn = (label, val, col) => (
    <button onClick={() => setSide(val)} style={{ flex: 1, padding: "8px 0", background: side === val ? col : "rgba(255,255,255,0.04)", border: `1px solid ${side === val ? col : "rgba(255,255,255,0.08)"}`, borderRadius: 6, color: side === val ? "#000" : "rgba(255,255,255,0.5)", fontWeight: 700, fontSize: 12, cursor: "pointer", transition: "all 0.15s" }}>{label}</button>
  )

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: 'var(--t-surface)', padding: "10px 12px", gap: 8, overflowY: "auto" }}>
      <div style={{ color: 'var(--t-text2)', fontSize: 10, fontFamily: "'Space Mono',monospace", letterSpacing: "0.1em" }}>ORDER ENTRY</div>

      {/* Buy/Sell toggle */}
      <div style={{ display: "flex", gap: 6 }}>
        {btn("BUY", "Buy", "#00D4AA")}
        {btn("SELL", "Sell", "#FF6B6B")}
      </div>

      {/* Symbol */}
      <div style={{ position: "relative" }}>
        <input value={searchQ || symbol} onChange={e => { setSearchQ(e.target.value); setSymbol(""); setShowDrop(true) }}
          onFocus={() => setShowDrop(true)} onBlur={() => setTimeout(() => setShowDrop(false), 150)}
          placeholder="Symbol e.g. BATBC" style={{ width: "100%", boxSizing: "border-box", background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 6, padding: "8px 10px", color: 'var(--t-text1)', fontSize: 12, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        {showDrop && filtered.length > 0 && (
          <div style={{ position: "absolute", top: "100%", left: 0, right: 0, background: 'var(--t-elevated)', border: '1px solid var(--t-border)', borderRadius: 6, zIndex: 20 }}>
            {filtered.map(s => (
              <button key={s.id} onMouseDown={() => { setSymbol(s.tradingCode); setSearchQ(""); setShowDrop(false); setPrice((s.lastTradePrice ?? 0).toFixed(2)) }}
                style={{ display: "block", width: "100%", padding: "6px 10px", background: "none", border: "none", color: 'var(--t-text1)', fontSize: 11, cursor: "pointer", textAlign: "left", fontFamily: "'Space Mono',monospace" }}>
                <span style={{ color: "#00D4AA" }}>{s.tradingCode}</span> <span style={{ color: "rgba(255,255,255,0.35)", fontSize: 10 }}>৳{(s.lastTradePrice ?? 0).toFixed(2)}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Live price */}
      {liveStock && (
        <div style={{ display: "flex", justifyContent: "space-between", background: "rgba(0,212,170,0.05)", border: "1px solid rgba(0,212,170,0.1)", borderRadius: 5, padding: "4px 8px" }}>
          <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>LTP</span>
          <span style={{ color: "#00D4AA", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>৳{liveStock.lastTradePrice?.toFixed(2)}</span>
        </div>
      )}

      {/* Order type */}
      <div style={{ display: "flex", gap: 4 }}>
        {["Market","Limit","StopLoss"].map(t => (
          <button key={t} onClick={() => setType(t)} style={{ flex: 1, padding: "5px 0", background: type === t ? "rgba(255,255,255,0.1)" : "none", border: `1px solid ${type === t ? "rgba(255,255,255,0.2)" : "rgba(255,255,255,0.06)"}`, borderRadius: 5, color: type === t ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{t}</button>
        ))}
      </div>

      {/* Qty + Price */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 6 }}>
        <div>
          <div style={{ color: 'var(--t-text3)', fontSize: 10, marginBottom: 3, fontFamily: "'Space Mono',monospace" }}>QTY</div>
          <input type="number" value={qty} onChange={e => setQty(e.target.value)} style={{ width: "100%", boxSizing: "border-box", background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 5, padding: "7px 8px", color: 'var(--t-text1)', fontSize: 12, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        </div>
        {type !== "Market" && (
          <div>
            <div style={{ color: 'var(--t-text3)', fontSize: 10, marginBottom: 3, fontFamily: "'Space Mono',monospace" }}>PRICE ৳</div>
            <input type="number" value={price} onChange={e => setPrice(e.target.value)} style={{ width: "100%", boxSizing: "border-box", background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 5, padding: "7px 8px", color: 'var(--t-text1)', fontSize: 12, outline: "none", fontFamily: "'Space Mono',monospace" }} />
          </div>
        )}
      </div>
      {type === "StopLoss" && (
        <div>
          <div style={{ color: 'var(--t-text3)', fontSize: 10, marginBottom: 3, fontFamily: "'Space Mono',monospace" }}>STOP PRICE ৳</div>
          <input type="number" value={stop} onChange={e => setStop(e.target.value)} style={{ width: "100%", boxSizing: "border-box", background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 5, padding: "7px 8px", color: 'var(--t-text1)', fontSize: 12, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        </div>
      )}

      {/* Order value */}
      {qty && price && (
        <div style={{ background: "rgba(255,255,255,0.03)", borderRadius: 5, padding: "5px 8px", display: "flex", justifyContent: "space-between" }}>
          <span style={{ color: 'var(--t-text3)', fontSize: 10, fontFamily: "'Space Mono',monospace" }}>ORDER VALUE</span>
          <span style={{ color: 'var(--t-text1)', fontSize: 11, fontFamily: "'Space Mono',monospace" }}>৳{(Number(qty) * Number(price)).toLocaleString()}</span>
        </div>
      )}

      {rmsWarn && <div style={{ background: "rgba(245,158,11,0.1)", border: "1px solid rgba(245,158,11,0.3)", borderRadius: 5, padding: "6px 8px", color: "#F59E0B", fontSize: 11 }}>{rmsWarn}</div>}
      {msg && <div style={{ background: msg.ok ? "rgba(0,212,170,0.1)" : "rgba(255,107,107,0.1)", border: `1px solid ${msg.ok ? "rgba(0,212,170,0.3)" : "rgba(255,107,107,0.3)"}`, borderRadius: 5, padding: "6px 8px", color: msg.ok ? "#00D4AA" : "#FF6B6B", fontSize: 11 }}>{msg.text}</div>}

      <button onClick={validate} disabled={!symbol || !qty || placing} style={{ padding: "10px", background: side === "Buy" ? "#00D4AA" : "#FF6B6B", border: "none", borderRadius: 7, color: "#000", fontWeight: 700, fontSize: 13, cursor: "pointer", marginTop: "auto" }}>
        {placing ? "Placing…" : `${side} ${symbol || "—"}`}
      </button>

      {/* Confirm modal */}
      {confirm && (
        <div style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,0.7)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 9999 }}>
          <div style={{ background: 'var(--t-surface)', border: "1px solid rgba(0,212,170,0.2)", borderRadius: 12, padding: "24px 28px", minWidth: 280 }}>
            <div style={{ color: 'var(--t-text1)', fontSize: 15, fontWeight: 700, marginBottom: 8 }}>Confirm Order</div>
            <div style={{ color: 'var(--t-text2)', fontSize: 13, marginBottom: 16, fontFamily: "'Space Mono',monospace", lineHeight: 1.8 }}>
              {side} {qty} × {symbol}<br/>
              {type !== "Market" && <>Price: ৳{price}<br/></>}
              Type: {type}
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={submit} style={{ flex: 1, padding: "9px", background: side === "Buy" ? "#00D4AA" : "#FF6B6B", border: "none", borderRadius: 7, color: "#000", fontWeight: 700, cursor: "pointer" }}>Confirm</button>
              <button onClick={() => setConfirm(false)} style={{ flex: 1, padding: "9px", background: "rgba(255,255,255,0.06)", border: '1px solid var(--t-border)', borderRadius: 7, color: "rgba(255,255,255,0.6)", cursor: "pointer" }}>Cancel</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
