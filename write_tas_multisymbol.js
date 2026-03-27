const fs = require("fs");
const f = "BdStockOMS.Client/src/components/widgets/TimeAndSalesWidget.tsx";
let c = fs.readFileSync(f, "utf8");

// 1. Add pinnedSymbols state after the existing state declarations
c = c.replace(
  "const [connected,  setConnected]    = useState(false)",
  "const [connected,  setConnected]    = useState(false)\n  const [pinnedSymbols, setPinnedSymbols] = useState<string[]>([defaultTradingCode])\n  const [activeSymbolTab, setActiveSymbolTab] = useState(defaultTradingCode)"
);

// 2. Add pin/unpin helpers after handleSearch
c = c.replace(
  "  const patchFilter = (patch: Partial<Filters>) => setFilters(f => ({ ...f, ...patch }))",
  `  const pinSymbol = (code: string) => {
    if (!pinnedSymbols.includes(code)) setPinnedSymbols(p => [...p, code])
    setActiveSymbolTab(code)
    setTradingCode(code)
    setInputCode(code)
  }
  const unpinSymbol = (code: string) => {
    const next = pinnedSymbols.filter(s => s !== code)
    if (next.length === 0) return
    setPinnedSymbols(next)
    if (activeSymbolTab === code) {
      const newActive = next[next.length - 1]
      setActiveSymbolTab(newActive)
      setTradingCode(newActive)
      setInputCode(newActive)
    }
  }
  const patchFilter = (patch: Partial<Filters>) => setFilters(f => ({ ...f, ...patch }))`
);

// 3. Update handleSearch to pin the symbol
c = c.replace(
  "  const handleSearch = () => {\n    const code = inputCode.trim().toUpperCase()\n    if (code) { setTradingCode(code); emitSymbol(code) }\n  }",
  `  const handleSearch = () => {
    const code = inputCode.trim().toUpperCase()
    if (code) { pinSymbol(code); emitSymbol(code) }
  }`
);

// 4. Insert pinned symbol tabs bar after the symbol search bar div (after the "GO" button div)
const tabsBar = `
      {/* ── Pinned Symbol Tabs ── */}
      {pinnedSymbols.length > 0 && (
        <div style={{ display: "flex", gap: 2, padding: "4px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)", overflowX: "auto" }}>
          {pinnedSymbols.map(sym => (
            <div key={sym} style={{ display: "flex", alignItems: "center", gap: 2,
              background: activeSymbolTab === sym ? "rgba(34,211,238,0.12)" : "var(--t-hover)",
              border: \`1px solid \${activeSymbolTab === sym ? "rgba(34,211,238,0.4)" : "var(--t-border)"}\`,
              borderRadius: 5, padding: "2px 6px", cursor: "pointer", flexShrink: 0 }}
              onClick={() => { setActiveSymbolTab(sym); setTradingCode(sym); setInputCode(sym) }}>
              <span style={{ fontSize: 10, fontWeight: 700, fontFamily: mono,
                color: activeSymbolTab === sym ? "#22d3ee" : "var(--t-text2)" }}>{sym}</span>
              {pinnedSymbols.length > 1 && (
                <span onClick={e => { e.stopPropagation(); unpinSymbol(sym) }}
                  style={{ fontSize: 8, color: "var(--t-text3)", cursor: "pointer", marginLeft: 2,
                    lineHeight: 1, padding: "0 1px" }}>✕</span>
              )}
            </div>
          ))}
          <div style={{ display: "flex", alignItems: "center", marginLeft: "auto", flexShrink: 0 }}>
            <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
              {pinnedSymbols.length}/6
            </span>
          </div>
        </div>
      )}`;

// Insert tabs after the symbol search bar closing div
c = c.replace(
  "      {/* ── Collapsible Filter Panel ── */}",
  tabsBar + "\n\n      {/* ── Collapsible Filter Panel ── */}"
);

fs.writeFileSync(f, c);
console.log("TimeAndSalesWidget multi-symbol tabs added");
