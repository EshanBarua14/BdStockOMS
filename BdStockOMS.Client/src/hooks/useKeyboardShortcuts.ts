// useKeyboardShortcuts.ts - Day 78
// Central keyboard shortcut registry for BdStockOMS
// Usage: useKeyboardShortcuts() in DashboardLayout — registers all global shortcuts

import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'

export interface Shortcut {
  key: string
  ctrl?: boolean
  shift?: boolean
  alt?: boolean
  description: string
  category: string
  action?: () => void
}

// All shortcuts exported so KeyboardHelp can display them
export const SHORTCUTS: Omit<Shortcut, 'action'>[] = [
  // Navigation
  { key: 'k',      ctrl: true,  category: 'Global',     description: 'Open command palette' },
  { key: '?',                   category: 'Global',     description: 'Show keyboard shortcuts' },
  { key: 'Escape',              category: 'Global',     description: 'Close modal / palette' },
  // Page navigation
  { key: 'g d',                 category: 'Navigation', description: 'Go to Dashboard' },
  { key: 'g o',                 category: 'Navigation', description: 'Go to Orders' },
  { key: 'g p',                 category: 'Navigation', description: 'Go to Portfolio' },
  { key: 'g m',                 category: 'Navigation', description: 'Go to Market' },
  { key: 'g r',                 category: 'Navigation', description: 'Go to Reports' },
  { key: 'g a',                 category: 'Navigation', description: 'Go to Accounts' },
  { key: 'g s',                 category: 'Navigation', description: 'Go to Settings' },
  // Trading
  { key: 'F1',                  category: 'Trading',    description: 'Open Buy console' },
  { key: 'F2',                  category: 'Trading',    description: 'Open Sell console' },
  // UI
  { key: 'ArrowLeft', alt: true, category: 'UI',        description: 'Go back' },
  { key: 'ArrowRight',alt: true, category: 'UI',        description: 'Go forward' },
]

interface Options {
  onOpenPalette: () => void
  onShowHelp: () => void
}

export function useKeyboardShortcuts({ onOpenPalette, onShowHelp }: Options) {
  const navigate = useNavigate()
  let gBuffer = ''
  let gTimer: ReturnType<typeof setTimeout> | null = null

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement)?.tagName?.toLowerCase()
      const isInput = tag === 'input' || tag === 'textarea' || tag === 'select' || (e.target as HTMLElement)?.isContentEditable

      // Ctrl+K — command palette (works even in inputs)
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault()
        onOpenPalette()
        return
      }

      // Alt+Left/Right — browser history nav
      if (e.altKey && e.key === 'ArrowLeft')  { e.preventDefault(); window.history.back();    return }
      if (e.altKey && e.key === 'ArrowRight') { e.preventDefault(); window.history.forward(); return }

      // Skip remaining shortcuts when typing in inputs
      if (isInput) return

      // ? — show help
      if (e.key === '?' && !e.ctrlKey && !e.altKey) {
        e.preventDefault()
        onShowHelp()
        return
      }

      // g-prefix navigation (vim style)
      if (e.key === 'g' && !e.ctrlKey && !e.altKey && !e.shiftKey) {
        gBuffer = 'g'
        if (gTimer) clearTimeout(gTimer)
        gTimer = setTimeout(() => { gBuffer = '' }, 1000)
        return
      }

      if (gBuffer === 'g') {
        gBuffer = ''
        if (gTimer) clearTimeout(gTimer)
        const routes: Record<string, string> = {
          d: '/dashboard',
          o: '/orders',
          p: '/portfolio',
          m: '/market',
          r: '/reports',
          a: '/accounts',
          s: '/settings/general',
          t: '/trade-monitor',
          i: '/ipo',
          b: '/tbond',
        }
        const route = routes[e.key.toLowerCase()]
        if (route) { e.preventDefault(); navigate(route) }
        return
      }
    }

    window.addEventListener('keydown', handler)
    return () => {
      window.removeEventListener('keydown', handler)
      if (gTimer) clearTimeout(gTimer)
    }
  }, [navigate, onOpenPalette, onShowHelp])
}
