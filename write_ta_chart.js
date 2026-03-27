const fs = require("fs");
const f = "BdStockOMS.Client/src/components/widgets/PriceChartWidget.tsx";
let c = fs.readFileSync(f, "utf8");

// Add ComposedChart, Area, Bar to recharts import
c = c.replace(
  'import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, ReferenceLine } from "recharts"',
  'import { LineChart, ComposedChart, Line, Area, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, ReferenceLine } from "recharts"'
);

// Add TA state after existing state
c = c.replace(
  '  const [history, setHistory] = useState([])',
  '  const [history, setHistory] = useState([])\n  const [showTA, setShowTA] = useState<"none"|"rsi"|"macd"|"bb">("none")'
);

// Add TA calculation helpers before the return statement
const taHelpers = `
  // ── TA Calculations ────────────────────────────────────────
  const calcSMA = (data: number[], period: number) =>
    data.map((_, i) => i < period - 1 ? null : data.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period)

  const calcRSI = (prices: number[], period = 14) => {
    if (prices.length < period + 1) return prices.map(() => null)
    return prices.map((_, i) => {
      if (i < period) return null
      const changes = prices.slice(i - period, i).map((p, j) => prices[i - period + j + 1] - p)
      const gains = changes.filter(c => c > 0).reduce((a, b) => a + b, 0) / period
      const losses = Math.abs(changes.filter(c => c < 0).reduce((a, b) => a + b, 0)) / period
      if (losses === 0) return 100
      const rs = gains / losses
      return Math.round((100 - 100 / (1 + rs)) * 100) / 100
    })
  }

  const calcMACD = (prices: number[]) => {
    const ema = (data: number[], n: number) => {
      const k = 2 / (n + 1)
      return data.reduce<number[]>((acc, p, i) => {
        if (i === 0) return [p]
        return [...acc, p * k + acc[i-1] * (1-k)]
      }, [])
    }
    if (prices.length < 26) return prices.map(() => ({ macd: null, signal: null, hist: null }))
    const ema12 = ema(prices, 12)
    const ema26 = ema(prices, 26)
    const macdLine = ema12.map((v, i) => v - ema26[i])
    const signal = ema(macdLine.slice(-9), 9)
    return prices.map((_, i) => {
      const m = macdLine[i] ?? null
      const s = i >= prices.length - 9 ? signal[i - (prices.length - 9)] : null
      return { macd: m ? Math.round(m * 100) / 100 : null, signal: s ? Math.round(s * 100) / 100 : null, hist: m && s ? Math.round((m - s) * 100) / 100 : null }
    })
  }

  const calcBB = (prices: number[], period = 20, stdMult = 2) => {
    const sma = calcSMA(prices, period)
    return prices.map((_, i) => {
      if (i < period - 1) return { upper: null, mid: null, lower: null }
      const slice = prices.slice(i - period + 1, i + 1)
      const mean = sma[i]!
      const std = Math.sqrt(slice.reduce((s, p) => s + (p - mean) ** 2, 0) / period)
      return {
        upper: Math.round((mean + stdMult * std) * 100) / 100,
        mid:   Math.round(mean * 100) / 100,
        lower: Math.round((mean - stdMult * std) * 100) / 100,
      }
    })
  }

  const prices = history.map(h => h.price)
  const rsiData  = useMemo(() => calcRSI(prices), [history])
  const macdData = useMemo(() => calcMACD(prices), [history])
  const bbData   = useMemo(() => calcBB(prices),   [history])

  const chartData = useMemo(() => history.map((h, i) => ({
    ...h,
    rsi:    rsiData[i],
    macd:   macdData[i]?.macd,
    signal: macdData[i]?.signal,
    hist:   macdData[i]?.hist,
    bbUpper: showTA === "bb" ? bbData[i]?.upper : null,
    bbMid:   showTA === "bb" ? bbData[i]?.mid   : null,
    bbLower: showTA === "bb" ? bbData[i]?.lower  : null,
  })), [history, rsiData, macdData, bbData, showTA])
`;

// Insert TA helpers before the return
c = c.replace(
  "\n  return (\n    <div style={{ height: \"100%\"",
  taHelpers + "\n  return (\n    <div style={{ height: \"100%\""
);

// Add TA selector buttons to the toolbar (after period selector)
c = c.replace(
  "        {stock && (\n          <div style={{ display: \"flex\", gap: 6, alignItems: \"center\" }}>",
  `        <div style={{ display: "flex", gap: 2, marginLeft: "auto" }}>
          {(["none","rsi","macd","bb"] as const).map(ta => (
            <button key={ta} onClick={() => setShowTA(ta)}
              style={{ padding: "3px 7px", fontSize: 9, fontFamily: "'Space Mono',monospace", fontWeight: 600,
                background: showTA === ta ? "rgba(34,211,238,0.12)" : "none",
                border: \`1px solid \${showTA === ta ? "rgba(34,211,238,0.4)" : "rgba(255,255,255,0.06)"}\`,
                borderRadius: 4, color: showTA === ta ? "#22d3ee" : "rgba(255,255,255,0.3)", cursor: "pointer" }}>
              {ta === "none" ? "OFF" : ta.toUpperCase()}
            </button>
          ))}
        </div>
        {stock && (
          <div style={{ display: "flex", gap: 6, alignItems: "center" }}>`
);

// Add TA panel below the main chart — find the closing of the main chart ResponsiveContainer
// and add the TA panel after it
const taPanel = `
          {/* ── TA Indicator Panel ── */}
          {showTA !== "none" && (
            <div style={{ height: 100, flexShrink: 0, borderTop: "1px solid rgba(255,255,255,0.06)", paddingTop: 4 }}>
              <div style={{ fontSize: 8, color: "rgba(255,255,255,0.3)", fontFamily: "'Space Mono',monospace", paddingLeft: 8, marginBottom: 2 }}>
                {showTA === "rsi" ? "RSI (14)" : showTA === "macd" ? "MACD (12,26,9)" : "Bollinger Bands (20,2)"}
              </div>
              <ResponsiveContainer width="100%" height={84}>
                {showTA === "rsi" ? (
                  <LineChart data={chartData} margin={{ left: 0, right: 8, top: 0, bottom: 0 }}>
                    <XAxis dataKey="t" hide />
                    <YAxis domain={[0, 100]} tick={{ fontSize: 8, fill: "rgba(255,255,255,0.3)" }} width={28} />
                    <Tooltip contentStyle={{ background: "var(--t-elevated)", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 6, fontSize: 10 }} />
                    <ReferenceLine y={70} stroke="rgba(255,107,107,0.4)" strokeDasharray="3 3" />
                    <ReferenceLine y={30} stroke="rgba(0,212,170,0.4)" strokeDasharray="3 3" />
                    <Line type="monotone" dataKey="rsi" dot={false} stroke="#FFB800" strokeWidth={1.5} connectNulls />
                  </LineChart>
                ) : showTA === "macd" ? (
                  <ComposedChart data={chartData} margin={{ left: 0, right: 8, top: 0, bottom: 0 }}>
                    <XAxis dataKey="t" hide />
                    <YAxis tick={{ fontSize: 8, fill: "rgba(255,255,255,0.3)" }} width={28} />
                    <Tooltip contentStyle={{ background: "var(--t-elevated)", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 6, fontSize: 10 }} />
                    <ReferenceLine y={0} stroke="rgba(255,255,255,0.1)" />
                    <Bar dataKey="hist" fill="#64B4FF" opacity={0.5} />
                    <Line type="monotone" dataKey="macd"   dot={false} stroke="#22d3ee" strokeWidth={1.5} connectNulls />
                    <Line type="monotone" dataKey="signal" dot={false} stroke="#FF6B6B" strokeWidth={1}   connectNulls />
                  </ComposedChart>
                ) : (
                  <LineChart data={chartData} margin={{ left: 0, right: 8, top: 0, bottom: 0 }}>
                    <XAxis dataKey="t" hide />
                    <YAxis domain={["auto","auto"]} tick={{ fontSize: 8, fill: "rgba(255,255,255,0.3)" }} width={28} />
                    <Tooltip contentStyle={{ background: "var(--t-elevated)", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 6, fontSize: 10 }} />
                    <Line type="monotone" dataKey="bbUpper" dot={false} stroke="rgba(255,184,0,0.5)"  strokeWidth={1} strokeDasharray="3 3" connectNulls />
                    <Line type="monotone" dataKey="bbMid"   dot={false} stroke="rgba(255,255,255,0.3)" strokeWidth={1} connectNulls />
                    <Line type="monotone" dataKey="bbLower" dot={false} stroke="rgba(255,184,0,0.5)"  strokeWidth={1} strokeDasharray="3 3" connectNulls />
                  </LineChart>
                )}
              </ResponsiveContainer>
            </div>
          )}`;

// Insert TA panel before the last </div> of the widget
c = c.replace(
  /(\s+<\/div>\s*\)\s*\}\s*\nexport default)/,
  taPanel + "\n$1"
);

fs.writeFileSync(f, c);
console.log("PriceChartWidget TA indicators added");
