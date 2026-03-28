// @ts-nocheck
// KeyboardHelp.tsx - Day 78
// Press ? to show all keyboard shortcuts

import { useEffect } from 'react'
import { SHORTCUTS } from '@/hooks/useKeyboardShortcuts'

const mono = "'JetBrains Mono', monospace"

function Kbd({ keys }: { keys: string }) {
  return (
    <span style={{display:'inline-flex',alignItems:'center',gap:3}}>
      {keys.split('+').map((k,i) => (
        <kbd key={i} style={{
          background:'var(--t-hover)',border:'1px solid var(--t-border)',
          borderRadius:4,padding:'2px 6px',fontSize:10,fontFamily:mono,
          color:'var(--t-text2)',minWidth:20,textAlign:'center',
        }}>{k.trim()}</kbd>
      ))}
    </span>
  )
}

export function KeyboardHelp({ open, onClose }: { open: boolean; onClose: () => void }) {
  useEffect(() => {
    if (!open) return
    const h = (e) => { if (e.key === 'Escape' || e.key === '?') { e.preventDefault(); onClose() } }
    window.addEventListener('keydown', h)
    return () => window.removeEventListener('keydown', h)
  }, [open, onClose])

  if (!open) return null

  const categories = [...new Set(SHORTCUTS.map(s => s.category))]

  const fmtKey = (s: typeof SHORTCUTS[0]) => {
    const parts = []
    if (s.ctrl)  parts.push('Ctrl')
    if (s.alt)   parts.push('Alt')
    if (s.shift) parts.push('Shift')
    parts.push(s.key)
    return parts.join(' + ')
  }

  return (
    <div
      onClick={onClose}
      style={{
        position:'fixed',inset:0,zIndex:9998,
        background:'rgba(0,0,0,0.6)',backdropFilter:'blur(4px)',
        display:'flex',alignItems:'center',justifyContent:'center',
      }}
    >
      <div
        onClick={e=>e.stopPropagation()}
        style={{
          width:'100%',maxWidth:540,borderRadius:12,overflow:'hidden',
          border:'1px solid var(--t-border)',background:'var(--t-panel)',
          boxShadow:'0 25px 60px rgba(0,0,0,0.5)',
        }}
      >
        {/* Header */}
        <div style={{padding:'14px 20px',borderBottom:'1px solid var(--t-border)',display:'flex',alignItems:'center',justifyContent:'space-between'}}>
          <span style={{fontSize:14,fontWeight:700,color:'var(--t-text1)',fontFamily:mono}}>Keyboard Shortcuts</span>
          <button onClick={onClose} style={{background:'none',border:'none',cursor:'pointer',color:'var(--t-text3)',fontSize:18,lineHeight:1}}>x</button>
        </div>

        {/* Shortcut grid */}
        <div style={{padding:20,display:'flex',flexDirection:'column',gap:20,maxHeight:'70vh',overflowY:'auto'}}>
          {categories.map(cat => (
            <div key={cat}>
              <div style={{fontSize:9,color:'var(--t-text3)',fontFamily:mono,letterSpacing:'0.1em',textTransform:'uppercase',marginBottom:8}}>{cat}</div>
              <div style={{display:'flex',flexDirection:'column',gap:2}}>
                {SHORTCUTS.filter(s=>s.category===cat).map((s,i)=>(
                  <div key={i} style={{display:'flex',alignItems:'center',justifyContent:'space-between',padding:'5px 8px',borderRadius:6,background:'var(--t-surface)'}}>
                    <span style={{fontSize:12,color:'var(--t-text2)',fontFamily:mono}}>{s.description}</span>
                    <Kbd keys={fmtKey(s)}/>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div style={{padding:'10px 20px',borderTop:'1px solid var(--t-border)',display:'flex',justifyContent:'space-between',alignItems:'center'}}>
          <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>Press ? or Esc to close</span>
          <span style={{fontSize:10,color:'var(--t-text3)',fontFamily:mono}}>BdStockOMS v1.0</span>
        </div>
      </div>
    </div>
  )
}
