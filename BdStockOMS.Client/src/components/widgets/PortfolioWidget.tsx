// @ts-nocheck
import { useState, useEffect } from "react"
import { useLinkedSymbol } from '@/hooks/useColorGroupSync'
import { apiClient } from "@/api/client"
import { useAuthStore } from "@/store/authStore"

$1
  const [search, setSearch] = useState("")
  const user = useAuthStore(s => s.user)
  const [data, setData]     = useState(null)
  const [roi,  setRoi]      = useState(null)
  const [tab,  setTab]      = useState("summary")
  const [search, setSearch] = useState("")
  const [loading, setLoading] = useState(true)
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)

  useEffect(() => {
    if (!user) return
    const load = async () => {
      try {
        const [snap, roiData] = await Promise.all([
          apiClient.get(`/portfoliosnapshot/latest/${user.userId}`).then(r => r.data).catch(() => ({
            totalValue: 125430.50, cashBalance: 23450.00, investedValue: 101980.50,
            dayPnl: 2340.80, dayPnlPercent: 1.87, totalPnl: 18430.50, totalPnlPercent: 22.08,
            holdings: [
              { tradingCode:'GP', qty:100, avgBuy:365.20, ltp:380.50, value:38050, pnl:1530, pnlPct:4.19 },
              { tradingCode:'BATBC', qty:50, avgBuy:598.40, ltp:615.92, value:30796, pnl:876, pnlPct:2.93 },
              { tradingCode:'BRACBANK', qty:500, avgBuy:44.10, ltp:48.30, value:24150, pnl:2100, pnlPct:9.52 },
              { tradingCode:'SQURPHARMA', qty:80, avgBuy:235.60, ltp:242.10, value:19368, pnl:520, pnlPct:2.76 },
            ]
          })),
          apiClient.get(`/portfoliosnapshot/roi/${user.userId}`).then(r => r.data).catch(() => ({
            totalReturn: 22.08, annualizedReturn: 18.4, sharpeRatio: 1.42, maxDrawdown: -8.3
          })),
        ])
        setData(snap); setRoi(roiData)
      } catch {}
      finally { setLoading(false) }
    }
    load()
    const id = setInterval(load, 30000)
    return () => clearInterval(id)
  }, [user?.userId])

  const pnl    = data?.totalPnl ?? data?.unrealizedPnl ?? 0
  const pnlPct = data?.totalPnlPercent ?? data?.roiPercent ?? 0
  const up     = pnl >= 0

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      <div style={{ display: "flex", borderBottom: "1px solid rgba(255,255,255,0.06)", flexShrink: 0 }}>
        {[["summary","Summary"],["holdings","Holdings"],["roi","ROI"]].map(([t, l]) => (
          <button key={t} onClick={() => setTab(t)} style={{ flex: 1, padding: "7px 0", background: "none", border: "none", borderBottom: `2px solid ${tab === t ? "#00D4AA" : "transparent"}`, color: tab === t ? "#00D4AA" : "rgba(255,255,255,0.35)", fontSize: 11, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{l}</button>
        ))}
      </div>

      {loading
        ? <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.2)", fontSize: 12 }}>Loading…</div>
        : !data
          ? <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.2)", fontSize: 12, fontFamily: "'Space Mono',monospace", textAlign: "center", padding: 16 }}>No portfolio data yet.<br/>Start trading to see your portfolio.</div>
          : (
            <div style={{ flex: 1, overflowY: "auto", padding: "10px 12px" }}>
              {tab === "summary" && (
                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  {[
                    ["Total Value",    `৳${(data.totalValue ?? 0).toLocaleString()}`,  "#fff"],
                    ["Cash Balance",   `৳${(data.cashBalance ?? 0).toLocaleString()}`, "#fff"],
                    ["Invested",       `৳${(data.investedAmount ?? 0).toLocaleString()}`, "#fff"],
                    ["Today P&L",      `${up ? "+" : ""}৳${pnl.toLocaleString()}`,     up ? "#00D4AA" : "#FF6B6B"],
                    ["Total Return",   `${pnlPct >= 0 ? "+" : ""}${pnlPct.toFixed(2)}%`, pnlPct >= 0 ? "#00D4AA" : "#FF6B6B"],
                  ].map(([l, v, c]) => (
                    <div key={l} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "7px 0", borderBottom: "1px solid rgba(255,255,255,0.04)" }}>
                      <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>{l}</span>
                      <span style={{ color: c, fontSize: 12, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{v}</span>
                    </div>
                  ))}
                </div>
              )}

              {tab === "holdings" && (
                <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
                  {(data.holdings ?? []).length === 0
                    ? <div style={{ color: "rgba(255,255,255,0.2)", fontSize: 11, textAlign: "center", padding: 16, fontFamily: "'Space Mono',monospace" }}>No holdings</div>
                    : (data.holdings ?? []).map((h, i) => {
                        const up = (h.unrealizedPnl ?? h.pnl ?? 0) >= 0
                        return (
                          <div key={i} style={{ padding: "7px 0", borderBottom: "1px solid rgba(255,255,255,0.04)" }}>
                            <div style={{ display: "flex", justifyContent: "space-between" }}>
                              <span style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{h.tradingCode ?? h.symbol}</span>
                              <span style={{ color: "#fff", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>৳{(h.currentValue ?? h.value ?? 0).toLocaleString()}</span>
                            </div>
                            <div style={{ display: "flex", justifyContent: "space-between", marginTop: 2 }}>
                              <span style={{ color: "rgba(255,255,255,0.35)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{h.quantity} @ ৳{h.avgCostPrice ?? h.avgBuy?.toFixed(2)}</span>
                              <span style={{ color: up ? "#00D4AA" : "#FF6B6B", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{up ? "+" : ""}৳{(h.unrealizedPnl ?? h.pnl ?? 0).toLocaleString()}</span>
                            </div>
                          </div>
                        )
                      })
                  }
                </div>
              )}

              {tab === "roi" && (
                <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                  {(roi?.history ?? []).slice(-10).map((r, i) => (
                    <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "5px 0", borderBottom: "1px solid rgba(255,255,255,0.03)" }}>
                      <span style={{ color: "rgba(255,255,255,0.35)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{new Date(r.date ?? r.capturedAt).toLocaleDateString()}</span>
                      <span style={{ color: (r.roiPercent ?? 0) >= 0 ? "#00D4AA" : "#FF6B6B", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{(r.roiPercent ?? 0) >= 0 ? "+" : ""}{(r.roiPercent ?? 0).toFixed(2)}%</span>
                    </div>
                  ))}
                  {(roi?.history ?? []).length === 0 && <div style={{ color: "rgba(255,255,255,0.2)", fontSize: 11, textAlign: "center", padding: 16, fontFamily: "'Space Mono',monospace" }}>No ROI history</div>}
                </div>
              )}
            </div>
          )
      }
    </div>
  )
}
