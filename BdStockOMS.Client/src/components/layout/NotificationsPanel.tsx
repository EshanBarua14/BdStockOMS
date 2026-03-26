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
