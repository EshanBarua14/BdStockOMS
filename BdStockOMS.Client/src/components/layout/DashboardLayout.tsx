// @ts-nocheck
// DashboardLayout.tsx - Day 78 — central keyboard shortcut wiring
import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Topbar }  from './Topbar'
import { CommandPalette } from '@/components/ui/CommandPalette'
import { KeyboardHelp }   from '@/components/ui/KeyboardHelp'
import { useKeyboardShortcuts } from '@/hooks/useKeyboardShortcuts'

function Inner({ paletteOpen, setPaletteOpen, helpOpen, setHelpOpen }) {
  useKeyboardShortcuts({
    onOpenPalette: () => setPaletteOpen(o => !o),
    onShowHelp:    () => setHelpOpen(o => !o),
  })
  return null
}

export function DashboardLayout() {
  const [paletteOpen, setPaletteOpen] = useState(false)
  const [helpOpen,    setHelpOpen]    = useState(false)

  return (
    <div style={{
      display: 'flex', minHeight: '100vh',
      background: 'var(--t-bg)',
      fontFamily: "'Outfit', sans-serif",
      position: 'relative', overflow: 'hidden',
    }}>
      <Sidebar />
      <div style={{
        flex: 1, display: 'flex', flexDirection: 'column',
        minWidth: 0, overflow: 'hidden', position: 'relative', zIndex: 1,
      }}>
        <Topbar onOpenPalette={() => setPaletteOpen(true)} />
        <main style={{ flex: 1, overflow: 'auto', background: 'var(--t-bg)' }}>
          <Outlet />
        </main>
      </div>
      <Inner
        paletteOpen={paletteOpen} setPaletteOpen={setPaletteOpen}
        helpOpen={helpOpen}       setHelpOpen={setHelpOpen}
      />
      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
      <KeyboardHelp   open={helpOpen}    onClose={() => setHelpOpen(false)} />
    </div>
  )
}
