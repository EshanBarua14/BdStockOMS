#!/usr/bin/env bash
set -e
ROOT="/e/Projects/BdStockOMS"
LAYOUT="$ROOT/BdStockOMS.Client/src/components/layout"
TRADING="$ROOT/BdStockOMS.Client/src/components/trading"
WIDGETS="$ROOT/BdStockOMS.Client/src/components/widgets"
cd "$ROOT"

echo "╔══════════════════════════════════════════════════════╗"
echo "║  BdStockOMS — UI Fix Script                          ║"
echo "╚══════════════════════════════════════════════════════╝"

# ── #14/#11: Add NotificationsPanel + SettingsPanel to Topbar ──────────────
echo ""
echo "── Writing NotificationsPanel ──"
cat > "$LAYOUT/NotificationsPanel.tsx" << 'EOF'
// @ts-nocheck
import { useState } from 'react'
const mono = "'JetBrains Mono', monospace"

const MOCK_NOTIFS = [
  { id:1, type:"trade",   title:"Order Filled",        msg:"BUY 100 × GP @ ৳380.50",         time:"2m ago",  read:false },
  { id:2, type:"alert",   title:"Price Alert",          msg:"BRACBANK crossed ৳48.00",         time:"15m ago", read:false },
  { id:3, type:"system",  title:"Market Opening",       msg:"DSE continuous session started",  time:"1h ago",  read:true  },
  { id:4, type:"risk",    title:"RMS Warning",          msg:"Exposure limit at 85%",           time:"2h ago",  read:true  },
  { id:5, type:"trade",   title:"Order Rejected",       msg:"SELL 50 × BATBC — insufficient balance", time:"3h ago", read:true },
  { id:6, type:"news",    title:"Price Sensitive News", msg:"SQURPHARMA declares 30% dividend", time:"4h ago", read:true },
]

const TYPE_COLORS: Record<string,string> = {
  trade:"var(--t-accent)", alert:"#f59e0b", system:"#60a5fa", risk:"var(--t-sell)", news:"#a78bfa"
}

interface Props { onClose: () => void }

export function NotificationsPanel({ onClose }: Props) {
  const [notifs, setNotifs] = useState(MOCK_NOTIFS)
  const unread = notifs.filter(n => !n.read).length

  const markAll = () => setNotifs(n => n.map(x => ({ ...x, read: true })))
  const markOne = (id: number) => setNotifs(n => n.map(x => x.id === id ? { ...x, read: true } : x))

  return (
    <>
      <div onClick={onClose} style={{ position:'fixed', inset:0, zIndex:199 }} />
      <div style={{
        position:'fixed', top:52, right:14, zIndex:200, width:340,
        background:'var(--t-elevated)', border:'1px solid var(--t-border)',
        borderRadius:12, boxShadow:'0 16px 48px rgba(0,0,0,0.5)',
        display:'flex', flexDirection:'column', maxHeight:'80vh',
      }}>
        {/* Header */}
        <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', padding:'12px 14px', borderBottom:'1px solid var(--t-border)', flexShrink:0 }}>
          <div style={{ display:'flex', alignItems:'center', gap:8 }}>
            <span style={{ fontSize:13, fontWeight:800, color:'var(--t-text1)', fontFamily:mono }}>Notifications</span>
            {unread > 0 && <span style={{ fontSize:9, fontWeight:800, background:'var(--t-sell)', color:'#fff', borderRadius:10, padding:'1px 6px', fontFamily:mono }}>{unread}</span>}
          </div>
          <button onClick={markAll} style={{ fontSize:9, color:'var(--t-accent)', background:'none', border:'none', cursor:'pointer', fontFamily:mono }}>Mark all read</button>
        </div>

        {/* Sections */}
        {['Unread','Earlier'].map(section => {
          const items = section === 'Unread' ? notifs.filter(n => !n.read) : notifs.filter(n => n.read)
          if (!items.length) return null
          return (
            <div key={section}>
              <div style={{ fontSize:8, fontWeight:700, color:'var(--t-text3)', fontFamily:mono, padding:'8px 14px 4px', letterSpacing:'0.08em' }}>{section.toUpperCase()}</div>
              {items.map(n => (
                <div key={n.id} onClick={() => markOne(n.id)} style={{
                  display:'flex', gap:10, padding:'10px 14px', cursor:'pointer',
                  background:n.read ? 'transparent' : 'rgba(255,255,255,0.02)',
                  borderBottom:'1px solid var(--t-border)',
                }}
                  onMouseEnter={e => e.currentTarget.style.background='var(--t-hover)'}
                  onMouseLeave={e => e.currentTarget.style.background=n.read?'transparent':'rgba(255,255,255,0.02)'}
                >
                  <div style={{ width:6, height:6, borderRadius:'50%', background:TYPE_COLORS[n.type]??'var(--t-text3)', flexShrink:0, marginTop:4 }} />
                  <div style={{ flex:1 }}>
                    <div style={{ fontSize:11, fontWeight:n.read?500:700, color:'var(--t-text1)', marginBottom:2 }}>{n.title}</div>
                    <div style={{ fontSize:10, color:'var(--t-text2)', lineHeight:1.4 }}>{n.msg}</div>
                    <div style={{ fontSize:8, color:'var(--t-text3)', marginTop:3, fontFamily:mono }}>{n.time}</div>
                  </div>
                </div>
              ))}
            </div>
          )
        })}

        <div style={{ padding:'10px 14px', borderTop:'1px solid var(--t-border)', flexShrink:0 }}>
          <button style={{ width:'100%', padding:'7px', fontSize:10, fontFamily:mono, background:'transparent', border:'1px solid var(--t-border)', borderRadius:6, color:'var(--t-text2)', cursor:'pointer' }}>
            View all notifications
          </button>
        </div>
      </div>
    </>
  )
}
EOF
echo "  ✓ NotificationsPanel.tsx created"

echo ""
echo "── Writing SettingsPanel ──"
cat > "$LAYOUT/SettingsPanel.tsx" << 'EOF'
// @ts-nocheck
import { useState } from 'react'
import { ThemeMenu } from '@/components/ui/ThemeMenu'
const mono = "'JetBrains Mono', monospace"

const SECTIONS = [
  { id:'display',   label:'Display',        icon:'🖥' },
  { id:'trading',   label:'Trading',        icon:'⚡' },
  { id:'alerts',    label:'Alerts',         icon:'🔔' },
  { id:'ticker',    label:'Ticker',         icon:'📈' },
  { id:'keyboard',  label:'Keyboard',       icon:'⌨' },
  { id:'data',      label:'Data & Privacy', icon:'🔒' },
]

interface Props { onClose: () => void }

export function SettingsPanel({ onClose }: Props) {
  const [activeSection, setActiveSection] = useState('display')
  const [settings, setSettings] = useState({
    // Display
    showAnimations: true, compactMode: false, showVolumeBars: true,
    // Trading
    confirmOrders: true, boRequired: true, showRmsWarnings: true, defaultOrderType: 'Limit',
    // Alerts
    priceAlerts: true, orderAlerts: true, rmsAlerts: true, newsAlerts: true,
    // Ticker
    showTicker: true, tickerSpeed: 'normal', tickerFilter: 'all',
    // Keyboard
    f1Buy: true, f2Sell: true, escClose: true,
    // Data
    autoSaveLayout: true, analyticsEnabled: false,
  })

  const set = (key: string, val: any) => setSettings(s => ({ ...s, [key]: val }))

  const Toggle = ({ k }: { k: string }) => (
    <div onClick={() => set(k, !(settings as any)[k])} style={{
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

  const renderSection = () => {
    switch (activeSection) {
      case 'display': return (
        <>
          <Row label="Animations" desc="Widget transitions and flash effects"><Toggle k="showAnimations" /></Row>
          <Row label="Compact Mode" desc="Reduce padding for more data density"><Toggle k="compactMode" /></Row>
          <Row label="Volume Bars" desc="Show relative volume bars in tables"><Toggle k="showVolumeBars" /></Row>
          <Row label="Theme" desc="Color scheme">
            <ThemeMenu variant="compact" />
          </Row>
        </>
      )
      case 'trading': return (
        <>
          <Row label="Confirm Orders" desc="Show confirmation popup before placing"><Toggle k="confirmOrders" /></Row>
          <Row label="BO Code Required" desc="Mandatory BO selection in buy/sell console"><Toggle k="boRequired" /></Row>
          <Row label="RMS Warnings" desc="Show risk management warnings"><Toggle k="showRmsWarnings" /></Row>
          <Row label="Default Order Type" desc="Pre-select order type in console">
            <select value={settings.defaultOrderType} onChange={e => set('defaultOrderType', e.target.value)}
              style={{ background:'var(--t-hover)', border:'1px solid var(--t-border)', borderRadius:4, padding:'3px 8px', color:'var(--t-text1)', fontSize:10, fontFamily:mono, outline:'none' }}>
              <option>Limit</option><option>Market</option><option>Stop</option>
            </select>
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
            <select value={settings.tickerSpeed} onChange={e => set('tickerSpeed', e.target.value)}
              style={{ background:'var(--t-hover)', border:'1px solid var(--t-border)', borderRadius:4, padding:'3px 8px', color:'var(--t-text1)', fontSize:10, fontFamily:mono, outline:'none' }}>
              <option value="slow">Slow</option><option value="normal">Normal</option><option value="fast">Fast</option>
            </select>
          </Row>
          <Row label="Filter" desc="Which stocks appear in ticker">
            <select value={settings.tickerFilter} onChange={e => set('tickerFilter', e.target.value)}
              style={{ background:'var(--t-hover)', border:'1px solid var(--t-border)', borderRadius:4, padding:'3px 8px', color:'var(--t-text1)', fontSize:10, fontFamily:mono, outline:'none' }}>
              <option value="all">All Stocks</option>
              <option value="watchlist">Watchlist Only</option>
              <option value="dse">DSE Only</option>
              <option value="cse">CSE Only</option>
            </select>
          </Row>
        </>
      )
      case 'keyboard': return (
        <>
          <Row label="F1 = Buy" desc="Press F1 to open Buy console"><Toggle k="f1Buy" /></Row>
          <Row label="F2 = Sell" desc="Press F2 to open Sell console"><Toggle k="f2Sell" /></Row>
          <Row label="ESC = Close" desc="Press Escape to close dialogs"><Toggle k="escClose" /></Row>
          <Row label="Ctrl+K = Search" desc="Focus global search"><div style={{ fontSize:10, fontFamily:mono, color:'var(--t-text3)' }}>Built-in</div></Row>
        </>
      )
      case 'data': return (
        <>
          <Row label="Auto-save Layout" desc="Automatically save widget positions"><Toggle k="autoSaveLayout" /></Row>
          <Row label="Analytics" desc="Send anonymous usage data to improve the product"><Toggle k="analyticsEnabled" /></Row>
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
EOF
echo "  ✓ SettingsPanel.tsx created"

# ── Wire panels into Topbar ──────────────────────────────
echo ""
echo "── Wiring panels into Topbar ──"
# Add imports
sed -i "s|import { TopbarIconBtn } from \"./TopbarIconBtn\";|import { TopbarIconBtn } from \"./TopbarIconBtn\";\nimport { NotificationsPanel } from \"./NotificationsPanel\";\nimport { SettingsPanel } from \"./SettingsPanel\";|" \
  "$LAYOUT/Topbar.tsx"

# Add state for panels
sed -i "s/const \[searchFocused, setSearchFocused\] = useState(false)/const [searchFocused, setSearchFocused] = useState(false)\n  const [showNotifs, setShowNotifs] = useState(false)\n  const [showSettings, setShowSettings] = useState(false)/" \
  "$LAYOUT/Topbar.tsx"

# Wire notification button
sed -i "s/onClick={() => {}} \\/>\n\n.*{\\/\\* ── App Settings ── \\*\\/}/onClick={() => setShowNotifs(v => !v)} \/>/" \
  "$LAYOUT/Topbar.tsx"
sed -i "s/<TopbarIconBtn icon=\"🔔\" title=\"Notifications\" count={3} onClick={() => {}} \/>/<TopbarIconBtn icon=\"🔔\" title=\"Notifications\" count={3} onClick={() => { setShowNotifs(v => !v); setShowSettings(false) }} \/>/" \
  "$LAYOUT/Topbar.tsx"

# Wire news button
sed -i "s/<TopbarIconBtn icon=\"📰\" title=\"News\" count={0} onClick={() => {}} \/>/<TopbarIconBtn icon=\"📰\" title=\"News\" count={0} onClick={() => {}} \/>/" \
  "$LAYOUT/Topbar.tsx"

# Wire settings button - replace AppSettingsBtn with gear icon
sed -i "s/<AppSettingsBtn \/>/<TopbarIconBtn icon=\"⚙\" title=\"Settings\" count={0} onClick={() => { setShowSettings(v => !v); setShowNotifs(false) }} \/>/" \
  "$LAYOUT/Topbar.tsx"

# Add panels before closing header tag
sed -i "s|</header>|      {showNotifs   \&\& <NotificationsPanel onClose={() => setShowNotifs(false)} />}\n      {showSettings \&\& <SettingsPanel onClose={() => setShowSettings(false)} />}\n    </header>|" \
  "$LAYOUT/Topbar.tsx"

echo "  ✓ Topbar wired with Notifications + Settings panels"

# ── #17 Remove/hide global search from topbar ──────────────
echo ""
echo "── #17 Hide global search from topbar ──"
# Comment out the center search div by reducing its display
sed -i "s/flex: 1, display: 'flex', justifyContent: 'center', minWidth: 0, padding: '0 8px'/display: 'none'/" \
  "$LAYOUT/Topbar.tsx"
echo "  ✓ Global search hidden from topbar"

# ── #20 Sidebar: add more menu items ──────────────────────
echo ""
echo "── #20 Expanding Sidebar menu ──"
sed -i "s/const NAV_ITEMS: NavItem\[\] = \[/const NAV_ITEMS: NavItem[] = [/" \
  "$LAYOUT/Sidebar.tsx" 2>/dev/null || true

# Add missing nav items to NAV_ITEMS array
perl -i -0pe "
  s/(\{ label: 'RBAC \& Roles',.*?section: 'Admin' \},)/\$1
  { label: 'Trade Monitor',    path: '\/trade-monitor', Icon: Icon.Market,     roles: ['SuperAdmin','Admin','BrokerageAdmin','Trader'], section: 'Monitor' },
  { label: 'Market Watch',     path: '\/market',        Icon: Icon.Market,     section: 'Market'  },
  { label: 'News \& Research', path: '\/news',          Icon: Icon.Portfolio,  section: 'Market'  },
  { label: 'Risk Management',  path: '\/rms',           Icon: Icon.Admin,      roles: ['SuperAdmin','Admin','BrokerageAdmin'], section: 'Risk' },
  { label: 'Reports',          path: '\/reports',       Icon: Icon.Orders,     roles: ['SuperAdmin','Admin','BrokerageAdmin','Trader'], section: 'Reports' },
  { label: 'Audit Log',        path: '\/audit',         Icon: Icon.Orders,     roles: ['SuperAdmin','Admin'], section: 'Reports' },
  { label: 'KYC',              path: '\/kyc',           Icon: Icon.RBAC,       roles: ['SuperAdmin','Admin','BrokerageAdmin'], section: 'Compliance' },
  { label: 'Settlements',      path: '\/settlements',   Icon: Icon.Orders,     roles: ['SuperAdmin','Admin','BrokerageAdmin'], section: 'Compliance' },
  { label: 'System Settings',  path: '\/settings',      Icon: Icon.Admin,      roles: ['SuperAdmin','Admin'], section: 'System' },
/s
" "$LAYOUT/Sidebar.tsx"

echo "  ✓ Sidebar menu items expanded"

# ── VERIFY ────────────────────────────────────────────────
echo ""
echo "── VERIFY ──"
grep -c "NotificationsPanel\|SettingsPanel" "$LAYOUT/Topbar.tsx" && echo "✓ Panels wired in Topbar" || echo "✗ Panels missing"
grep -c "display: 'none'" "$LAYOUT/Topbar.tsx" && echo "✓ Global search hidden" || echo "✗ Search still visible"
grep -c "showNotifs\|showSettings" "$LAYOUT/Topbar.tsx" && echo "✓ Panel state in Topbar" || echo "✗ Panel state missing"
[ -f "$LAYOUT/NotificationsPanel.tsx" ] && echo "✓ NotificationsPanel.tsx exists" || echo "✗ NotificationsPanel missing"
[ -f "$LAYOUT/SettingsPanel.tsx" ]      && echo "✓ SettingsPanel.tsx exists"      || echo "✗ SettingsPanel missing"

# ── COMMIT ────────────────────────────────────────────────
echo ""
git add \
  "$LAYOUT/Topbar.tsx" \
  "$LAYOUT/Sidebar.tsx" \
  "$LAYOUT/NotificationsPanel.tsx" \
  "$LAYOUT/SettingsPanel.tsx"

git commit -m "Fixes #11/#14/#17/#20: Notifications panel, Settings panel (6 sections), global search hidden, sidebar menu expanded"
git push origin day-63-widget-redesign-polish

echo "╔══════════════════════════════════════════════════════╗"
echo "║  UI fixes done. ✓                                    ║"
echo "╚══════════════════════════════════════════════════════╝"
