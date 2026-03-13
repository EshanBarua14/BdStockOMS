// @ts-nocheck
// src/components/trading/BuySellConsole.tsx
// Day 53 (revised) — Full-featured Buy/Sell Order Panel
// F1=Buy F2=Sell, BO Code lookup, client info, all order fields, keyboard nav

import { useState, useEffect, useRef, useCallback, useMemo } from 'react'
import { useOrders } from '../../hooks/useOrders'
import { useMarketData } from '../../hooks/useMarketData'
import { useAuthStore } from '../../store/authStore'
import { getBOAccounts, getMyInvestors } from '../../api/client'

// ─── Types ────────────────────────────────────────────────────────────────────
type Side        = 'BUY' | 'SELL'
type PriceType   = 'Limit' | 'Market' | 'MarketAtBest'
type TimeInForce = 'Day' | 'IOC' | 'FOK'
type Exchange    = 'DSE' | 'CSE'
type Market      = 'Public' | 'SME' | 'ATB' | 'GOV'

interface BOClient {
  userId: number
  fullName: string
  boNumber: string
  cashBalance: number
  marginLimit: number
  availableMargin: number
  accountType: string
}

interface ConsoleState { open: boolean; side: Side; symbol: string }
type ConsoleListener = (s: Partial<ConsoleState>) => void
const _listeners = new Set<ConsoleListener>()

export const BuySellConsoleEvents = {
  open:  (side: Side, symbol = '') => _listeners.forEach(fn => fn({ open: true, side, symbol })),
  close: ()                        => _listeners.forEach(fn => fn({ open: false })),
}

// ─── Global keyboard shortcuts ────────────────────────────────────────────────
function useGlobalShortcuts(onF1: () => void, onF2: () => void, onEsc: () => void) {
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

// ─── Styled label ─────────────────────────────────────────────────────────────
const Label = ({ text }: { text: string }) => (
  <div style={{
    fontSize: 9, fontWeight: 700, color: 'var(--t-text3)',
    letterSpacing: '0.08em', marginBottom: 4,
    fontFamily: "'JetBrains Mono', monospace",
  }}>{text}</div>
)

// ─── Segmented control ────────────────────────────────────────────────────────
function Seg<T extends string>({ options, value, onChange, accent }: {
  options: T[], value: T, onChange: (v: T) => void, accent?: string
}) {
  return (
    <div style={{ display: 'flex', borderRadius: 7, overflow: 'hidden', border: '1px solid var(--t-border)' }}>
      {options.map(o => (
        <button key={o} onClick={() => onChange(o)} style={{
          flex: 1, padding: '6px 0', border: 'none', cursor: 'pointer',
          fontSize: 10, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace",
          letterSpacing: '0.04em', transition: 'all 0.1s',
          background: value === o ? (accent ?? 'var(--t-accent)') : 'var(--t-hover)',
          color: value === o ? '#000' : 'var(--t-text3)',
        }}>{o}</button>
      ))}
    </div>
  )
}

// ─── BO Code autocomplete ─────────────────────────────────────────────────────
function BOCodeSearch({ clients, boQuery, setBoQuery, onSelect, selectedClient }: any) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const [cursor, setCursor] = useState(0)

  const matches = useMemo(() => {
    if (!boQuery) return clients.slice(0, 10)
    const q = boQuery.toLowerCase()
    return clients.filter((c: BOClient) =>
      c.boNumber?.toLowerCase().includes(q) ||
      c.fullName?.toLowerCase().includes(q)
    ).slice(0, 10)
  }, [boQuery, clients])

  useEffect(() => { setCursor(0) }, [matches.length])

  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [])

  const handleKey = (e: React.KeyboardEvent) => {
    if (!open) { if (e.key === 'ArrowDown') setOpen(true); return }
    if (e.key === 'ArrowDown')  { e.preventDefault(); setCursor(c => Math.min(c + 1, matches.length - 1)) }
    if (e.key === 'ArrowUp')    { e.preventDefault(); setCursor(c => Math.max(c - 1, 0)) }
    if (e.key === 'Enter')      { e.preventDefault(); if (matches[cursor]) { onSelect(matches[cursor]); setOpen(false) } }
    if (e.key === 'Escape')     setOpen(false)
    if (e.key === 'Tab')        { if (matches[cursor]) { onSelect(matches[cursor]); setOpen(false) } }
  }

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <input
        value={boQuery}
        onChange={e => { setBoQuery(e.target.value); setOpen(true) }}
        onFocus={() => setOpen(true)}
        onKeyDown={handleKey}
        placeholder="BO Number or Client Name"
        style={{
          width: '100%', boxSizing: 'border-box',
          background: 'var(--t-hover)', border: '1px solid var(--t-border)',
          borderRadius: 6, padding: '7px 10px', color: 'var(--t-text1)',
          fontSize: 12, fontFamily: "'JetBrains Mono', monospace", outline: 'none',
        }}
        onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
        onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
      />
      {open && matches.length > 0 && (
        <div ref={listRef} style={{
          position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 200,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 8, marginTop: 2, maxHeight: 200, overflowY: 'auto',
          boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
        }}>
          {matches.map((c: BOClient, i: number) => (
            <div key={c.userId}
              onMouseDown={() => { onSelect(c); setOpen(false) }}
              style={{
                padding: '7px 12px', cursor: 'pointer',
                background: i === cursor ? 'var(--t-hover)' : 'transparent',
                borderBottom: '1px solid var(--t-border)',
                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
              }}
              onMouseEnter={() => setCursor(i)}
            >
              <div>
                <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-accent)', fontFamily: "'JetBrains Mono', monospace" }}>
                  {c.boNumber}
                </div>
                <div style={{ fontSize: 10, color: 'var(--t-text2)' }}>{c.fullName}</div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>CASH</div>
                <div style={{ fontSize: 10, fontWeight: 600, color: 'var(--t-buy)', fontFamily: "'JetBrains Mono', monospace" }}>
                  ৳{c.cashBalance?.toLocaleString()}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── Symbol search ────────────────────────────────────────────────────────────
function SymbolSearch({ value, onChange, onSelect, stocks }: any) {
  const [open, setOpen] = useState(false)
  const [cursor, setCursor] = useState(0)
  const ref = useRef<HTMLDivElement>(null)

  const matches = useMemo(() => value.length < 1 ? [] :
    stocks.filter((s: any) =>
      s.tradingCode?.toUpperCase().startsWith(value.toUpperCase()) ||
      s.stockName?.toUpperCase().includes(value.toUpperCase()) ||
      s.companyName?.toUpperCase().includes(value.toUpperCase())
    ).slice(0, 8)
  , [value, stocks])

  useEffect(() => { setCursor(0) }, [matches.length])

  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [])

  const handleKey = (e: React.KeyboardEvent) => {
    if (!open || !matches.length) return
    if (e.key === 'ArrowDown')  { e.preventDefault(); setCursor(c => Math.min(c + 1, matches.length - 1)) }
    if (e.key === 'ArrowUp')    { e.preventDefault(); setCursor(c => Math.max(c - 1, 0)) }
    if (e.key === 'Enter')      { e.preventDefault(); if (matches[cursor]) { onSelect(matches[cursor]); setOpen(false) } }
    if (e.key === 'Tab')        { if (matches[cursor]) { onSelect(matches[cursor]); setOpen(false) } }
    if (e.key === 'Escape')     setOpen(false)
  }

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <input
        value={value}
        onChange={e => { onChange(e.target.value.toUpperCase()); setOpen(true) }}
        onFocus={() => setOpen(true)}
        onKeyDown={handleKey}
        placeholder="e.g. GP, BATBC, RENATA"
        style={{
          width: '100%', boxSizing: 'border-box',
          background: 'var(--t-hover)', border: '1px solid var(--t-border)',
          borderRadius: 6, padding: '7px 10px', color: 'var(--t-text1)',
          fontSize: 13, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", outline: 'none',
        }}
        onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
        onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
      />
      {open && matches.length > 0 && (
        <div style={{
          position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 200,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 8, marginTop: 2, maxHeight: 220, overflowY: 'auto',
          boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
        }}>
          {matches.map((s: any, i: number) => (
            <div key={s.tradingCode}
              onMouseDown={() => { onSelect(s); setOpen(false) }}
              style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '7px 12px', cursor: 'pointer',
                background: i === cursor ? 'var(--t-hover)' : 'transparent',
                borderBottom: '1px solid var(--t-border)',
              }}
              onMouseEnter={() => setCursor(i)}
            >
              <div>
                <div style={{ fontSize: 12, fontWeight: 700, color: 'var(--t-accent)', fontFamily: "'JetBrains Mono', monospace" }}>
                  {s.tradingCode}
                </div>
                <div style={{ fontSize: 9, color: 'var(--t-text3)' }}>
                  {(s.stockName ?? s.companyName ?? '').slice(0, 28)}
                  {s.category && <span style={{ marginLeft: 6, color: 'var(--t-text3)', fontWeight: 700 }}>[{s.category}]</span>}
                </div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--t-text1)', fontFamily: "'JetBrains Mono', monospace" }}>
                  ৳{(s.lastPrice ?? s.lastTradePrice ?? 0).toFixed(2)}
                </div>
                <div style={{ fontSize: 9, color: s.exchange === 'DSE' ? '#60a5fa' : '#a78bfa', fontFamily: "'JetBrains Mono', monospace" }}>
                  {s.exchange ?? 'DSE'}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ─── Numeric field with up/down arrows ───────────────────────────────────────
function NumField({ label, value, onChange, prefix = '', step = 1, min = 0, tabIndex }: any) {
  const inputRef = useRef<HTMLInputElement>(null)

  const handleKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowUp')   { e.preventDefault(); onChange(String(Math.max(min, Number(value || 0) + step))) }
    if (e.key === 'ArrowDown') { e.preventDefault(); onChange(String(Math.max(min, Number(value || 0) - step))) }
  }

  return (
    <div>
      <Label text={label} />
      <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
        {prefix && (
          <span style={{
            position: 'absolute', left: 8, pointerEvents: 'none',
            color: 'var(--t-text3)', fontSize: 11, fontFamily: "'JetBrains Mono', monospace",
          }}>{prefix}</span>
        )}
        <input
          ref={inputRef}
          type="number" value={value} min={min} step={step}
          tabIndex={tabIndex}
          onChange={e => onChange(e.target.value)}
          onKeyDown={handleKey}
          style={{
            width: '100%', boxSizing: 'border-box',
            paddingLeft: prefix ? 22 : 10, paddingRight: 10, paddingTop: 7, paddingBottom: 7,
            background: 'var(--t-hover)', border: '1px solid var(--t-border)',
            borderRadius: 6, color: 'var(--t-text1)', fontSize: 12, outline: 'none',
            fontFamily: "'JetBrains Mono', monospace", fontWeight: 600,
          }}
          onFocus={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
          onBlur={e => e.currentTarget.style.borderColor = 'var(--t-border)'}
        />
      </div>
    </div>
  )
}

// ─── Row divider ─────────────────────────────────────────────────────────────
const HR = () => <div style={{ height: 1, background: 'var(--t-border)', margin: '2px 0' }} />

// ─── Main Component ───────────────────────────────────────────────────────────
export function BuySellConsole() {
  const { place, placing } = useOrders()
  const { ticksArray } = useMarketData()
  const user = useAuthStore(s => s.user)

  // Panel visibility
  const [open, setOpen]     = useState(false)
  const [side, setSide]     = useState<Side>('BUY')

  // BO / Client
  const [clients, setClients]             = useState<BOClient[]>([])
  const [boQuery, setBoQuery]             = useState('')
  const [selectedClient, setSelectedClient] = useState<BOClient | null>(null)
  const [clientsLoading, setClientsLoading] = useState(false)

  // Order fields
  const [exchange, setExchange]   = useState<Exchange>('DSE')
  const [market, setMarket]       = useState<Market>('Public')
  const [symbol, setSymbol]       = useState('')
  const [priceType, setPriceType] = useState<PriceType>('Limit')
  const [qty, setQty]             = useState('')
  const [price, setPrice]         = useState('')
  const [displayQty, setDisplayQty] = useState('')
  const [tif, setTif]             = useState<TimeInForce>('Day')
  const [minQty, setMinQty]       = useState('')
  const [isPrivate, setIsPrivate] = useState(false)

  // UI state
  const [confirm, setConfirm]   = useState(false)
  const [result, setResult]     = useState<{ ok: boolean; text: string } | null>(null)
  const [warn, setWarn]         = useState<string | null>(null)

  // Derived
  const liveStock = useMemo(() =>
    ticksArray.find(s => s.tradingCode === symbol),
    [ticksArray, symbol]
  )

  const orderValue = qty && price ? Number(qty) * Number(price) : 0

  const purchasePower = selectedClient
    ? selectedClient.cashBalance + selectedClient.availableMargin
    : null

  // Load BO accounts once on open
  const loadClients = useCallback(async () => {
    if (clients.length > 0) return
    setClientsLoading(true)
    try {
      const raw: any = await getBOAccounts()
      const list = Array.isArray(raw) ? raw : raw?.items ?? raw?.data ?? []
      setClients(list.map((c: any) => ({
        userId:         c.userId,
        fullName:       c.fullName,
        boNumber:       c.boNumber ?? c.bONumber ?? '',
        cashBalance:    c.cashBalance ?? 0,
        marginLimit:    c.marginLimit ?? 0,
        availableMargin: c.availableMargin ?? 0,
        accountType:    c.accountType ?? '',
      })))
    } catch {
      // silently fall back — trader may not have CCD role
    } finally {
      setClientsLoading(false)
    }
  }, [clients.length])

  // Event bus
  useEffect(() => {
    const fn: ConsoleListener = (state) => {
      if (state.open !== undefined) setOpen(state.open)
      if (state.side) setSide(state.side)
      if (state.symbol !== undefined) {
        setSymbol(state.symbol)
        const tick = ticksArray.find(s => s.tradingCode === state.symbol)
        if (tick?.lastPrice) setPrice(tick.lastPrice.toFixed(2))
      }
    }
    _listeners.add(fn)
    return () => { _listeners.delete(fn) }
  }, [ticksArray])

  // Auto-fill price from live data
  useEffect(() => {
    if (liveStock?.lastPrice && !price) setPrice(liveStock.lastPrice.toFixed(2))
  }, [liveStock?.lastPrice])

  // Load clients when panel opens
  useEffect(() => { if (open) loadClients() }, [open])

  const doOpen = useCallback((s: Side) => {
    setSide(s); setOpen(true)
  }, [])

  const doClose = useCallback(() => {
    setOpen(false); setConfirm(false); setResult(null); setWarn(null)
  }, [])

  useGlobalShortcuts(() => doOpen('BUY'), () => doOpen('SELL'), doClose)

  const handleClientSelect = (c: BOClient) => {
    setSelectedClient(c)
    setBoQuery(c.boNumber)
  }

  const handleSymbolSelect = (s: any) => {
    setSymbol(s.tradingCode)
    const lp = s.lastPrice ?? s.lastTradePrice ?? 0
    if (lp) setPrice(lp.toFixed(2))
  }

  const validate = () => {
    if (!symbol) { setWarn('Symbol is required'); return false }
    if (!qty || Number(qty) <= 0) { setWarn('Quantity must be positive'); return false }
    if (priceType !== 'Market' && priceType !== 'MarketAtBest' && (!price || Number(price) <= 0)) {
      setWarn('Price is required for Limit orders'); return false
    }
    setWarn(null); return true
  }

  const handleSubmit = () => { if (validate()) setConfirm(true) }

  const handleConfirm = async () => {
    setConfirm(false)
    try {
      const dto = {
        stockId:       liveStock?.stockId ?? 0,
        orderType:     side === 'BUY' ? 0 : 1,
        orderCategory: priceType === 'Limit' ? 1 : 0,
        quantity:      Number(qty),
        limitPrice:    priceType === 'Limit' ? Number(price) : undefined,
        investorId:    selectedClient?.userId ?? undefined,
      }
      await place(dto)
      setResult({ ok: true, text: `✓ ${side} ${qty} × ${symbol} placed` })
      setTimeout(() => { setResult(null); doClose() }, 2000)
    } catch (e: any) {
      setResult({ ok: false, text: e?.message ?? 'Order placement failed' })
    }
  }

  const reset = () => {
    setSymbol(''); setQty(''); setPrice(''); setDisplayQty(''); setMinQty('')
    setPriceType('Limit'); setTif('Day'); setIsPrivate(false)
    setSelectedClient(null); setBoQuery('')
    setConfirm(false); setResult(null); setWarn(null)
  }

  if (!open) return null

  const isBuy      = side === 'BUY'
  const sideColor  = isBuy ? 'var(--t-buy)' : 'var(--t-sell)'
  const sideBg     = isBuy ? 'rgba(0,230,118,0.06)' : 'rgba(255,23,68,0.06)'
  const sideText   = isBuy ? '#000' : '#fff'

  return (
    <>
      {/* Backdrop */}
      <div onClick={doClose} style={{
        position: 'fixed', inset: 0, zIndex: 9990,
        background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(3px)',
      }} />

      {/* Panel */}
      <div style={{
        position: 'fixed', top: '50%', left: '50%', zIndex: 9991,
        transform: 'translate(-50%, -50%)',
        width: 520, maxWidth: '96vw', maxHeight: '92vh', overflowY: 'auto',
        background: 'var(--t-surface)', border: `1px solid ${sideColor}30`,
        borderRadius: 14, overflow: 'hidden',
        boxShadow: `0 32px 64px rgba(0,0,0,0.7), 0 0 0 1px ${sideColor}15`,
        animation: 'oms-slide-up 0.18s ease',
        display: 'flex', flexDirection: 'column',
      }}>
        {/* Top accent line */}
        <div style={{ height: 2, flexShrink: 0, background: `linear-gradient(90deg, transparent, ${sideColor}, transparent)`, opacity: 0.7 }} />

        {/* ── Header ── */}
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '11px 16px 10px', background: sideBg,
          borderBottom: '1px solid var(--t-border)', flexShrink: 0,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            {/* BUY / SELL toggle */}
            <div style={{ display: 'flex', borderRadius: 8, overflow: 'hidden', border: '1px solid var(--t-border)' }}>
              {(['BUY', 'SELL'] as Side[]).map(s => (
                <button key={s} onClick={() => setSide(s)} style={{
                  padding: '5px 16px', border: 'none', cursor: 'pointer',
                  fontSize: 11, fontWeight: 800, letterSpacing: '0.06em',
                  fontFamily: "'JetBrains Mono', monospace", transition: 'all 0.1s',
                  background: side === s ? (s === 'BUY' ? 'var(--t-buy)' : 'var(--t-sell)') : 'var(--t-hover)',
                  color: side === s ? (s === 'BUY' ? '#000' : '#fff') : 'var(--t-text3)',
                }}>{s === 'BUY' ? 'F1  BUY' : 'F2  SELL'}</button>
              ))}
            </div>
            <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>
              ESC to close
            </span>
          </div>
          <button onClick={doClose} style={{ background: 'none', border: 'none', color: 'var(--t-text3)', cursor: 'pointer', fontSize: 18, lineHeight: 1 }}>✕</button>
        </div>

        {/* ── Body ── */}
        <div style={{ padding: '14px 16px', display: 'flex', flexDirection: 'column', gap: 12, overflowY: 'auto' }}>

          {/* ── BO Code ── */}
          <div>
            <Label text="BO CODE / CLIENT" />
            <BOCodeSearch
              clients={clients}
              boQuery={boQuery}
              setBoQuery={setBoQuery}
              onSelect={handleClientSelect}
              selectedClient={selectedClient}
            />
          </div>

          {/* ── Client Info Card ── */}
          {selectedClient && (
            <div style={{
              display: 'grid', gridTemplateColumns: '1fr 1fr 1fr',
              gap: 8, background: 'var(--t-panel)',
              borderRadius: 8, padding: '10px 12px',
              border: `1px solid ${sideColor}20`,
            }}>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", marginBottom: 2 }}>CLIENT</div>
                <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)' }}>{selectedClient.fullName}</div>
                <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>{selectedClient.accountType}</div>
              </div>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", marginBottom: 2 }}>AVAILABLE CASH</div>
                <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--t-buy)', fontFamily: "'JetBrains Mono', monospace" }}>
                  ৳{selectedClient.cashBalance.toLocaleString()}
                </div>
              </div>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", marginBottom: 2 }}>PURCHASE POWER</div>
                <div style={{ fontSize: 13, fontWeight: 700, color: 'var(--t-accent)', fontFamily: "'JetBrains Mono', monospace" }}>
                  ৳{purchasePower?.toLocaleString()}
                </div>
              </div>
            </div>
          )}

          <HR />

          {/* ── Exchange + Market ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <div>
              <Label text="EXCHANGE" />
              <Seg options={['DSE', 'CSE'] as Exchange[]} value={exchange} onChange={setExchange} accent="var(--t-accent)" />
            </div>
            <div>
              <Label text="MARKET" />
              <Seg options={['Public', 'SME', 'ATB', 'GOV'] as Market[]} value={market} onChange={setMarket} accent="var(--t-accent)" />
            </div>
          </div>

          {/* ── Symbol ── */}
          <div>
            <Label text="SYMBOL" />
            <SymbolSearch
              value={symbol}
              onChange={(v: string) => { setSymbol(v); setPrice('') }}
              onSelect={handleSymbolSelect}
              stocks={ticksArray}
            />
          </div>

          {/* ── Live price strip ── */}
          {liveStock && (
            <div style={{
              display: 'flex', gap: 12, alignItems: 'center',
              background: 'var(--t-panel)', borderRadius: 8, padding: '8px 12px',
              border: '1px solid var(--t-border)',
            }}>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>LAST</div>
                <div style={{ fontSize: 16, fontWeight: 700, color: 'var(--t-text1)', fontFamily: "'JetBrains Mono', monospace" }}>৳{liveStock.lastPrice?.toFixed(2)}</div>
              </div>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>CHG%</div>
                <div style={{ fontSize: 12, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: (liveStock.changePercent ?? 0) >= 0 ? 'var(--t-buy)' : 'var(--t-sell)' }}>
                  {(liveStock.changePercent ?? 0) >= 0 ? '▲' : '▼'}{Math.abs(liveStock.changePercent ?? 0).toFixed(2)}%
                </div>
              </div>
              <div>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>VOL</div>
                <div style={{ fontSize: 11, color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace" }}>{((liveStock.volume ?? 0) / 1000).toFixed(0)}K</div>
              </div>
              <div style={{ marginLeft: 'auto' }}>
                <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>H / L</div>
                <div style={{ fontSize: 10, color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace" }}>৳{liveStock.highPrice?.toFixed(2)} / ৳{liveStock.lowPrice?.toFixed(2)}</div>
              </div>
            </div>
          )}

          <HR />

          {/* ── Price Type ── */}
          <div>
            <Label text="PRICE TYPE" />
            <Seg options={['Limit', 'Market', 'MarketAtBest'] as PriceType[]} value={priceType} onChange={setPriceType} accent={sideColor} />
          </div>

          {/* ── Qty + Price ── */}
          <div style={{ display: 'grid', gridTemplateColumns: priceType === 'Market' ? '1fr' : '1fr 1fr', gap: 10 }}>
            <NumField label="QUANTITY" value={qty} onChange={setQty} step={1} min={1} tabIndex={1} />
            {priceType === 'Limit' && (
              <NumField label="PRICE ৳" value={price} onChange={setPrice} prefix="৳" step={0.01} min={0.01} tabIndex={2} />
            )}
            {priceType === 'MarketAtBest' && (
              <NumField label="BEST PRICE ৳" value={price} onChange={setPrice} prefix="৳" step={0.01} min={0.01} tabIndex={2} />
            )}
          </div>

          {/* ── Display Qty + Min Qty ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <NumField label="DISPLAY QTY (STEALTH)" value={displayQty} onChange={setDisplayQty} step={1} min={0} tabIndex={3} />
            <NumField label="MIN QTY" value={minQty} onChange={setMinQty} step={1} min={0} tabIndex={4} />
          </div>

          {/* ── Order Time + Private ── */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 10, alignItems: 'end' }}>
            <div>
              <Label text="ORDER TIME" />
              <Seg options={['Day', 'IOC', 'FOK'] as TimeInForce[]} value={tif} onChange={setTif} accent={sideColor} />
            </div>
            <div style={{ paddingBottom: 1 }}>
              <label style={{
                display: 'flex', alignItems: 'center', gap: 7, cursor: 'pointer',
                fontSize: 11, fontWeight: 600, color: isPrivate ? sideColor : 'var(--t-text3)',
                fontFamily: "'JetBrains Mono', monospace",
                background: 'var(--t-hover)', border: `1px solid ${isPrivate ? sideColor + '60' : 'var(--t-border)'}`,
                borderRadius: 7, padding: '7px 12px', userSelect: 'none', transition: 'all 0.1s',
              }}>
                <input type="checkbox" checked={isPrivate} onChange={e => setIsPrivate(e.target.checked)}
                  style={{ accentColor: sideColor, width: 13, height: 13 }} />
                PRIVATE ORDER
              </label>
            </div>
          </div>

          {/* ── Order value ── */}
          {orderValue > 0 && (
            <div style={{
              display: 'flex', justifyContent: 'space-between', alignItems: 'center',
              background: 'var(--t-panel)', borderRadius: 8, padding: '9px 12px',
              border: `1px solid ${sideColor}20`,
            }}>
              <span style={{ fontSize: 10, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>ORDER VALUE</span>
              <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
                {selectedClient && purchasePower !== null && (
                  <span style={{
                    fontSize: 10, fontFamily: "'JetBrains Mono', monospace",
                    color: orderValue <= purchasePower ? 'var(--t-buy)' : 'var(--t-sell)',
                  }}>
                    {orderValue <= purchasePower ? '✓ within power' : '⚠ exceeds power'}
                  </span>
                )}
                <span style={{ fontSize: 15, fontWeight: 700, color: sideColor, fontFamily: "'JetBrains Mono', monospace" }}>
                  ৳{orderValue.toLocaleString('en-BD', { minimumFractionDigits: 2 })}
                </span>
              </div>
            </div>
          )}

          {/* Warnings / Results */}
          {warn && (
            <div style={{ background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.3)', borderRadius: 6, padding: '7px 10px', color: '#f59e0b', fontSize: 11 }}>
              ⚠ {warn}
            </div>
          )}
          {result && (
            <div style={{
              background: result.ok ? 'rgba(0,230,118,0.08)' : 'rgba(255,23,68,0.08)',
              border: `1px solid ${result.ok ? 'rgba(0,230,118,0.3)' : 'rgba(255,23,68,0.3)'}`,
              borderRadius: 6, padding: '7px 10px',
              color: result.ok ? 'var(--t-buy)' : 'var(--t-sell)', fontSize: 11,
            }}>{result.text}</div>
          )}

          {/* ── Action buttons ── */}
          {!confirm ? (
            <div style={{ display: 'flex', gap: 8, marginTop: 2 }}>
              <button
                onClick={handleSubmit}
                disabled={placing}
                tabIndex={5}
                style={{
                  flex: 1, padding: '12px', fontSize: 13, fontWeight: 800,
                  borderRadius: 8, border: 'none', cursor: placing ? 'wait' : 'pointer',
                  background: sideColor, color: sideText,
                  letterSpacing: '0.04em', fontFamily: "'JetBrains Mono', monospace",
                  opacity: placing ? 0.7 : 1, transition: 'opacity 0.12s',
                }}
              >{placing ? 'Placing…' : `${side}  ${symbol || '—'}  ${qty ? `× ${qty}` : ''}`}</button>
              <button onClick={reset} tabIndex={6} style={{
                padding: '12px 16px', fontSize: 11, borderRadius: 8,
                border: '1px solid var(--t-border)', cursor: 'pointer',
                background: 'transparent', color: 'var(--t-text3)',
                fontFamily: "'JetBrains Mono', monospace",
              }}>Reset</button>
            </div>
          ) : (
            <div style={{
              background: sideBg, border: `1px solid ${sideColor}30`,
              borderRadius: 10, padding: '12px 14px',
            }}>
              <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--t-text1)', marginBottom: 8, fontFamily: "'JetBrains Mono', monospace" }}>
                Confirm {side} Order
              </div>
              <div style={{ fontSize: 11, color: 'var(--t-text2)', lineHeight: 2, fontFamily: "'JetBrains Mono', monospace", marginBottom: 10 }}>
                {selectedClient && <>{selectedClient.fullName} ({selectedClient.boNumber})<br /></>}
                {side} {qty} × {symbol} @ {priceType === 'Market' ? 'Market' : `৳${price}`}<br />
                Exchange: {exchange} · Market: {market} · TIF: {tif}<br />
                {displayQty && <>Display Qty: {displayQty}<br /></>}
                {isPrivate && <span style={{ color: sideColor }}>🔒 PRIVATE ORDER<br /></span>}
                Value: ৳{orderValue.toLocaleString()}
              </div>
              <div style={{ display: 'flex', gap: 8 }}>
                <button onClick={handleConfirm} tabIndex={7} style={{
                  flex: 1, padding: '10px', fontSize: 12, fontWeight: 800,
                  borderRadius: 7, border: 'none', cursor: 'pointer',
                  background: sideColor, color: sideText,
                  fontFamily: "'JetBrains Mono', monospace",
                }}>Confirm &amp; Place</button>
                <button onClick={() => setConfirm(false)} tabIndex={8} style={{
                  flex: 1, padding: '10px', fontSize: 12, borderRadius: 7,
                  border: '1px solid var(--t-border)', cursor: 'pointer',
                  background: 'var(--t-hover)', color: 'var(--t-text2)',
                  fontFamily: "'JetBrains Mono', monospace",
                }}>Back</button>
              </div>
            </div>
          )}
        </div>

        {/* ── Footer ── */}
        <div style={{
          padding: '7px 16px', borderTop: '1px solid var(--t-border)',
          background: 'var(--t-panel)', display: 'flex', justifyContent: 'space-between',
          alignItems: 'center', flexShrink: 0,
        }}>
          <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>
            F1 Buy · F2 Sell · ESC Close · ↑↓ Navigate
          </span>
          <span style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>
            {liveStock ? `● LIVE ${symbol}` : '○ No symbol'}
            {selectedClient ? ` · ${selectedClient.boNumber}` : ''}
          </span>
        </div>
      </div>
    </>
  )
}

// ─── Hover trigger (for watchlist rows etc.) ──────────────────────────────────
export function BuySellHoverTrigger({ symbol }: { symbol?: string }) {
  const [visible, setVisible] = useState(false)
  const timer = useRef<ReturnType<typeof setTimeout>>()
  const show = () => { clearTimeout(timer.current); setVisible(true) }
  const hide = () => { timer.current = setTimeout(() => setVisible(false), 300) }

  return (
    <div style={{ position: 'relative', display: 'inline-flex' }}
      onMouseEnter={show} onMouseLeave={hide}>
      <div style={{ width: 8, height: '100%', position: 'absolute', left: 0, top: 0 }} />
      {visible && (
        <div style={{
          position: 'absolute', left: 0, top: '50%', transform: 'translateY(-50%)',
          display: 'flex', gap: 3, zIndex: 100, animation: 'oms-fade-in 0.12s ease',
        }}
          onMouseEnter={show} onMouseLeave={hide}>
          <button onClick={() => BuySellConsoleEvents.open('BUY', symbol)} style={{
            padding: '3px 8px', fontSize: 9, fontWeight: 800, borderRadius: 4,
            border: 'none', cursor: 'pointer', background: 'var(--t-buy)',
            color: '#000', fontFamily: "'JetBrains Mono', monospace",
          }}>B</button>
          <button onClick={() => BuySellConsoleEvents.open('SELL', symbol)} style={{
            padding: '3px 8px', fontSize: 9, fontWeight: 800, borderRadius: 4,
            border: 'none', cursor: 'pointer', background: 'var(--t-sell)',
            color: '#fff', fontFamily: "'JetBrains Mono', monospace",
          }}>S</button>
        </div>
      )}
    </div>
  )
}
