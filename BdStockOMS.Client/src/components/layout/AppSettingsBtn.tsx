// @ts-nocheck
import { useState, useEffect, useRef } from 'react'
import { TopbarIconBtn } from './TopbarIconBtn'
import { apiClient } from '@/api/client'

const mono = "'JetBrains Mono', monospace"

export const AppSettingsBtn: React.FC = () => {
  const [open, setOpen] = useState(false)
  const [tab, setTab] = useState<'settings'|'notifications'|'news'>('settings')
  const [notifs, setNotifs] = useState<any[]>([])
  const [news, setNews] = useState<any[]>([])
  const [unread, setUnread] = useState(0)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    apiClient.get('/news?count=5').then(r => setNews(r.data?.length ? r.data : DEMO_NEWS)).catch(() => setNews(DEMO_NEWS))
    setNotifs(DEMO_NOTIFS)
    setUnread(DEMO_NOTIFS.filter(n => !n.read).length)
  }, [open])

  useEffect(() => {
    const close = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false) }
    if (open) window.addEventListener('mousedown', close)
    return () => window.removeEventListener('mousedown', close)
  }, [open])

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <TopbarIconBtn icon="⚙️" title="App Settings" count={unread} onClick={() => setOpen(o => !o)} />
      {open && (
        <div style={{
          position: 'absolute', right: 0, top: '110%', width: 320, zIndex: 9999,
          background: 'var(--t-surface)', border: '1px solid var(--t-border)',
          borderRadius: 12, boxShadow: '0 12px 40px rgba(0,0,0,0.4)', overflow: 'hidden',
        }}>
          {/* Tabs */}
          <div style={{ display: 'flex', borderBottom: '1px solid var(--t-border)' }}>
            {(['settings','notifications','news'] as const).map(t => (
              <button key={t} onClick={() => setTab(t)} style={{
                flex: 1, padding: '10px 0', fontSize: 10, fontWeight: tab === t ? 700 : 400,
                border: 'none', borderBottom: `2px solid ${tab === t ? 'var(--t-accent)' : 'transparent'}`,
                background: 'transparent', cursor: 'pointer', fontFamily: mono,
                color: tab === t ? 'var(--t-accent)' : 'var(--t-text3)', textTransform: 'uppercase',
              }}>{t === 'notifications' ? '🔔' : t === 'news' ? '📰' : '⚙️'} {t}</button>
            ))}
          </div>

          {/* Settings tab */}
          {tab === 'settings' && (
            <div style={{ padding: 16 }}>
              {[
                { label: 'Price Ticker', key: 'ticker' },
                { label: 'Sound Alerts', key: 'sound' },
                { label: 'Desktop Notifications', key: 'desktop' },
                { label: 'Auto-refresh Data', key: 'refresh' },
                { label: 'Compact View', key: 'compact' },
              ].map(item => (
                <div key={item.key} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid var(--t-border)' }}>
                  <span style={{ fontSize: 12, color: 'var(--t-text2)', fontFamily: mono }}>{item.label}</span>
                  <Toggle />
                </div>
              ))}
            </div>
          )}

          {/* Notifications tab */}
          {tab === 'notifications' && (
            <div style={{ maxHeight: 300, overflowY: 'auto' }}>
              {notifs.map(n => (
                <div key={n.id} onClick={() => setNotifs(ns => ns.map(x => x.id === n.id ? {...x, read:true} : x))}
                  style={{ padding: '10px 14px', borderBottom: '1px solid var(--t-border)', cursor: 'pointer', background: n.read ? 'transparent' : 'rgba(0,212,170,0.04)' }}>
                  <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                    <span style={{ fontSize: 14, flexShrink: 0 }}>{n.icon}</span>
                    <div>
                      <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--t-text1)', fontFamily: mono }}>{n.title}</div>
                      <div style={{ fontSize: 10, color: 'var(--t-text3)', marginTop: 2 }}>{n.body}</div>
                      <div style={{ fontSize: 9, color: 'var(--t-text3)', marginTop: 3, fontFamily: mono }}>{n.time}</div>
                    </div>
                    {!n.read && <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'var(--t-accent)', flexShrink: 0, marginTop: 4 }} />}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* News tab */}
          {tab === 'news' && (
            <div style={{ maxHeight: 300, overflowY: 'auto' }}>
              {news.map((n: any, i: number) => (
                <div key={n.id ?? i} style={{ padding: '10px 14px', borderBottom: '1px solid var(--t-border)', cursor: 'pointer' }}
                  onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
                  onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                  <div style={{ fontSize: 11, fontWeight: 600, color: 'var(--t-text1)', lineHeight: 1.4 }}>{n.title ?? n.headline}</div>
                  <div style={{ fontSize: 9, color: 'var(--t-text3)', marginTop: 3, fontFamily: mono }}>{n.source ?? 'BD Market'} · {n.category ?? 'Market'}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function Toggle() {
  const [on, setOn] = useState(false)
  return (
    <div onClick={() => setOn(o => !o)} style={{
      width: 32, height: 18, borderRadius: 9, cursor: 'pointer', transition: 'background 0.2s',
      background: on ? 'var(--t-accent)' : 'var(--t-border)', position: 'relative', flexShrink: 0,
    }}>
      <div style={{
        position: 'absolute', top: 2, left: on ? 14 : 2, width: 14, height: 14,
        borderRadius: '50%', background: '#fff', transition: 'left 0.2s',
      }} />
    </div>
  )
}

const DEMO_NOTIFS = [
  { id:1, icon:'📈', title:'Order Executed', body:'GP × 100 @ ৳380.50 executed', time:'2 min ago', read:false },
  { id:2, icon:'✅', title:'KYC Approved', body:'Pioneer Investor One KYC approved', time:'15 min ago', read:false },
  { id:3, icon:'⚠️', title:'RMS Warning', body:'Margin utilization at 78%', time:'1 hr ago', read:true },
  { id:4, icon:'📉', title:'Order Rejected', body:'BATBC × 50 rejected — insufficient funds', time:'2 hr ago', read:true },
  { id:5, icon:'🔔', title:'Market Open', body:'DSE & CSE market opened at 10:00 AM', time:'Today', read:true },
]

const DEMO_NEWS = [
  { id:1, title:'DSE index rises 1.2% on banking gains', source:'Daily Star', category:'Market' },
  { id:2, title:'BATBC reports 15% profit growth in Q3', source:'Financial Express', category:'Corporate' },
  { id:3, title:'Bangladesh Bank cuts policy rate 25bps', source:'Prothom Alo', category:'Economy' },
  { id:4, title:'GP subscriber base crosses 86 million', source:'BD News24', category:'Corporate' },
  { id:5, title:'BSEC approves 3 new mutual fund listings', source:'BSS', category:'Regulation' },
]
