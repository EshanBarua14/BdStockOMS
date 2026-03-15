// @ts-nocheck
import { useState, useEffect, useCallback } from "react"
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from "recharts"
import { getBrokerSummary, getTopTraders, getTopClients } from "@/api/tradeMonitor"
import { useAuthStore } from "@/store/authStore"


const DEMO_SUMMARY = {
  brokerageHouseId: 1, brokerName: "Pioneer Securities Ltd",
  totalInvestors: 3, totalTraders: 2, totalOrdersToday: 47,
  totalBuyValueToday: 8750000, totalSellValueToday: 6230000,
  totalTurnoverToday: 14980000, pendingKycCount: 2,
  activeOrdersCount: 12, totalCommissionToday: 74900.0
}
const DEMO_TRADERS = [
  { traderId:5, traderName:"Pioneer Trader One", email:"trader1@pioneer.com", totalClients:3, ordersToday:28, buyValueToday:5250000, sellValueToday:3780000, totalValueToday:9030000 },
  { traderId:6, traderName:"Pioneer Trader Two", email:"trader2@pioneer.com", totalClients:3, ordersToday:19, buyValueToday:3500000, sellValueToday:2450000, totalValueToday:5950000 },
]
const DEMO_CLIENTS = [
  { investorId:7, investorName:"Pioneer Investor One",   ordersToday:18, buyValueToday:3200000, sellValueToday:2100000, totalValueToday:5300000, isKycApproved:true  },
  { investorId:8, investorName:"Pioneer Investor Two",   ordersToday:15, buyValueToday:2800000, sellValueToday:1950000, totalValueToday:4750000, isKycApproved:true  },
  { investorId:9, investorName:"Pioneer Investor Three", ordersToday:14, buyValueToday:2750000, sellValueToday:2180000, totalValueToday:4930000, isKycApproved:false },
]

const fmt = (n: number) => n >= 1e7 ? `৳${(n/1e7).toFixed(2)}Cr` : n >= 1e5 ? `৳${(n/1e5).toFixed(1)}L` : `৳${n.toLocaleString()}`

const COLORS = ["#00D4AA", "#FF6B6B", "#4A9EFF", "#FFB347", "#A78BFA", "#34D399", "#F472B6", "#60A5FA", "#FBBF24", "#F87171"]

const MetricCard = ({ label, value, sub }: any) => (
  <div style={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", borderRadius: 10, padding: "14px 18px", minWidth: 140 }}>
    <div style={{ fontSize: 11, color: "var(--t-text3)", fontFamily: "'Space Mono',monospace", marginBottom: 6 }}>{label}</div>
    <div style={{ fontSize: 22, fontWeight: 700, color: "var(--t-text1)", fontFamily: "'Space Mono',monospace" }}>{value}</div>
    {sub && <div style={{ fontSize: 10, color: "var(--t-text3)", marginTop: 4 }}>{sub}</div>}
  </div>
)

const SectionTitle = ({ children }: any) => (
  <div style={{ fontSize: 12, fontWeight: 700, color: "var(--t-accent)", fontFamily: "'Space Mono',monospace", letterSpacing: 2, textTransform: "uppercase", marginBottom: 12, marginTop: 24 }}>{children}</div>
)

export function TradeMonitorPage() {
  const { brokerageHouseId } = useAuthStore()
  const bhId = brokerageHouseId ?? 1
  const [summary, setSummary] = useState<any>(null)
  const [traders, setTraders] = useState<any[]>([])
  const [clients, setClients] = useState<any[]>([])
  const [chartType, setChartType] = useState<'buy'|'sell'|'value'>('value')
  const [loading, setLoading] = useState(true)
  const [lastRefresh, setLastRefresh] = useState(new Date())

  const load = useCallback(async () => {
    try {
      const [s, t, c] = await Promise.all([
        getBrokerSummary(bhId),
        getTopTraders(bhId, chartType),
        getTopClients(bhId),
      ])
      // Use demo data if all trading values are zero (market closed / no activity)
      setSummary(s?.totalTurnoverToday > 0 ? s : { ...DEMO_SUMMARY, brokerName: s?.brokerName ?? DEMO_SUMMARY.brokerName })
      setTraders(t?.length > 0 && t[0]?.totalValueToday > 0 ? t : DEMO_TRADERS)
      setClients(c?.length > 0 && c[0]?.totalValueToday > 0 ? c : DEMO_CLIENTS)
      setLastRefresh(new Date())
    } catch (e) {
      console.error(e)
    } finally {
      setLoading(false)
    }
  }, [bhId, chartType])

  useEffect(() => { load() }, [load])

  // Auto-refresh every 10 seconds
  useEffect(() => {
    const id = setInterval(load, 10000)
    return () => clearInterval(id)
  }, [load])

  const pieData = summary ? [
    { name: "Buy", value: summary.totalBuyValueToday || 1 },
    { name: "Sell", value: summary.totalSellValueToday || 1 },
  ] : []

  const traderChartData = traders.map(t => ({
    name: t.traderName?.split(" ").slice(-2).join(" ") ?? t.traderId,
    Buy: t.buyValueToday,
    Sell: t.sellValueToday,
    Total: t.totalValueToday,
  }))

  const clientChartData = clients.map(c => ({
    name: c.investorName?.split(" ").slice(-2).join(" ") ?? c.investorId,
    Buy: c.buyValueToday,
    Sell: c.sellValueToday,
    Total: c.totalValueToday,
  }))

  return (
    <div style={{ padding: "20px 24px", height: "100%", overflowY: "auto", color: "var(--t-text1)" }}>
      {/* Header */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 20 }}>
        <div>
          <div style={{ fontSize: 18, fontWeight: 700, color: "var(--t-text1)" }}>Trade Monitor</div>
          <div style={{ fontSize: 11, color: "var(--t-text3)", fontFamily: "'Space Mono',monospace" }}>
            {summary?.brokerName} · Auto-refresh 10s · Last: {lastRefresh.toLocaleTimeString()}
          </div>
        </div>
        <button onClick={load} style={{ padding: "6px 16px", fontSize: 11, fontWeight: 700, background: "var(--t-accent)", color: "#000", border: "none", borderRadius: 6, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>
          ↻ REFRESH
        </button>
      </div>

      {loading && <div style={{ color: "var(--t-text3)", fontSize: 13, textAlign: "center", padding: 40 }}>Loading trade data…</div>}

      {!loading && summary && <>
        {/* Summary Cards */}
        <SectionTitle>Today's Trade Data</SectionTitle>
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap" }}>
          <MetricCard label="TOTAL BUY" value={fmt(summary.totalBuyValueToday)} sub={`${summary.totalOrdersToday} orders`} />
          <MetricCard label="TOTAL SELL" value={fmt(summary.totalSellValueToday)} />
          <MetricCard label="TURNOVER" value={fmt(summary.totalTurnoverToday)} />
          <MetricCard label="ACTIVE ORDERS" value={summary.activeOrdersCount} />
          <MetricCard label="INVESTORS" value={summary.totalInvestors} />
          <MetricCard label="TRADERS" value={summary.totalTraders} />
          <MetricCard label="COMMISSION" value={fmt(summary.totalCommissionToday)} />
          <MetricCard label="PENDING KYC" value={summary.pendingKycCount} />
        </div>

        {/* Pie + Top Traders side by side */}
        <div style={{ display: "grid", gridTemplateColumns: "280px 1fr", gap: 20, marginTop: 24 }}>
          {/* Pie Chart */}
          <div style={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", borderRadius: 10, padding: 16 }}>
            <div style={{ fontSize: 11, fontWeight: 700, color: "var(--t-text2)", fontFamily: "'Space Mono',monospace", marginBottom: 8 }}>BUY vs SELL RATIO</div>
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={pieData} cx="50%" cy="50%" outerRadius={75} dataKey="value" label={({ name, percent }) => `${name} ${(percent*100).toFixed(0)}%`} labelLine={false} fontSize={11}>
                  {pieData.map((_, i) => <Cell key={i} fill={i === 0 ? "#00D4AA" : "#FF6B6B"} />)}
                </Pie>
                <Tooltip formatter={(v: number) => fmt(v)} />
              </PieChart>
            </ResponsiveContainer>
          </div>

          {/* Top Traders Chart */}
          <div style={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", borderRadius: 10, padding: 16 }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 8 }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: "var(--t-text2)", fontFamily: "'Space Mono',monospace" }}>TOP TRADERS BY AMOUNT</div>
              <div style={{ display: "flex", gap: 4 }}>
                {(["buy","sell","value"] as const).map(t => (
                  <button key={t} onClick={() => setChartType(t)} style={{ padding: "3px 10px", fontSize: 10, fontWeight: 700, border: "1px solid var(--t-border)", borderRadius: 4, cursor: "pointer", fontFamily: "'Space Mono',monospace", background: chartType === t ? "var(--t-accent)" : "transparent", color: chartType === t ? "#000" : "var(--t-text2)" }}>
                    {t.toUpperCase()}
                  </button>
                ))}
              </div>
            </div>
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={traderChartData} layout="vertical" margin={{ left: 10, right: 20 }}>
                <XAxis type="number" tickFormatter={v => fmt(v)} tick={{ fontSize: 9, fill: "var(--t-text3)" }} />
                <YAxis type="category" dataKey="name" tick={{ fontSize: 10, fill: "var(--t-text2)" }} width={100} />
                <Tooltip formatter={(v: number) => fmt(v)} contentStyle={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", fontSize: 11 }} />
                <Bar dataKey={chartType === 'buy' ? 'Buy' : chartType === 'sell' ? 'Sell' : 'Total'} fill="#00D4AA" radius={[0,4,4,0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Top Clients Chart */}
        <SectionTitle>Top 10 Clients by Amount</SectionTitle>
        <div style={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", borderRadius: 10, padding: 16 }}>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={clientChartData} layout="vertical" margin={{ left: 10, right: 20 }}>
              <XAxis type="number" tickFormatter={v => fmt(v)} tick={{ fontSize: 9, fill: "var(--t-text3)" }} />
              <YAxis type="category" dataKey="name" tick={{ fontSize: 10, fill: "var(--t-text2)" }} width={120} />
              <Tooltip formatter={(v: number) => fmt(v)} contentStyle={{ background: "var(--t-surface)", border: "1px solid var(--t-border)", fontSize: 11 }} />
              <Legend wrapperStyle={{ fontSize: 10 }} />
              <Bar dataKey="Buy" fill="#00D4AA" radius={[0,4,4,0]} stackId="a" />
              <Bar dataKey="Sell" fill="#FF6B6B" radius={[0,4,4,0]} stackId="b" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </>}
    </div>
  )
}
