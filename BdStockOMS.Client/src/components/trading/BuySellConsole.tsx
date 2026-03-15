// @ts-nocheck
// src/components/trading/BuySellConsole.tsx
// Day 53/54 revised — Full Professional Buy/Sell Order Console
// F1=Buy F2=Sell, all BD market fields, BO lookup, circuit breaker, keyboard nav

import { useState, useEffect, useRef, useCallback, useMemo } from 'react'
import { useOrders } from '../../hooks/useOrders'
import { useMarketData } from '../../hooks/useMarketData'
import { getBOAccounts } from '../../api/client'

// ─── Types ────────────────────────────────────────────────────────────────────
type Side        = 'BUY' | 'SELL'
type PriceType   = 'Limit' | 'Market' | 'MarketAtBest'
type TimeInForce = 'Day' | 'IOC' | 'FOK'
type Exchange    = 'DSE' | 'CSE' | 'Both'
type Market      = 'Regular' | 'SME' | 'ATB' | 'GOV' | 'ODD_LOT' | 'BLOCK' | 'Spot'

// BD market places reference:
// DSE: Regular, SME (Small & Medium), ATB (Alternative Trading Board), GOV (Govt securities)
// CSE: Regular, SME, ATB, GOV, Odd Lot, Block
const MARKETS: Market[] = ['Regular', 'SME', 'ATB', 'GOV', 'ODD_LOT', 'BLOCK', 'Spot']

interface BOClient {
  userId: number
  fullName: string
  boNumber: string
  cashBalance: number
  marginLimit: number
  availableMargin: number
  accountType: string
}

// ─── Global event bus ────────────────────────────────────────────────────────
type Listener = (s: Partial<{ open: boolean; side: Side; symbol: string }>) => void
const _bus = new Set<Listener>()
export const BuySellConsoleEvents = {
  open:  (side: Side = 'BUY', symbol = '') => _bus.forEach(fn => fn({ open: true, side, symbol })),
  close: ()                                 => _bus.forEach(fn => fn({ open: false })),
}

// ─── Keyboard shortcuts ───────────────────────────────────────────────────────
function useGlobalShortcuts(onF1: ()=>void, onF2: ()=>void, onEsc: ()=>void) {
  useEffect(() => {
    const h = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement)?.tagName
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return
      if (e.key === 'F1') { e.preventDefault(); onF1() }
      if (e.key === 'F2') { e.preventDefault(); onF2() }
      if (e.key === 'Escape') onEsc()
    }
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [onF1, onF2, onEsc])
}

// ─── Reusable components ─────────────────────────────────────────────────────
const mono = "'JetBrains Mono', monospace"

const Label = ({ text, sub }: { text: string; sub?: string }) => (
  <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--t-text3)', letterSpacing: '0.08em', marginBottom: 3, fontFamily: mono, display: 'flex', justifyContent: 'space-between' }}>
    <span>{text}</span>
    {sub && <span style={{ color: 'var(--t-accent)', fontWeight: 600 }}>{sub}</span>}
  </div>
)

const HR = () => <div style={{ height: 1, background: 'var(--t-border)', margin: '4px 0' }} />

function StyledSelect({ value, onChange, options, tabIndex }: { value: string; onChange: (v: string) => void; options: { value: string; label: string }[]; tabIndex?: number }) {
  return (
    <select value={value} onChange={e => onChange(e.target.value)} tabIndex={tabIndex}
      style={{
        width: '100%', background: 'var(--t-hover)', border: '1px solid var(--t-border)',
        borderRadius: 6, padding: '7px 10px', color: 'var(--t-text1)', fontSize: 11,
        outline: 'none', fontFamily: mono, fontWeight: 600, cursor: 'pointer',
        appearance: 'none', backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6'%3E%3Cpath d='M0 0l5 6 5-6z' fill='%23888'/%3E%3C/svg%3E")`,
        backgroundRepeat: 'no-repeat', backgroundPosition: 'right 10px center',
        paddingRight: 28,
      }}
      onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
      onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
    >
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  )
}

function NumField({ label, value, onChange, prefix = '', step = 1, min = 0, tabIndex, sub, readOnly }: any) {
  const handleKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (readOnly) return
    if (e.key === 'ArrowUp')   { e.preventDefault(); onChange(String(+(+value || 0) + step)) }
    if (e.key === 'ArrowDown') { e.preventDefault(); onChange(String(Math.max(min, (+value || 0) - step))) }
  }
  return (
    <div>
      <Label text={label} sub={sub} />
      <div style={{ position: 'relative' }}>
        {prefix && <span style={{ position: 'absolute', left: 9, top: '50%', transform: 'translateY(-50%)', color: 'var(--t-text3)', fontSize: 11, fontFamily: mono, pointerEvents: 'none' }}>{prefix}</span>}
        <input type="number" value={value} min={min} step={step} tabIndex={tabIndex}
          readOnly={readOnly}
          onChange={e => !readOnly && onChange(e.target.value)}
          onKeyDown={handleKey}
          style={{
            width: '100%', boxSizing: 'border-box',
            paddingLeft: prefix ? 22 : 10, paddingRight: 10, paddingTop: 7, paddingBottom: 7,
            background: readOnly ? 'var(--t-panel)' : 'var(--t-hover)',
            border: '1px solid var(--t-border)', borderRadius: 6,
            color: readOnly ? 'var(--t-text3)' : 'var(--t-text1)',
            fontSize: 12, outline: 'none', fontFamily: mono, fontWeight: 600,
          }}
          onFocus={e => { if (!readOnly) e.currentTarget.style.borderColor = 'var(--t-accent)' }}
          onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
        />
      </div>
    </div>
  )
}

// ─── BO Code autocomplete ─────────────────────────────────────────────────────
function BOSearch({ clients, query, setQuery, onSelect }: any) {
  const [open, setOpen] = useState(false)
  const [cur, setCur]   = useState(0)
  const ref = useRef<HTMLDivElement>(null)

  const matches = useMemo(() => {
    const q = query.toLowerCase()
    return clients.filter((c: BOClient) =>
      c.boNumber?.toLowerCase().includes(q) ||
      c.fullName?.toLowerCase().includes(q)
    ).slice(0, 10)
  }, [query, clients])

  useEffect(() => { setCur(0) }, [matches.length])
  useEffect(() => {
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', h); return () => document.removeEventListener('mousedown', h)
  }, [])

  const select = (c: BOClient) => { onSelect(c); setOpen(false) }

  const onKey = (e: React.KeyboardEvent) => {
    if (!open) { if (e.key === 'ArrowDown') setOpen(true); return }
    if (e.key === 'ArrowDown')  { e.preventDefault(); setCur(c => Math.min(c+1, matches.length-1)) }
    if (e.key === 'ArrowUp')    { e.preventDefault(); setCur(c => Math.max(c-1, 0)) }
    if (e.key === 'Enter' || e.key === 'Tab') { e.preventDefault(); if (matches[cur]) select(matches[cur]) }
    if (e.key === 'Escape') setOpen(false)
  }

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <input value={query} onChange={e => { setQuery(e.target.value); setOpen(true) }}
        onFocus={() => setOpen(true)} onKeyDown={onKey}
        placeholder="BO Number or Client Name" tabIndex={1}
        style={{ width: '100%', boxSizing: 'border-box', background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 6, padding: '7px 10px', color: 'var(--t-text1)', fontSize: 12, fontFamily: mono, outline: 'none' }}
        onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
        onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
      />
      {open && matches.length > 0 && (
        <div style={{ position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 300, background: 'var(--t-elevated)', border: '1px solid var(--t-border)', borderRadius: 8, maxHeight: 180, overflowY: 'auto', boxShadow: '0 8px 24px rgba(0,0,0,0.5)', marginTop: 2 }}>
          {matches.map((c: BOClient, i: number) => (
            <div key={c.userId} onMouseDown={() => select(c)}
              style={{ padding: '7px 12px', cursor: 'pointer', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: i === cur ? 'var(--t-hover)' : 'transparent', borderBottom: '1px solid var(--t-border)' }}
              onMouseEnter={() => setCur(i)}>
              <div>
                <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-accent)', fontFamily: mono }}>{c.boNumber}</div>
                <div style={{ fontSize: 10, color: 'var(--t-text2)' }}>{c.fullName} · {c.accountType}</div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>CASH</div>
                <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-buy)', fontFamily: mono }}>৳{c.cashBalance?.toLocaleString()}</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── Symbol autocomplete ──────────────────────────────────────────────────────
function SymbolSearch({ value, onChange, onSelect, stocks }: any) {
  const [open, setOpen] = useState(false)
  const [cur, setCur]   = useState(0)
  const ref = useRef<HTMLDivElement>(null)

  const matches = useMemo(() => value.length < 1 ? [] :
    stocks.filter((s: any) =>
      s.tradingCode?.toUpperCase().startsWith(value.toUpperCase()) ||
      s.companyName?.toUpperCase().includes(value.toUpperCase()) ||
      s.stockName?.toUpperCase().includes(value.toUpperCase())
    ).slice(0, 8)
  , [value, stocks])

  useEffect(() => { setCur(0) }, [matches.length])
  useEffect(() => {
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', h); return () => document.removeEventListener('mousedown', h)
  }, [])

  const select = (s: any) => { onSelect(s); setOpen(false) }

  const onKey = (e: React.KeyboardEvent) => {
    if (!open || !matches.length) return
    if (e.key === 'ArrowDown')  { e.preventDefault(); setCur(c => Math.min(c+1, matches.length-1)) }
    if (e.key === 'ArrowUp')    { e.preventDefault(); setCur(c => Math.max(c-1, 0)) }
    if (e.key === 'Enter' || e.key === 'Tab') { e.preventDefault(); if (matches[cur]) select(matches[cur]) }
    if (e.key === 'Escape') setOpen(false)
  }

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <input value={value} onChange={e => { onChange(e.target.value.toUpperCase()); setOpen(true) }}
        onFocus={() => setOpen(true)} onKeyDown={onKey} tabIndex={4}
        placeholder="e.g. GP, BATBC, RENATA"
        style={{ width: '100%', boxSizing: 'border-box', background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 6, padding: '7px 10px', color: 'var(--t-text1)', fontSize: 13, fontWeight: 700, fontFamily: mono, outline: 'none' }}
        onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
        onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
      />
      {open && matches.length > 0 && (
        <div style={{ position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 300, background: 'var(--t-elevated)', border: '1px solid var(--t-border)', borderRadius: 8, maxHeight: 240, overflowY: 'auto', boxShadow: '0 8px 24px rgba(0,0,0,0.5)', marginTop: 2 }}>
          {matches.map((s: any, i: number) => (
            <div key={s.tradingCode ?? s.id} onMouseDown={() => select(s)}
              style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '7px 12px', cursor: 'pointer', background: i === cur ? 'var(--t-hover)' : 'transparent', borderBottom: '1px solid var(--t-border)' }}
              onMouseEnter={() => setCur(i)}>
              <div>
                <div style={{ fontSize: 12, fontWeight: 700, color: 'var(--t-accent)', fontFamily: mono }}>{s.tradingCode}</div>
                <div style={{ fontSize: 9, color: 'var(--t-text3)', display: 'flex', gap: 8 }}>
                  <span>{(s.companyName ?? s.stockName ?? '').slice(0, 26)}</span>
                  {s.category && <span style={{ color: catColor(s.category), fontWeight: 700 }}>[{s.category}]</span>}
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--t-text1)', fontFamily: mono }}>৳{(s.lastTradePrice ?? s.lastPrice ?? 0).toFixed(2)}</div>
                <div style={{ fontSize: 9, color: s.exchange === 'DSE' ? '#60a5fa' : '#a78bfa', fontFamily: mono }}>{s.exchange}</div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function catColor(cat: string) {
  const m: Record<string,string> = { A:'#00e676', B:'#ffd740', G:'#60a5fa', N:'#a78bfa', Z:'#ff1744', Spot:'#ff9100' }
  return m[cat] ?? 'var(--t-text3)'
}

// ─── Segmented button group ───────────────────────────────────────────────────
function Seg({ options, value, onChange, color }: { options: string[]; value: string; onChange: (v: string) => void; color?: string }) {
  return (
    <div style={{ display: 'flex', borderRadius: 7, overflow: 'hidden', border: '1px solid var(--t-border)' }}>
      {options.map(o => (
        <button key={o} onClick={() => onChange(o)} style={{
          flex: 1, padding: '6px 4px', border: 'none', cursor: 'pointer',
          fontSize: 10, fontWeight: 700, fontFamily: mono, letterSpacing: '0.03em',
          background: value === o ? (color ?? 'var(--t-accent)') : 'var(--t-hover)',
          color: value === o ? '#000' : 'var(--t-text3)', transition: 'all 0.1s',
        }}>{o}</button>
      ))}
    </div>
  )
}

// ─── Info tile ────────────────────────────────────────────────────────────────
function Tile({ label, value, color }: { label: string; value: string; color?: string }) {
  return (
    <div style={{ background: 'var(--t-panel)', borderRadius: 6, padding: '6px 10px' }}>
      <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: mono, marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 12, fontWeight: 700, color: color ?? 'var(--t-text1)', fontFamily: mono }}>{value}</div>
    </div>
  )
}

// ─── Confirmation popup ───────────────────────────────────────────────────────
function ConfirmPopup({ order, onConfirm, onCancel, placing }: any) {
  const isBuy = order.side === 'BUY'
  const color = isBuy ? 'var(--t-buy)' : 'var(--t-sell)'

  useEffect(() => {
    const h = (e: KeyboardEvent) => {
      if (e.key === 'Enter')  { e.preventDefault(); onConfirm() }
      if (e.key === 'Escape') onCancel()
    }
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [])

  return (
    <>
      <div onClick={onCancel} style={{ position: 'fixed', inset: 0, zIndex: 9995, background: 'rgba(0,0,0,0.5)' }} />
      <div style={{
        position: 'fixed', top: '50%', left: '50%', zIndex: 9996,
        transform: 'translate(-50%, -50%)',
        width: 380, maxWidth: '95vw',
        background: 'var(--t-surface)', border: `2px solid ${color}40`,
        borderRadius: 14, overflow: 'hidden',
        boxShadow: `0 24px 48px rgba(0,0,0,0.7), 0 0 0 1px ${color}20`,
        animation: 'oms-slide-up 0.15s ease',
      }}>
        <div style={{ height: 3, background: `linear-gradient(90deg, transparent, ${color}, transparent)` }} />
        <div style={{ padding: '16px 20px' }}>
          <div style={{ fontSize: 13, fontWeight: 800, color: color, fontFamily: mono, marginBottom: 14, letterSpacing: '0.04em' }}>
            CONFIRM {order.side} ORDER
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 14 }}>
            <Tile label="SYMBOL"    value={order.symbol} color="var(--t-accent)" />
            <Tile label="SIDE"      value={order.side}   color={color} />
            <Tile label="QUANTITY"  value={order.qty?.toLocaleString()} />
            <Tile label="PRICE"     value={order.priceType === 'Market' ? 'MARKET' : `৳${(+order.price).toFixed(2)}`} />
            <Tile label="EXCHANGE"  value={order.exchange} />
            <Tile label="ORDER TYPE" value={order.priceType} />
            <Tile label="TIF"       value={order.tif} />
            <Tile label="MARKET"    value={order.market} />
            {order.client && <Tile label="CLIENT"  value={order.client.fullName} />}
            {order.client && <Tile label="BO CODE" value={order.client.boNumber} />}
          </div>
          <div style={{ background: `${color}10`, border: `1px solid ${color}30`, borderRadius: 8, padding: '10px 14px', marginBottom: 14, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontSize: 11, color: 'var(--t-text3)', fontFamily: mono }}>TOTAL ORDER VALUE</span>
            <span style={{ fontSize: 18, fontWeight: 800, color: color, fontFamily: mono }}>৳{order.value?.toLocaleString('en-BD', { minimumFractionDigits: 2 })}</span>
          </div>
          <div style={{ display: 'flex', gap: 10 }}>
            <button onClick={onConfirm} disabled={placing} autoFocus
              style={{ flex: 2, padding: '11px', fontSize: 13, fontWeight: 800, borderRadius: 8, border: 'none', cursor: placing ? 'wait' : 'pointer', background: color, color: isBuy ? '#000' : '#fff', fontFamily: mono, opacity: placing ? 0.7 : 1 }}>
              {placing ? 'Placing…' : `CONFIRM ${order.side} ↵`}
            </button>
            <button onClick={onCancel}
              style={{ flex: 1, padding: '11px', fontSize: 12, borderRadius: 8, border: '1px solid var(--t-border)', cursor: 'pointer', background: 'var(--t-hover)', color: 'var(--t-text2)', fontFamily: mono }}>
              Cancel ESC
            </button>
          </div>
          <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono, textAlign: 'center', marginTop: 8 }}>
            Press Enter to confirm · Esc to cancel
          </div>
        </div>
      </div>
    </>
  )
}

// ─── Main Console ─────────────────────────────────────────────────────────────
export function BuySellConsole({ embedded = false }: { embedded?: boolean } = {}) {
  const { place, placing } = useOrders()
  const { ticksArray }     = useMarketData()

  const [open, setOpen]   = useState(false)
  const [side, setSide]   = useState<Side>('BUY')

  // BO / Client
  const [clients, setClients]           = useState<BOClient[]>([])
  const [boQuery, setBoQuery]           = useState('')
  const [client, setClient]             = useState<BOClient | null>(null)

  // Order fields
  const [exchange, setExchange]   = useState<Exchange>('DSE')
  const [market, setMarket]       = useState<Market>('Regular')
  const [symbol, setSymbol]       = useState('')
  const [priceType, setPriceType] = useState<PriceType>('Limit')
  const [qty, setQty]             = useState('')
  const [price, setPrice]         = useState('')
  const [displayQty, setDisplayQty] = useState('')
  const [tif, setTif]             = useState<TimeInForce>('Day')
  const [minQty, setMinQty]       = useState('')
  const [isPrivate, setIsPrivate] = useState(false)

  // UI
  const [showConfirm, setShowConfirm] = useState(false)
  const [result, setResult]           = useState<{ ok: boolean; text: string } | null>(null)
  const [warn, setWarn]               = useState<string | null>(null)
  const [limitRequest, setLimitRequest] = useState(false)

  // Derived live stock
  const live = useMemo(() => ticksArray.find(s => s.tradingCode === symbol), [ticksArray, symbol])

  // Computed values
  const numQty   = +qty   || 0
  const numPrice = +price || 0
  const orderValue  = numQty * numPrice
  const purchasePower = client ? client.cashBalance + client.availableMargin : null
  const remaining   = purchasePower !== null ? purchasePower - orderValue : null
  const commission  = orderValue * 0.005  // 0.5% indicative BD commission

  // Event bus
  useEffect(() => {
    const fn = (s: any) => {
      if (s.open !== undefined) setOpen(s.open)
      if (s.side) setSide(s.side)
      if (s.symbol !== undefined) {
        setSymbol(s.symbol)
        const t = ticksArray.find(x => x.tradingCode === s.symbol)
        if (t?.lastPrice) setPrice(t.lastPrice.toFixed(2))
      }
    }
    _bus.add(fn); return () => { _bus.delete(fn) }
  }, [ticksArray])

  // Auto-fill price
  useEffect(() => {
    if (live?.lastPrice && !price) setPrice(live.lastPrice.toFixed(2))
  }, [live?.lastPrice])

  // Load BO clients once
  useEffect(() => {
    if ((open || embedded) && clients.length === 0) {
      getBOAccounts().then((raw: any) => {
        const list = Array.isArray(raw) ? raw : raw?.items ?? raw?.data ?? []
        setClients(list.map((c: any) => ({
          userId: c.userId, fullName: c.fullName,
          boNumber: c.boNumber ?? c.bONumber ?? '',
          cashBalance: c.cashBalance ?? 0,
          marginLimit: c.marginLimit ?? 0,
          availableMargin: c.availableMargin ?? 0,
          accountType: c.accountType ?? '',
        })))
      }).catch(() => {})
    }
  }, [open])

  const doOpen  = useCallback((s: Side) => { setSide(s); setOpen(true) }, [])
  const doClose = useCallback(() => { setOpen(false); setShowConfirm(false); setResult(null); setWarn(null) }, [])

  useGlobalShortcuts(() => embedded ? null : doOpen('BUY'), () => embedded ? null : doOpen('SELL'), embedded ? () => {} : doClose)

  const handleSymbolSelect = (s: any) => {
    setSymbol(s.tradingCode)
    const lp = s.lastTradePrice ?? s.lastPrice ?? 0
    if (lp) setPrice(lp.toFixed(2))
  }

  const validate = () => {
    if (!symbol)               { setWarn('Symbol is required'); return false }
    if (!qty || numQty <= 0)   { setWarn('Quantity must be positive'); return false }
    if (priceType === 'Limit' && (!price || numPrice <= 0)) { setWarn('Price required for Limit orders'); return false }
    if (live?.circuitBreakerHigh && numPrice > live.circuitBreakerHigh) { setWarn(`Price exceeds circuit breaker high ৳${live.circuitBreakerHigh}`); return false }
    if (live?.circuitBreakerLow  && numPrice < live.circuitBreakerLow  && priceType === 'Limit') { setWarn(`Price below circuit breaker low ৳${live.circuitBreakerLow}`); return false }
    setWarn(null); return true
  }

  const handleSubmit = () => { if (validate()) setShowConfirm(true) }

  const handleConfirm = async () => {
    setShowConfirm(false)
    try {
      await place({
        stockId:       live?.stockId ?? 0,
        orderType:     side === 'BUY' ? 0 : 1,
        orderCategory: priceType === 'Limit' ? 1 : 0,
        quantity:      numQty,
        limitPrice:    priceType === 'Limit' ? numPrice : undefined,
        investorId:    client?.userId,
      })
      setResult({ ok: true, text: `✓ ${side} ${numQty} × ${symbol} @ ${priceType === 'Market' ? 'MARKET' : `৳${numPrice.toFixed(2)}`} placed` })
      setTimeout(() => { setResult(null); doClose() }, 2500)
    } catch (e: any) {
      setResult({ ok: false, text: e?.message ?? 'Order failed' })
    }
  }

  const reset = () => {
    setSymbol(''); setQty(''); setPrice(''); setDisplayQty(''); setMinQty('')
    setPriceType('Limit'); setTif('Day'); setIsPrivate(false)
    setClient(null); setBoQuery(''); setWarn(null); setResult(null)
  }

  if (!open && !embedded) return null

  const isBuy      = side === 'BUY'
  const sideColor  = isBuy ? 'var(--t-buy)' : 'var(--t-sell)'
  const sideBg     = isBuy ? 'rgba(0,230,118,0.05)' : 'rgba(255,23,68,0.05)'
  const sideText   = isBuy ? '#000' : '#fff'

  const confirmOrder = {
    side, symbol, qty: numQty, price, priceType, exchange, market, tif, client,
    value: orderValue, isPrivate,
  }

  return (
    <>
      {!embedded && <div onClick={doClose} style={{ position: 'fixed', inset: 0, zIndex: 9990, background: 'rgba(0,0,0,0.55)', backdropFilter: 'blur(3px)' }} />}

      <div style={{
        ...(embedded ? {
          position: 'relative', width: '100%', height: '100%',
          display: 'flex', flexDirection: 'column',
          background: 'var(--t-surface)',
          border: `1px solid ${sideColor}30`, borderRadius: 10,
          overflow: 'hidden',
        } : {
          position: 'fixed', top: '50%', left: '50%', zIndex: 9991,
          transform: 'translate(-50%, -50%)',
          width: 540, maxWidth: '97vw', maxHeight: '94vh',
          background: 'var(--t-surface)', border: `1px solid ${sideColor}30`,
          borderRadius: 14, display: 'flex', flexDirection: 'column',
          boxShadow: `0 32px 64px rgba(0,0,0,0.7), 0 0 0 1px ${sideColor}15`,
          animation: 'oms-slide-up 0.18s ease',
        }),
      }}>
        {/* Accent line */}
        <div style={{ height: 2, flexShrink: 0, background: `linear-gradient(90deg, transparent, ${sideColor}, transparent)`, opacity: 0.8 }} />

        {/* ── Topbar ── */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '10px 14px 9px', background: sideBg, borderBottom: '1px solid var(--t-border)', flexShrink: 0, gap: 8 }}>
          {/* Side toggle */}
          <div style={{ display: 'flex', borderRadius: 8, overflow: 'hidden', border: '1px solid var(--t-border)', flexShrink: 0 }}>
            {(['BUY','SELL'] as Side[]).map(s => (
              <button key={s} onClick={() => setSide(s)} style={{
                padding: '6px 18px', border: 'none', cursor: 'pointer',
                fontSize: 12, fontWeight: 800, letterSpacing: '0.06em', fontFamily: mono,
                background: side === s ? (s==='BUY' ? 'var(--t-buy)' : 'var(--t-sell)') : 'var(--t-hover)',
                color: side === s ? (s==='BUY' ? '#000' : '#fff') : 'var(--t-text3)',
                transition: 'all 0.1s',
              }}>{s === 'BUY' ? 'F1  BUY' : 'F2  SELL'}</button>
            ))}
          </div>

          {/* Limit Request toggle */}
          <button onClick={() => setLimitRequest(l => !l)} style={{
            padding: '5px 10px', fontSize: 9, fontWeight: 700, fontFamily: mono,
            borderRadius: 6, border: `1px solid ${limitRequest ? 'var(--t-accent)' : 'var(--t-border)'}`,
            background: limitRequest ? 'rgba(var(--t-accent-rgb),0.1)' : 'transparent',
            color: limitRequest ? 'var(--t-accent)' : 'var(--t-text3)', cursor: 'pointer',
            transition: 'all 0.1s',
          }}>⚑ LIMIT REQ</button>

          <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono, flex: 1, textAlign: 'right' }}>ESC close</span>
          <button onClick={doClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 18, lineHeight: 1, padding: '0 2px' }}>✕</button>
        </div>

        {/* ── Scrollable body ── */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '12px 14px', display: 'flex', flexDirection: 'column', gap: 10 }}>

          {/* ── BO Code ── */}
          <div>
            <Label text="BO CODE / CLIENT" />
            <BOSearch clients={clients} query={boQuery} setQuery={setBoQuery}
              onSelect={(c: BOClient) => { setClient(c); setBoQuery(c.boNumber) }} />
          </div>

          {/* ── Client info card ── */}
          {client && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 6, background: 'var(--t-panel)', borderRadius: 8, padding: '10px 12px', border: `1px solid ${sideColor}20` }}>
              <div style={{ gridColumn: '1 / -1', display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                <div>
                  <span style={{ fontSize: 12, fontWeight: 700, color: 'var(--t-text1)' }}>{client.fullName}</span>
                  <span style={{ fontSize: 9, color: 'var(--t-accent)', fontFamily: mono, marginLeft: 8 }}>{client.boNumber}</span>
                </div>
                <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>{client.accountType}</span>
              </div>
              <Tile label="CASH BALANCE"    value={`৳${client.cashBalance.toLocaleString()}`}       color="var(--t-buy)" />
              <Tile label="PURCHASE POWER"  value={`৳${(client.cashBalance + client.availableMargin).toLocaleString()}`} color="var(--t-accent)" />
              <Tile label="MARGIN AVAIL"    value={`৳${client.availableMargin.toLocaleString()}`}   color="#a78bfa" />
            </div>
          )}

          <HR />

          {/* ── Exchange / Market row ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: 10 }}>
            <div>
              <Label text="EXCHANGE (SOR)" />
              <StyledSelect value={exchange} onChange={v => setExchange(v as Exchange)} tabIndex={2}
                options={[
                  { value: 'DSE',  label: 'DSE — Dhaka' },
                  { value: 'CSE',  label: 'CSE — Chittagong' },
                  { value: 'Both', label: 'Both (SOR)' },
                ]} />
            </div>
            <div>
              <Label text="MARKET / BOARD" />
              <StyledSelect value={market} onChange={v => setMarket(v as Market)} tabIndex={3}
                options={[
                  { value: 'Regular', label: 'Regular Market' },
                  { value: 'SME',     label: 'SME Board' },
                  { value: 'ATB',     label: 'ATB — Alternative Trading Board' },
                  { value: 'GOV',     label: 'GOV — Govt Securities' },
                  { value: 'ODD_LOT', label: 'Odd Lot Market' },
                  { value: 'BLOCK',   label: 'Block Market' },
                  { value: 'Spot',    label: 'Spot Market (T+0)' },
                ]} />
            </div>
          </div>

          {/* ── Symbol ── */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 3 }}>
              <Label text="SYMBOL" />
              {live?.category && (
                <span style={{ fontSize: 9, fontWeight: 800, fontFamily: mono, padding: '2px 7px', borderRadius: 4, border: `1px solid ${catColor(live.category)}50`, color: catColor(live.category), background: `${catColor(live.category)}12` }}>
                  Category {live.category}
                </span>
              )}
              {live?.exchange && (
                <span style={{ fontSize: 9, fontWeight: 700, fontFamily: mono, padding: '2px 7px', borderRadius: 4, color: live.exchange === 'DSE' ? '#60a5fa' : '#a78bfa', border: `1px solid ${live.exchange === 'DSE' ? '#60a5fa' : '#a78bfa'}40` }}>
                  {live.exchange}
                </span>
              )}
            </div>
            <SymbolSearch value={symbol} onChange={(v: string) => { setSymbol(v); setPrice('') }}
              onSelect={handleSymbolSelect} stocks={ticksArray} />
          </div>

          {/* ── Live price strip + circuit breaker ── */}
          {live?.category === 'Z' && (
            <div style={{ background: 'rgba(255,23,68,0.08)', border: '1px solid rgba(255,23,68,0.3)', borderRadius: 6, padding: '6px 10px', color: 'var(--t-sell)', fontSize: 10, fontFamily: mono }}>
              ⚠ Category Z — Defaulter stock. SPOT settlement only, T+0. No margin eligible.
            </div>
          )}
          {live?.category === 'Spot' && (
            <div style={{ background: 'rgba(255,145,0,0.08)', border: '1px solid rgba(255,145,0,0.3)', borderRadius: 6, padding: '6px 10px', color: '#ff9100', fontSize: 10, fontFamily: mono }}>
              ⚡ Spot Market — T+0 settlement. Immediate cash required.
            </div>
          )}
          {live && (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 6 }}>
              <Tile label="LAST"   value={`৳${live.lastPrice?.toFixed(2) ?? '—'}`}       color="var(--t-text1)" />
              <Tile label="CHG%"   value={`${(live.changePercent ?? 0) >= 0 ? '▲' : '▼'}${Math.abs(live.changePercent ?? 0).toFixed(2)}%`} color={(live.changePercent ?? 0) >= 0 ? 'var(--t-buy)' : 'var(--t-sell)'} />
              <Tile label="HIGH"   value={`৳${live.highPrice?.toFixed(2) ?? '—'}`}       color="var(--t-buy)" />
              <Tile label="LOW"    value={`৳${live.lowPrice?.toFixed(2) ?? '—'}`}         color="var(--t-sell)" />
              <Tile label="VOL"    value={live.volume >= 1e6 ? `${(live.volume/1e6).toFixed(1)}M` : live.volume >= 1e3 ? `${(live.volume/1e3).toFixed(0)}K` : String(live.volume ?? '—')} />
              {(live.circuitBreakerHigh || live.circuitBreakerLow) && (
                <div style={{ gridColumn: '1 / -1', background: 'rgba(245,158,11,0.06)', border: '1px solid rgba(245,158,11,0.2)', borderRadius: 6, padding: '5px 10px', display: 'flex', gap: 16, alignItems: 'center' }}>
                  <span style={{ fontSize: 9, color: '#f59e0b', fontFamily: mono, fontWeight: 700 }}>⚡ CIRCUIT BREAKER</span>
                  <span style={{ fontSize: 10, fontFamily: mono, color: 'var(--t-buy)' }}>HIGH: ৳{live.circuitBreakerHigh?.toFixed(2)}</span>
                  <span style={{ fontSize: 10, fontFamily: mono, color: 'var(--t-sell)' }}>LOW: ৳{live.circuitBreakerLow?.toFixed(2)}</span>
                  {numPrice > 0 && live.circuitBreakerHigh && live.circuitBreakerLow && (
                    <span style={{ marginLeft: 'auto', fontSize: 9, fontFamily: mono,
                      color: numPrice > live.circuitBreakerHigh ? 'var(--t-sell)' : numPrice < live.circuitBreakerLow ? 'var(--t-sell)' : 'var(--t-buy)' }}>
                      {numPrice > live.circuitBreakerHigh ? '⚠ ABOVE HIGH' : numPrice < live.circuitBreakerLow ? '⚠ BELOW LOW' : '✓ IN RANGE'}
                    </span>
                  )}
                </div>
              )}
            </div>
          )}

          <HR />

          {/* ── Price Type (dropdown) + Order Time (seg) ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <div>
              <Label text="PRICE TYPE" />
              <StyledSelect value={priceType} onChange={v => setPriceType(v as PriceType)} tabIndex={5}
                options={[
                  { value: 'Limit',        label: 'Limit' },
                  { value: 'Market',       label: 'Market' },
                  { value: 'MarketAtBest', label: 'Market at Best' },
                ]} />
            </div>
            <div>
              <Label text="ORDER TIME (TIF)" />
              <Seg options={['Day','IOC','FOK']} value={tif} onChange={v => setTif(v as TimeInForce)} color="var(--t-accent)" />
            </div>
          </div>

          {/* ── Qty + Price ── */}
          <div style={{ display: 'grid', gridTemplateColumns: priceType === 'Market' ? '1fr' : '1fr 1fr', gap: 10 }}>
            <NumField label="QUANTITY" value={qty} onChange={setQty} step={1} min={1} tabIndex={6} />
            {priceType !== 'Market' && (
              <NumField label="PRICE ৳" value={price} onChange={setPrice} prefix="৳" step={0.01} min={0.01} tabIndex={7}
                sub={live?.lastPrice ? `Last: ৳${live.lastPrice.toFixed(2)}` : undefined} />
            )}
          </div>

          {/* ── Display Qty + Min Qty ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <NumField label="DISPLAY QTY" value={displayQty} onChange={setDisplayQty} step={1} min={0} tabIndex={8} />
            <NumField label="MIN QTY" value={minQty} onChange={setMinQty} step={1} min={0} tabIndex={9} />
          </div>

          {/* ── Private order ── */}
          <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', userSelect: 'none',
            fontSize: 11, fontWeight: 600, color: isPrivate ? sideColor : 'var(--t-text3)', fontFamily: mono,
            background: 'var(--t-hover)', borderRadius: 6, padding: '7px 10px',
            border: `1px solid ${isPrivate ? sideColor + '50' : 'var(--t-border)'}`, transition: 'all 0.1s' }}>
            <input type="checkbox" checked={isPrivate} onChange={e => setIsPrivate(e.target.checked)}
              style={{ accentColor: sideColor, width: 13, height: 13 }} tabIndex={10} />
            🔒 PRIVATE ORDER
          </label>

          <HR />

          {/* ── Order Summary ── */}
          {numQty > 0 && (
            <div style={{ background: 'var(--t-panel)', borderRadius: 8, padding: '10px 12px', border: `1px solid ${sideColor}20` }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 6, marginBottom: 8 }}>
                <Tile label="TOTAL COST"   value={`৳${orderValue.toLocaleString('en-BD', { minimumFractionDigits: 2 })}`} color={sideColor} />
                <Tile label="COMMISSION ~" value={`৳${commission.toLocaleString('en-BD', { minimumFractionDigits: 2 })}`} color="var(--t-text3)" />
                {purchasePower !== null && <Tile label="REMAINING" value={`৳${(remaining ?? 0).toLocaleString('en-BD', { minimumFractionDigits: 2 })}`} color={(remaining ?? 0) >= 0 ? 'var(--t-buy)' : 'var(--t-sell)'} />}
              </div>
              {purchasePower !== null && (
                <div style={{ height: 4, background: 'var(--t-border)', borderRadius: 2, overflow: 'hidden' }}>
                  <div style={{ height: '100%', borderRadius: 2, transition: 'width 0.3s',
                    width: `${Math.min(100, (orderValue / purchasePower) * 100)}%`,
                    background: orderValue <= purchasePower ? sideColor : 'var(--t-sell)' }} />
                </div>
              )}
              {purchasePower !== null && orderValue > purchasePower && (
                <div style={{ fontSize: 9, color: 'var(--t-sell)', fontFamily: mono, marginTop: 4 }}>⚠ Order exceeds purchase power</div>
              )}
            </div>
          )}

          {/* Warn / Result */}
          {warn && (
            <div style={{ background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.3)', borderRadius: 6, padding: '7px 10px', color: '#f59e0b', fontSize: 11 }}>⚠ {warn}</div>
          )}
          {result && (
            <div style={{ background: result.ok ? 'rgba(0,230,118,0.08)' : 'rgba(255,23,68,0.08)', border: `1px solid ${result.ok ? 'rgba(0,230,118,0.3)' : 'rgba(255,23,68,0.3)'}`, borderRadius: 6, padding: '7px 10px', color: result.ok ? 'var(--t-buy)' : 'var(--t-sell)', fontSize: 11 }}>
              {result.text}
            </div>
          )}

          {/* ── Action buttons ── */}
          <div style={{ display: 'flex', gap: 8, marginTop: 2 }}>
            <button onClick={handleSubmit} tabIndex={11} style={{
              flex: 1, padding: '12px', fontSize: 13, fontWeight: 800, borderRadius: 8,
              border: 'none', cursor: 'pointer', background: sideColor, color: sideText,
              letterSpacing: '0.04em', fontFamily: mono, transition: 'opacity 0.1s',
            }}
              onMouseEnter={e => e.currentTarget.style.opacity = '0.85'}
              onMouseLeave={e => e.currentTarget.style.opacity = '1'}
            >{side}  {symbol || '—'}  {numQty ? `× ${numQty}` : ''}</button>
            <button onClick={reset} tabIndex={12} style={{
              padding: '12px 16px', fontSize: 11, borderRadius: 8,
              border: '1px solid var(--t-border)', cursor: 'pointer',
              background: 'transparent', color: 'var(--t-text3)', fontFamily: mono,
            }}>Reset</button>
          </div>
        </div>

        {/* ── Footer ── */}
        <div style={{ padding: '6px 14px', borderTop: '1px solid var(--t-border)', background: 'var(--t-panel)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexShrink: 0 }}>
          <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>F1 Buy · F2 Sell · ESC Close · ↑↓ Navigate · Enter Confirm</span>
          <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: mono }}>{live ? `● ${symbol} ${live.exchange ?? ''}` : '○ No symbol'}{client ? ` · ${client.boNumber}` : ''}</span>
        </div>
      </div>

      {/* ── Confirm popup ── */}
      {showConfirm && (
        <ConfirmPopup order={confirmOrder} onConfirm={handleConfirm} onCancel={() => setShowConfirm(false)} placing={placing} />
      )}
    </>
  )
}

// ─── Hover trigger ────────────────────────────────────────────────────────────
export function BuySellHoverTrigger({ symbol }: { symbol?: string }) {
  const [visible, setVisible] = useState(false)
  const timer = useRef<any>()
  const show = () => { clearTimeout(timer.current); setVisible(true) }
  const hide = () => { timer.current = setTimeout(() => setVisible(false), 300) }
  return (
    <div style={{ position: 'relative', display: 'inline-flex' }} onMouseEnter={show} onMouseLeave={hide}>
      {visible && (
        <div style={{ position: 'absolute', left: 0, top: '50%', transform: 'translateY(-50%)', display: 'flex', gap: 3, zIndex: 100, animation: 'oms-fade-in 0.12s ease' }}
          onMouseEnter={show} onMouseLeave={hide}>
          <button onClick={() => BuySellConsoleEvents.open('BUY',  symbol)} style={{ padding: '3px 8px', fontSize: 9, fontWeight: 800, borderRadius: 4, border: 'none', cursor: 'pointer', background: 'var(--t-buy)',  color: '#000', fontFamily: mono }}>B</button>
          <button onClick={() => BuySellConsoleEvents.open('SELL', symbol)} style={{ padding: '3px 8px', fontSize: 9, fontWeight: 800, borderRadius: 4, border: 'none', cursor: 'pointer', background: 'var(--t-sell)', color: '#fff', fontFamily: mono }}>S</button>
        </div>
      )}
    </div>
  )
}

export function BuySellConsoleInline(props: any) {
  return <BuySellConsole embedded={true} {...props} />
}
