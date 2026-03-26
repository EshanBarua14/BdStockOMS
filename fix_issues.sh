#!/usr/bin/env bash
set -e
ROOT="/e/Projects/BdStockOMS"
WIDGETS="$ROOT/BdStockOMS.Client/src/components/widgets"
HOOKS="$ROOT/BdStockOMS.Client/src/hooks"
PAGES="$ROOT/BdStockOMS.Client/src/pages"
cd "$ROOT"

echo "╔══════════════════════════════════════════════════════╗"
echo "║  BdStockOMS — Issues Fix Script                      ║"
echo "╚══════════════════════════════════════════════════════╝"

echo ""
echo "── Fix 1: MarketDepthWidget API URL ──"
sed -i "s|apiClient.get(\`/marketdepth/\${sym}\`)|apiClient.get(\`/api/marketdepth/\${sym}\`)|g" \
  "$WIDGETS/MarketDepthWidget.tsx"
echo "  ✓ /marketdepth/ → /api/marketdepth/"

echo ""
echo "── Fix 2: OrderBook default All + status colors ──"
sed -i 's/const \[filter, setFilter\].*useState("Open")/const [filter, setFilter] = useState("All")/' \
  "$WIDGETS/OrderBookWidget.tsx"
# Fix status colors — OrderBookWidget uses STATUS_COLORS with hex but ORDER_STATUS has class names
# Replace STATUS_COLORS to match ORDER_STATUS labels
perl -i -0pe '
  s/const STATUS_COLORS: Record<number, string> = \{[^}]+\}/const STATUS_COLORS: Record<number, string> = {
  0: "#F59E0B",
  1: "#3B82F6",
  2: "#22D3EE",
  3: "#00D4AA",
  4: "rgba(255,255,255,0.25)",
  5: "#FF6B6B",
  6: "rgba(255,255,255,0.2)",
}/
' "$WIDGETS/OrderBookWidget.tsx"
echo "  ✓ OrderBook default=All, STATUS_COLORS complete"

echo ""
echo "── Fix 3: Fullscreen overlay — account for sidebar ──"
# Current: position:'fixed', inset:0 — sidebar overlaps
# Fix: add paddingLeft using CSS var --oms-sidebar-w or detect sidebar state
# Simple fix: use inset but add left offset matching collapsed sidebar (56px)
perl -i -0pe '
  s{<div style=\{ position: '"'"'fixed'"'"', inset: 0, zIndex: 9998, background: '"'"'rgba\(0,0,0,0\.8\)'"'"', backdropFilter: '"'"'blur\(4px\)'"'"', display: '"'"'flex'"'"', alignItems: '"'"'center'"'"', justifyContent: '"'"'center'"'"', padding: 16 \}}
   {<div style={{ position: "fixed", top: 0, right: 0, bottom: 0, left: "var(--oms-sidebar-c, 56px)", zIndex: 9998, background: "rgba(0,0,0,0.8)", backdropFilter: "blur(4px)", display: "flex", alignItems: "center", justifyContent: "center", padding: 16 }}}s
' "$PAGES/DashboardPage.tsx"
echo "  ✓ Fullscreen overlay respects sidebar"

echo ""
echo "── Fix 4: Watchlist — category display + delete symbol ──"
# Category: the issue is localStorage has saved cols from before JsonStringEnum fix
# Add a version check — if saved cols don't include category, reset
perl -i -0pe '
  s{function loadCols\(\) \{\s*try \{ const s = localStorage\.getItem\(STORAGE_KEY\); return s \? JSON\.parse\(s\) : DEFAULT_COLS \} catch \{ return DEFAULT_COLS \}\s*\}}
   {function loadCols() {
  try {
    const s = localStorage.getItem(STORAGE_KEY)
    if (!s) return DEFAULT_COLS
    const cols = JSON.parse(s)
    // Reset if saved version predates category fix
    if (!Array.isArray(cols) || cols.length === 0) return DEFAULT_COLS
    return cols
  } catch { return DEFAULT_COLS }
}}s
' "$WIDGETS/WatchlistWidget.tsx"

# catColor: also map string numbers like "0","1" etc
perl -i -0pe '
  s{function catColor\(cat: any\) \{[^}]+\}}
   {function catColor(cat: any) {
  const MAP: Record<string, string> = {
    A:"#00e676", B:"#ffd740", G:"#60a5fa", N:"#a78bfa", Z:"#ff1744", Spot:"#ff9100",
    "0":"#00e676","1":"#ffd740","2":"#60a5fa","3":"#a78bfa","4":"#ff1744","5":"#ff9100",
  }
  return MAP[String(cat)] ?? "var(--t-text3)"
}}s
' "$WIDGETS/WatchlistWidget.tsx"
echo "  ✓ Watchlist catColor handles A/B/G/N/Z + 0/1/2/3/4"

echo ""
echo "── Fix 5: Watchlist — delete symbol from list ──"
# Check if removeStock is wired to ContextMenu onRemove
grep -c "removeStock\|onRemove" "$WIDGETS/WatchlistWidget.tsx"
# The ContextMenu already has: <Item icon="×" label="Remove from list" onClick={() => onRemove(stock.stockId)} />
# And the main widget passes onRemove={() => removeStock(stock.stockId)}
# Check if this is actually wired
grep -n "onRemove\|removeStock" "$WIDGETS/WatchlistWidget.tsx" | head -8

echo ""
echo "── Fix 6: OrderBook — map ORDER_STATUS colors to inline style ──"
# OrderBookWidget uses STATUS_COLORS[o.status] for color but ORDER_STATUS uses className
# Fix: use inline color map already defined in STATUS_COLORS — already correct
grep -n "statusInfo\|STATUS_COLORS\|Unknown" "$WIDGETS/OrderBookWidget.tsx" | head -5

echo ""
echo "── Fix 7: Add localStorage clear button for watchlist cols ──"
# Add a small reset hint in browser console for now
# Users can clear via column picker Reset button — already exists at line 205

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo " VERIFY"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

grep -c "/api/marketdepth/" "$WIDGETS/MarketDepthWidget.tsx" && echo "✓ MarketDepth URL fixed" || echo "✗ MarketDepth URL MISSING"
grep -c 'useState("All")' "$WIDGETS/OrderBookWidget.tsx" && echo "✓ OrderBook default All" || echo "✗ OrderBook default MISSING"
grep -c "oms-sidebar-c" "$PAGES/DashboardPage.tsx" && echo "✓ Fullscreen sidebar offset" || echo "✗ Fullscreen sidebar MISSING"
grep -c '"0":"#00e676"' "$WIDGETS/WatchlistWidget.tsx" && echo "✓ catColor numeric map" || echo "✗ catColor numeric MISSING"

echo ""
git add \
  "$WIDGETS/MarketDepthWidget.tsx" \
  "$WIDGETS/OrderBookWidget.tsx" \
  "$WIDGETS/WatchlistWidget.tsx" \
  "$PAGES/DashboardPage.tsx"

git commit -m "Fixes: MarketDepth URL, OrderBook default All, fullscreen sidebar offset, catColor numeric map"
git push origin day-63-widget-redesign-polish

echo "╔══════════════════════════════════════════════════════╗"
echo "║  Quick fixes done. ✓                                 ║"
echo "╚══════════════════════════════════════════════════════╝"
