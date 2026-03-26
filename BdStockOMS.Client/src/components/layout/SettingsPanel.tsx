// @ts-nocheck
// src/components/layout/SettingsPanel.tsx
import { useSettingsStore } from '@/store/useSettingsStore'
import { ThemeMenu } from '@/components/ui/ThemeMenu'

const mono = "'JetBrains Mono', monospace"

const SECTIONS = [
  { id:'display',  label:'Display',        icon:'🖥' },
  { id:'trading',  label:'Trading',        icon:'⚡' },
  { id:'alerts',   label:'Alerts',         icon:'🔔' },
  { id:'ticker',   label:'Ticker',         icon:'📈' },
  { id:'keyboard', label:'Keyboard',       icon:'⌨' },
  { id:'data',     label:'Data & Privacy', icon:'🔒' },
]

import { useState } from 'react'

interface Props { onClose: () => void }

export function SettingsPanel({ onClose }: Props) {
  const [activeSection, setActiveSection] = useState('display')
  const { settings, set, reset } = useSettingsStore()

  const Toggle = ({ k }: { k: string }) => (
    <div onClick={() => set(k as any, !(settings as any)[k])} style={{
      width:34, height:18, borderRadius:9, cursor:'pointer', transition:'background 0.15s',
      background:(settings as any)[k] ? 'var(--t-accent)' : 'var(--t-hover)',
      border:'1px solid var(--t-border)', position:'relative', flexShrink:0,
    }}>
      <div style={{ position:'absolute', top:2, left:(settings as any)[k]?16:2, width:14, height:14, borderRadius:'50%', background:'#fff', transition:'left 0.15s' }} />
    </div>
  )

  const Row = ({ label, desc, children }: any) => (
    <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', padding:'10px 0', borderBottom:'1px solid var(--t-border)' }}>
      <div>
        <div style={{ fontSize:11, fontWeight:600, color:'var(--t-text1)' }}>{label}</div>
        {desc && <div style={{ fontSize:9, color:'var(--t-text3)', marginTop:2, fontFamily:mono }}>{desc}</div>}
      </div>
      {children}
    </div>
  )

  const Select = ({ k, options }: { k: string, options: {value:string, label:string}[] }) => (
    <select value={(settings as any)[k]} onChange={e => set(k as any, e.target.value)}
      style={{ background:'var(--t-hover)', border:'1px solid var(--t-border)', borderRadius:4, padding:'3px 8px', color:'var(--t-text1)', fontSize:10, fontFamily:mono, outline:'none' }}>
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  )

  const renderSection = () => {
    switch (activeSection) {
      case 'display': return (
        <>
          <Row label="Animations" desc="Widget transitions and flash effects"><Toggle k="showAnimations" /></Row>
          <Row label="Compact Mode" desc="Reduce padding for more data density"><Toggle k="compactMode" /></Row>
          <Row label="Volume Bars" desc="Show relative volume bars in tables"><Toggle k="showVolumeBars" /></Row>
          <Row label="Theme" desc="Color scheme"><ThemeMenu variant="compact" /></Row>
        </>
      )
      case 'trading': return (
        <>
          <Row label="Confirm Orders" desc="Show confirmation popup before placing"><Toggle k="confirmOrders" /></Row>
          <Row label="BO Code Required" desc="Mandatory BO selection in buy/sell console"><Toggle k="boRequired" /></Row>
          <Row label="RMS Warnings" desc="Show risk management warnings"><Toggle k="showRmsWarnings" /></Row>
          <Row label="Default Order Type" desc="Pre-select order type in console">
            <Select k="defaultOrderType" options={[{value:'Limit',label:'Limit'},{value:'Market',label:'Market'},{value:'Stop',label:'Stop'}]} />
          </Row>
        </>
      )
      case 'alerts': return (
        <>
          <Row label="Price Alerts" desc="Notify when price crosses set level"><Toggle k="priceAlerts" /></Row>
          <Row label="Order Alerts" desc="Notify on order fill, rejection, cancel"><Toggle k="orderAlerts" /></Row>
          <Row label="RMS Alerts" desc="Notify on risk limit warnings"><Toggle k="rmsAlerts" /></Row>
          <Row label="News Alerts" desc="Notify on price-sensitive news"><Toggle k="newsAlerts" /></Row>
        </>
      )
      case 'ticker': return (
        <>
          <Row label="Show Ticker" desc="Display scrolling price ticker bar"><Toggle k="showTicker" /></Row>
          <Row label="Speed" desc="Ticker scroll speed">
            <Select k="tickerSpeed" options={[{value:'slow',label:'Slow'},{value:'normal',label:'Normal'},{value:'fast',label:'Fast'}]} />
          </Row>
          <Row label="Filter" desc="Which stocks appear in ticker">
            <Select k="tickerFilter" options={[
              {value:'all',label:'All Stocks'},
              {value:'watchlist',label:'Watchlist Only'},
              {value:'dse',label:'DSE Only'},
              {value:'cse',label:'CSE Only'},
            ]} />
          </Row>
        </>
      )
      case 'keyboard': return (
        <>
          <Row label="F1 = Buy" desc="Press F1 to open Buy console"><Toggle k="f1Buy" /></Row>
          <Row label="F2 = Sell" desc="Press F2 to open Sell console"><Toggle k="f2Sell" /></Row>
          <Row label="ESC = Close" desc="Press Escape to close dialogs"><Toggle k="escClose" /></Row>
          <Row label="Ctrl+K = Search" desc="Focus global search">
            <div style={{ fontSize:10, fontFamily:mono, color:'var(--t-text3)' }}>Built-in</div>
          </Row>
        </>
      )
      case 'data': return (
        <>
          <Row label="Auto-save Layout" desc="Automatically save widget positions"><Toggle k="autoSaveLayout" /></Row>
          <Row label="Analytics" desc="Send anonymous usage data to improve the product"><Toggle k="analyticsEnabled" /></Row>
          <Row label="Reset Settings" desc="Restore all settings to defaults">
            <button onClick={reset}
              style={{ padding:'4px 10px', fontSize:9, fontFamily:mono, background:'rgba(255,152,0,0.08)', border:'1px solid rgba(255,152,0,0.2)', borderRadius:4, color:'var(--t-accent)', cursor:'pointer' }}>
              Reset
            </button>
          </Row>
          <Row label="Clear Layout Cache" desc="Reset all widget positions to default">
            <button onClick={() => { localStorage.removeItem('bd_oms_templates_v2'); window.location.reload() }}
              style={{ padding:'4px 10px', fontSize:9, fontFamily:mono, background:'rgba(255,23,68,0.08)', border:'1px solid rgba(255,23,68,0.2)', borderRadius:4, color:'var(--t-sell)', cursor:'pointer' }}>
              Reset
            </button>
          </Row>
        </>
      )
      default: return null
    }
  }

  return (
    <>
      <div onClick={onClose} style={{ position:'fixed', inset:0, zIndex:199 }} />
      <div style={{
        position:'fixed', top:52, right:14, zIndex:200, width:480,
        background:'var(--t-elevated)', border:'1px solid var(--t-border)',
        borderRadius:12, boxShadow:'0 16px 48px rgba(0,0,0,0.5)',
        display:'flex', maxHeight:'80vh', overflow:'hidden',
      }}>
        {/* Left nav */}
        <div style={{ width:140, borderRight:'1px solid var(--t-border)', padding:'12px 0', flexShrink:0, overflowY:'auto' }}>
          <div style={{ fontSize:9, fontWeight:800, color:'var(--t-text3)', fontFamily:mono, padding:'0 14px 8px', letterSpacing:'0.08em' }}>SETTINGS</div>
          {SECTIONS.map(s => (
            <button key={s.id} onClick={() => setActiveSection(s.id)} style={{
              display:'flex', alignItems:'center', gap:8, width:'100%', padding:'8px 14px',
              background:activeSection===s.id?'var(--t-hover)':'transparent',
              border:'none', borderLeft:`2px solid ${activeSection===s.id?'var(--t-accent)':'transparent'}`,
              cursor:'pointer', color:activeSection===s.id?'var(--t-text1)':'var(--t-text3)',
              fontSize:11, fontFamily:mono, textAlign:'left',
            }}>
              <span>{s.icon}</span>{s.label}
            </button>
          ))}
        </div>

        {/* Right content */}
        <div style={{ flex:1, padding:'16px 18px', overflowY:'auto' }}>
          <div style={{ fontSize:13, fontWeight:800, color:'var(--t-text1)', fontFamily:mono, marginBottom:12 }}>
            {SECTIONS.find(s => s.id===activeSection)?.icon} {SECTIONS.find(s => s.id===activeSection)?.label}
          </div>
          {renderSection()}
        </div>
      </div>
    </>
  )
}
