interface StatusBadgeProps { status: number | string; showDot?: boolean }
interface SideBadgeProps   { side: number | string }

const BY_NUM: Record<number, [string, string]> = {
  0:  ["Pending",    "#f59e0b"],
  1:  ["Open",       "#38bdf8"],
  2:  ["Partial",    "#818cf8"],
  3:  ["Filled",     "#10b981"],
  4:  ["Completed",  "#2dd4bf"],
  5:  ["Cancelled",  "#71717a"],
  6:  ["Rejected",   "#ef4444"],
  7:  ["Queued",     "#a78bfa"],
  8:  ["Submitted",  "#6366f1"],
  9:  ["Waiting",    "#f97316"],
  10: ["CxlReq",     "#fb7185"],
  11: ["EditReq",    "#22d3ee"],
  12: ["Deleted",    "#52525b"],
  13: ["Replaced",   "#94a3b8"],
  14: ["Private",    "#c084fc"],
}

const BY_STR: Record<string, [string, string]> = {
  "Pending":          ["Pending",   "#f59e0b"],
  "Open":             ["Open",      "#38bdf8"],
  "PartiallyFilled":  ["Partial",   "#818cf8"],
  "Partial":          ["Partial",   "#818cf8"],
  "Filled":           ["Filled",    "#10b981"],
  "Completed":        ["Completed", "#2dd4bf"],
  "Cancelled":        ["Cancelled", "#71717a"],
  "Rejected":         ["Rejected",  "#ef4444"],
  "Queued":           ["Queued",    "#a78bfa"],
  "Submitted":        ["Submitted", "#6366f1"],
  "Waiting":          ["Waiting",   "#f97316"],
  "CancelRequested":  ["CxlReq",   "#fb7185"],
  "EditRequested":    ["EditReq",   "#22d3ee"],
  "Deleted":          ["Deleted",   "#52525b"],
  "Replaced":         ["Replaced",  "#94a3b8"],
  "Private":          ["Private",   "#c084fc"],
}

const PULSE = new Set(["Pending","Open","Partial","PartiallyFilled","Queued","Submitted","Waiting","CancelRequested","EditRequested"])

export function OrderStatusBadge({ status, showDot = true }: StatusBadgeProps) {
  const isNum = typeof status === "number" || (typeof status === "string" && !isNaN(Number(status)))
  const entry = isNum ? BY_NUM[Number(status)] : BY_STR[String(status)]
  const label = entry ? entry[0] : String(status)
  const color = entry ? entry[1] : "#71717a"
  const pulse = isNum ? [0,1,2,7,8,9,10,11].includes(Number(status)) : PULSE.has(String(status))
  return (
    <span style={{
      display: "inline-flex", alignItems: "center", gap: 4,
      padding: "2px 7px", borderRadius: 20,
      background: color + "22",
      border: "1px solid " + color + "55",
      color, fontSize: 10, fontWeight: 600,
      whiteSpace: "nowrap",
    }}>
      {showDot && <span style={{ width:5, height:5, borderRadius:"50%", background:color, flexShrink:0 }} />}
      {label}
    </span>
  )
}

export function OrderSideBadge({ side }: SideBadgeProps) {
  const isBuy = side === 0 || String(side).toLowerCase() === "buy"
  return (
    <span style={{
      display: "inline-flex", alignItems: "center",
      padding: "2px 6px", borderRadius: 3,
      background: isBuy ? "rgba(0,212,170,0.15)" : "rgba(255,107,107,0.15)",
      border: "1px solid " + (isBuy ? "rgba(0,212,170,0.35)" : "rgba(255,107,107,0.35)"),
      color: isBuy ? "#00d4aa" : "#ff6b6b",
      fontSize: 10, fontWeight: 700, letterSpacing: "0.04em",
    }}>
      {isBuy ? "BUY" : "SELL"}
    </span>
  )
}
