import React from 'react'

interface StatusBadgeProps { status: number | string; showDot?: boolean }
interface SideBadgeProps   { side: number | string }

const STATUS_CONFIG: Record<number, { label: string; pill: string; dot: string }> = {
  0: { label: 'Pending',   pill: 'bg-amber-500/15 text-amber-400 border border-amber-500/30',       dot: 'bg-amber-400 animate-pulse'  },
  1: { label: 'Open',      pill: 'bg-sky-500/15 text-sky-400 border border-sky-500/30',             dot: 'bg-sky-400 animate-pulse'    },
  2: { label: 'Partial',   pill: 'bg-blue-500/15 text-blue-400 border border-blue-500/30',          dot: 'bg-blue-400 animate-pulse'   },
  3: { label: 'Filled',    pill: 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30', dot: 'bg-emerald-400'              },
  4: { label: 'Settled',   pill: 'bg-teal-500/15 text-teal-400 border border-teal-500/30',          dot: 'bg-teal-400'                 },
  5: { label: 'Cancelled', pill: 'bg-zinc-500/15 text-zinc-400 border border-zinc-500/30',          dot: 'bg-zinc-500'                 },
  6: { label: 'Rejected',  pill: 'bg-red-500/15 text-red-400 border border-red-500/40',             dot: 'bg-red-400'                  },
}

const FALLBACK = { label: 'Unknown', pill: 'bg-zinc-500/15 text-zinc-400 border border-zinc-500/30', dot: 'bg-zinc-500' }

export function OrderStatusBadge({ status, showDot = true }: StatusBadgeProps) {
  const n   = typeof status === 'string' ? parseInt(status) : status
  const cfg = STATUS_CONFIG[n] ?? { ...FALLBACK, label: String(status) }
  return (
    <span className={cfg.pill + ' inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-semibold'}>
      {showDot && <span className={cfg.dot + ' w-1.5 h-1.5 rounded-full flex-shrink-0'} />}
      {cfg.label}
    </span>
  )
}

export function OrderSideBadge({ side }: SideBadgeProps) {
  const isBuy = side === 0 || String(side).toLowerCase() === 'buy'
  const cls   = isBuy
    ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/40'
    : 'bg-red-500/20 text-red-400 border border-red-500/40'
  return (
    <span className={cls + ' inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold tracking-wide'}>
      {isBuy ? 'BUY' : 'SELL'}
    </span>
  )
}
