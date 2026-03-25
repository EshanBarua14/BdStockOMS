const DEMO_RMS = {
  cashLimit: 500000, purchasePower: 387450.20, usedPurchasePower: 112549.80,
  mainMarket: { maxBuy: 300000, maxSell: 300000, consumed: 87450, remaining: 212550 },
  atbMarket:  { maxBuy: 100000, maxSell: 100000, consumed: 25099, remaining: 74901 },
  scMarket:   { maxBuy:  50000, maxSell:  50000, consumed:     0, remaining: 50000 },
  suspendBuy: false, suspendSell: false, shortSellEnabled: false,
}
// @ts-nocheck
import { useState, useEffect } from "react"
import { useNavigate } from "react-router-dom"
import { rmsApi } from "@/api/rms"

function GaugeBar({ label, used, total, color }: any) {
  const pct = total > 0 ? Math.min(100, (used / total) * 100) : 0
  const warn = pct > 80
  return (
    <div style={{ marginBottom: 10 }}>
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 3 }}>
        <span style={{ color: "rgba(255,255,255,0.4)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{label}</span>
        <span style={{ color: warn ? "#FF6B6B" : "#fff", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{pct.toFixed(1)}%</span>
      </div>
      <div style={{ height: 6, background: "rgba(255,255,255,0.06)", borderRadius: 3, overflow: "hidden" }}>
        <div style={{ width: `${pct}%`, height: "100%", background: warn ? "#FF6B6B" : color, borderRadius: 3, transition: "width 0.4s" }} />
      </div>
      <div style={{ display: "flex", justifyContent: "space-between", marginTop: 2 }}>
        <span style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>Used: ৳{(used ?? 0).toLocaleString()}</span>
        <span style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>Limit: ৳{(total ?? 0).toLocaleString()}</span>
      </div>
    </div>
  )
}

export function RMSLimitsWidget() {
  const navigate = useNavigate()
  const [limits, setLimits] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      try { setLimits(await rmsApi.getMyLimits()) } catch { setLimits(DEMO_RMS) }
      finally { setLoading(false) }
    }
    load()
    const id = setInterval(load, 15000)
    return () => clearInterval(id)
  }, [])

  const l = limits ?? {}
  const marginUsed  = l.marginUsed  ?? l.usedMargin  ?? 0
  const marginLimit = l.marginLimit ?? l.totalMargin ?? 1000000
  const dayBought   = l.dayBuyValue  ?? l.totalBuyToday  ?? 0
  const dayLimit    = l.dayBuyLimit  ?? l.dailyBuyLimit  ?? 5000000
  const exposure    = l.exposure     ?? l.totalExposure  ?? 0
  const expLimit    = l.exposureLimit ?? l.maxExposure   ?? 10000000

  const riskScore = marginLimit > 0 ? Math.min(100, (marginUsed / marginLimit) * 100) : 0
  const riskColor = riskScore > 80 ? "#FF6B6B" : riskScore > 60 ? "#F59E0B" : "#00D4AA"
  const riskLabel = riskScore > 80 ? "HIGH RISK" : riskScore > 60 ? "MODERATE" : "LOW RISK"

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", justifyContent: "space-between", alignItems: "center", flexShrink: 0 }}>
        <span style={{ color: "rgba(255,255,255,0.5)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>RMS LIMITS</span>
        <span style={{ color: riskColor, fontSize: 10, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>{riskLabel}</span>
      </div>

      {loading
        ? <div style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.2)", fontSize: 12 }}>Loading…</div>
        : (
          <div style={{ flex: 1, overflowY: "auto", padding: "10px 12px" }}>
            {/* Risk gauge */}
            <div style={{ textAlign: "center", marginBottom: 12 }}>
              <svg width="100" height="60" viewBox="0 0 100 60">
                <path d="M 10 55 A 40 40 0 0 1 90 55" fill="none" stroke="rgba(255,255,255,0.06)" strokeWidth="8" strokeLinecap="round" />
                <path d={`M 10 55 A 40 40 0 0 1 ${10 + 80 * Math.cos(Math.PI - (riskScore / 100) * Math.PI)} ${55 + 40 * Math.sin(Math.PI - (riskScore / 100) * Math.PI) * -1}`}
                  fill="none" stroke={riskColor} strokeWidth="8" strokeLinecap="round" style={{ transition: "all 0.5s" }} />
                <text x="50" y="55" textAnchor="middle" fill={riskColor} fontSize="12" fontFamily="Space Mono" fontWeight="bold">{riskScore.toFixed(0)}%</text>
              </svg>
            </div>

            <GaugeBar label="Margin Used"    used={marginUsed} total={marginLimit} color="#00D4AA" />
            <GaugeBar label="Day Buy Value"  used={dayBought}  total={dayLimit}    color="#3B82F6" />
            <GaugeBar label="Exposure"       used={exposure}   total={expLimit}    color="#8B5CF6" />

            {/* Quick stats */}
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 6, marginTop: 6 }}>
              {[
                ["Available Margin", `৳${(marginLimit - marginUsed).toLocaleString()}`, "#00D4AA"],
                ["Cash Balance",     `৳${(l.cashBalance ?? 0).toLocaleString()}`,       "#fff"],
                ["Open Orders",      l.openOrderCount ?? "—",                            "#F59E0B"],
                ["Buying Power",     `৳${(l.buyingPower ?? marginLimit - marginUsed).toLocaleString()}`, "#3B82F6"],
              ].map(([lbl, val, c]) => (
                <div key={lbl} style={{ background: "rgba(255,255,255,0.03)", borderRadius: 6, padding: "6px 8px", border: "1px solid rgba(255,255,255,0.05)" }}>
                  <div style={{ color: "rgba(255,255,255,0.3)", fontSize: 9, fontFamily: "'Space Mono',monospace" }}>{lbl}</div>
                  <div style={{ color: c, fontSize: 11, fontFamily: "'Space Mono',monospace", fontWeight: 700, marginTop: 2 }}>{val}</div>
                </div>
              ))}
            </div>
          <button onClick={() => navigate('/rms')} style={{
            margin: '8px 12px 4px', width: 'calc(100% - 24px)', padding: '7px 0', fontSize: 10, fontWeight: 700,
            border: '1px solid var(--t-accent)', borderRadius: 6, cursor: 'pointer',
            background: 'transparent', color: 'var(--t-accent)', fontFamily: "'Space Mono',monospace",
          }}>VIEW FULL RMS →</button>
          </div>
        )
      }
    </div>
  )
}
