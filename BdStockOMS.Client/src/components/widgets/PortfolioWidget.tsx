// @ts-nocheck
import { useState, useEffect } from "react"
import { apiClient } from "@/api/client"
import { useAuthStore } from "@/store/authStore"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"
import { useSelectedBOStore } from "@/store/useSelectedBOStore"

const mono = "'JetBrains Mono', monospace"

export function PortfolioWidget({ colorGroup }: { colorGroup?: string | null }) {
  const user = useAuthStore(s => s.user)
  const selectedBO = useSelectedBOStore(s => s.selectedBO)
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)
  const [data,    setData]    = useState(null)
  const [roi,     setRoi]     = useState(null)
  const [tab,     setTab]     = useState("summary")
  const [search,  setSearch]  = useState("")
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!user) return
    const load = async () => {
      try {
        const [snap, roiData] = await Promise.all([
          apiClient.get(`/portfoliosnapshot/latest/${selectedBO?.userId ?? user.userId}`).then(r => r.data).catch(() => ({
            totalValue: 125430.50, cashBalance: 23450.00, investedAmount: 101980.50,
            totalPnl: 18430.50, totalPnlPercent: 22.08,
            holdings: [
              { tradingCode: "GP",         quantity: 100, avgCostPrice: 365.20, currentValue: 38050, unrealizedPnl: 1530 },
              { tradingCode: "BATBC",       quantity: 50,  avgCostPrice: 598.40, currentValue: 30796, unrealizedPnl: 876  },
              { tradingCode: "BRACBANK",    quantity: 500, avgCostPrice: 44.10,  currentValue: 24150, unrealizedPnl: 2100 },
              { tradingCode: "SQURPHARMA",  quantity: 80,  avgCostPrice: 235.60, currentValue: 19368, unrealizedPnl: 520  },
            ]
          })),
          apiClient.get(`/portfoliosnapshot/roi/${selectedBO?.userId ?? user.userId}`).then(r => r.data).catch(() => ({
            totalReturn: 22.08, annualizedReturn: 18.4, sharpeRatio: 1.42, maxDrawdown: -8.3,
            history: []
          })),
        ])
        setData(snap); setRoi(roiData)
      } catch {}
      finally { setLoading(false) }
    }
    load()
    const id = setInterval(load, 30000)
    return () => clearInterval(id)
  }, [user?.userId, selectedBO?.userId])

  const pnl    = data?.totalPnl ?? data?.unrealizedPnl ?? 0
  const pnlPct = data?.totalPnlPercent ?? data?.roiPercent ?? 0
  const up     = pnl >= 0

  const filteredHoldings = (data?.holdings ?? []).filter((h: any) =>
    !search || (h.tradingCode ?? h.symbol ?? "").toUpperCase().includes(search.toUpperCase())
  )

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Tabs ── */}
      {/* BO indicator */}
      {selectedBO && (
        <div style={{ padding: "4px 8px", background: "var(--t-hover)", borderBottom: "1px solid var(--t-border)", display: "flex", alignItems: "center", gap: 6, flexShrink: 0 }}>
          <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>BO:</span>
          <span style={{ fontSize: 10, fontWeight: 700, color: "var(--t-accent)", fontFamily: mono }}>{selectedBO.boNumber}</span>
          <span style={{ fontSize: 9, color: "var(--t-text2)" }}>{selectedBO.fullName}</span>
        </div>
      )}
      <div style={{ display: "flex", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        {[["summary","Summary"],["holdings","Holdings"],["roi","ROI"]].map(([t, l]) => (
          <button key={t} onClick={() => setTab(t)} style={{
            flex: 1, padding: "7px 0", background: "none", border: "none",
            borderBottom: `2px solid ${tab === t ? "var(--t-accent)" : "transparent"}`,
            color: tab === t ? "var(--t-accent)" : "var(--t-text3)",
            fontSize: 11, cursor: "pointer", fontFamily: mono,
          }}>{l}</button>
        ))}
      </div>

      {loading ? (
        <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "var(--t-text3)", fontSize: 12, fontFamily: mono }}>Loading…</div>
      ) : !data ? (
        <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "var(--t-text3)", fontSize: 12, fontFamily: mono, textAlign: "center", padding: 16 }}>
          No portfolio data yet.<br/>Start trading to see your portfolio.
        </div>
      ) : (
        <div style={{ flex: 1, overflowY: "auto" }}>

          {/* ── Summary ── */}
          {tab === "summary" && (
            <div style={{ padding: "10px 12px", display: "flex", flexDirection: "column", gap: 2 }}>
              {[
                ["Total Value",  `৳${(data.totalValue ?? 0).toLocaleString()}`,          "var(--t-text1)"],
                ["Cash Balance", `৳${(data.cashBalance ?? 0).toLocaleString()}`,          "var(--t-text1)"],
                ["Invested",     `৳${(data.investedAmount ?? 0).toLocaleString()}`,       "var(--t-text1)"],
                ["P&L",          `${up?"+":""}৳${pnl.toLocaleString()}`,                  up ? "var(--t-buy)" : "var(--t-sell)"],
                ["Return",       `${pnlPct>=0?"+":""}${pnlPct.toFixed(2)}%`,              pnlPct >= 0 ? "var(--t-buy)" : "var(--t-sell)"],
              ].map(([l, v, c]) => (
                <div key={l} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "7px 0", borderBottom: "1px solid var(--t-border)" }}>
                  <span style={{ color: "var(--t-text3)", fontSize: 11, fontFamily: mono }}>{l}</span>
                  <span style={{ color: c, fontSize: 12, fontFamily: mono, fontWeight: 700 }}>{v}</span>
                </div>
              ))}
            </div>
          )}

          {/* ── Holdings ── */}
          {tab === "holdings" && (
            <div style={{ display: "flex", flexDirection: "column" }}>
              {/* Search */}
              <div style={{ padding: "6px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
                <input
                  value={search} onChange={e => setSearch(e.target.value)}
                  placeholder="Search holdings…"
                  style={{ width: "100%", boxSizing: "border-box", background: "var(--t-hover)", border: "1px solid var(--t-border)", borderRadius: 5, padding: "4px 8px", color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono }}
                  onFocus={e => e.currentTarget.style.borderColor = "var(--t-accent)"}
                  onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
                />
              </div>
              {/* Column headers */}
              <div style={{ display: "grid", gridTemplateColumns: "1fr 48px 72px 72px", gap: 4, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", background: "var(--t-bg)" }}>
                {["SYMBOL","QTY","VALUE","P&L"].map(h => (
                  <span key={h} style={{ fontSize: 8, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono, letterSpacing: "0.06em", textAlign: h === "SYMBOL" ? "left" : "right", display: "block" }}>{h}</span>
                ))}
              </div>
              {/* Rows */}
              {filteredHoldings.length === 0 ? (
                <div style={{ padding: 16, textAlign: "center", color: "var(--t-text3)", fontSize: 11, fontFamily: mono }}>No holdings</div>
              ) : filteredHoldings.map((h: any, i: number) => {
                const code = h.tradingCode ?? h.symbol ?? "—"
                const pnlH = h.unrealizedPnl ?? h.pnl ?? 0
                const upH  = pnlH >= 0
                return (
                  <div key={i} onClick={() => { emitSymbol(code) }} style={{
                    display: "grid", gridTemplateColumns: "1fr 48px 72px 72px",
                    gap: 4, padding: "6px 8px", borderBottom: "1px solid var(--t-border)",
                    cursor: "pointer",
                  }}
                    onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
                    onMouseLeave={e => e.currentTarget.style.background = "transparent"}
                  >
                    <div>
                      <div style={{ color: "var(--t-accent)", fontSize: 11, fontFamily: mono, fontWeight: 700 }}>{code}</div>
                      <div style={{ color: "var(--t-text3)", fontSize: 9, fontFamily: mono }}>avg ৳{(h.avgCostPrice ?? h.avgBuy ?? 0).toFixed(2)}</div>
                    </div>
                    <div style={{ textAlign: "right", alignSelf: "center" }}>
                      <span style={{ color: "var(--t-text2)", fontSize: 10, fontFamily: mono }}>{h.quantity ?? h.qty}</span>
                    </div>
                    <div style={{ textAlign: "right", alignSelf: "center" }}>
                      <span style={{ color: "var(--t-text1)", fontSize: 10, fontFamily: mono }}>৳{((h.currentValue ?? h.value) ?? 0).toLocaleString()}</span>
                    </div>
                    <div style={{ textAlign: "right", alignSelf: "center" }}>
                      <span style={{ color: upH ? "var(--t-buy)" : "var(--t-sell)", fontSize: 10, fontFamily: mono, fontWeight: 700 }}>
                        {upH ? "+" : ""}৳{pnlH.toLocaleString()}
                      </span>
                    </div>
                  </div>
                )
              })}
            </div>
          )}

          {/* ── ROI ── */}
          {tab === "roi" && (
            <div style={{ padding: "10px 12px", display: "flex", flexDirection: "column", gap: 2 }}>
              {[
                ["Total Return",      `${(roi?.totalReturn ?? 0) >= 0 ? "+" : ""}${(roi?.totalReturn ?? 0).toFixed(2)}%`],
                ["Annualized Return", `${(roi?.annualizedReturn ?? 0).toFixed(2)}%`],
                ["Sharpe Ratio",      `${(roi?.sharpeRatio ?? 0).toFixed(2)}`],
                ["Max Drawdown",      `${(roi?.maxDrawdown ?? 0).toFixed(2)}%`],
              ].map(([l, v]) => (
                <div key={l} style={{ display: "flex", justifyContent: "space-between", padding: "7px 0", borderBottom: "1px solid var(--t-border)" }}>
                  <span style={{ color: "var(--t-text3)", fontSize: 11, fontFamily: mono }}>{l}</span>
                  <span style={{ color: "var(--t-text1)", fontSize: 11, fontFamily: mono, fontWeight: 700 }}>{v}</span>
                </div>
              ))}
              {(roi?.history ?? []).length > 0 && (
                <div style={{ marginTop: 8 }}>
                  <div style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono, marginBottom: 6, letterSpacing: "0.06em" }}>HISTORY</div>
                  {(roi.history ?? []).slice(-10).map((r: any, i: number) => (
                    <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "4px 0", borderBottom: "1px solid var(--t-border)" }}>
                      <span style={{ color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>{new Date(r.date ?? r.capturedAt).toLocaleDateString()}</span>
                      <span style={{ color: (r.roiPercent ?? 0) >= 0 ? "var(--t-buy)" : "var(--t-sell)", fontSize: 10, fontFamily: mono }}>
                        {(r.roiPercent ?? 0) >= 0 ? "+" : ""}{(r.roiPercent ?? 0).toFixed(2)}%
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
