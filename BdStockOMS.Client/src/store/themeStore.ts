import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type ThemeId   = 'obsidian' | 'midnight' | 'slate' | 'aurora' | 'arctic'
export type AccentId  = 'azure' | 'emerald' | 'amber' | 'rose' | 'violet' | 'cyan'
export type DensityId = 'compact' | 'comfortable' | 'spacious'

export interface ThemeOption   { id: ThemeId;   label: string; emoji: string; dark: boolean }
export interface AccentOption  { id: AccentId;  label: string; color: string }
export interface DensityOption { id: DensityId; label: string; desc: string  }

export const THEMES: ThemeOption[] = [
  { id: 'obsidian', label: 'Obsidian', emoji: '⬛', dark: true  },
  { id: 'midnight', label: 'Midnight', emoji: '🌑', dark: true  },
  { id: 'slate',    label: 'Slate',    emoji: '🌫',  dark: true  },
  { id: 'aurora',   label: 'Aurora',   emoji: '🌊', dark: true  },
  { id: 'arctic',   label: 'Arctic',   emoji: '☀️', dark: false },
]

export const ACCENTS: AccentOption[] = [
  { id: 'azure',   label: 'Azure',   color: '#3B82F6' },
  { id: 'cyan',    label: 'Cyan',    color: '#06B6D4' },
  { id: 'emerald', label: 'Emerald', color: '#10B981' },
  { id: 'violet',  label: 'Violet',  color: '#8B5CF6' },
  { id: 'rose',    label: 'Rose',    color: '#F43F5E' },
  { id: 'amber',   label: 'Amber',   color: '#F59E0B' },
]

export const DENSITIES: DensityOption[] = [
  { id: 'compact',     label: 'Compact',     desc: 'Max data density'  },
  { id: 'comfortable', label: 'Comfortable', desc: 'Balanced default'   },
  { id: 'spacious',    label: 'Spacious',    desc: 'Relaxed reading'    },
]

interface ThemeState {
  theme:   ThemeId
  accent:  AccentId
  density: DensityId
  sidebarCollapsed: boolean
  tickerEnabled:    boolean
  setTheme:   (t: ThemeId)   => void
  setAccent:  (a: AccentId)  => void
  setDensity: (d: DensityId) => void
  toggleSidebar: () => void
  setSidebarCollapsed: (v: boolean) => void
  toggleTicker: () => void
}

function applyTheme(theme: ThemeId, accent: AccentId, density: DensityId) {
  const root = document.documentElement
  root.setAttribute('data-theme',   theme)
  root.setAttribute('data-accent',  accent)
  root.setAttribute('data-density', density)
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme:   'obsidian',
      accent:  'azure',
      density: 'comfortable',
      sidebarCollapsed: false,
      tickerEnabled: true,

      setTheme:   (theme)   => { set({ theme });   applyTheme(theme,        get().accent, get().density) },
      setAccent:  (accent)  => { set({ accent });  applyTheme(get().theme,  accent,       get().density) },
      setDensity: (density) => { set({ density }); applyTheme(get().theme,  get().accent, density) },
      toggleSidebar:   () => set(s => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setSidebarCollapsed: (v) => set({ sidebarCollapsed: v }),
      toggleTicker:    () => set(s => ({ tickerEnabled: !s.tickerEnabled })),
    }),
    {
      name: 'bd_oms_theme_v2',
      onRehydrateStorage: () => (state) => {
        if (state) applyTheme(state.theme, state.accent, state.density)
      },
    }
  )
)

// Apply immediately before React mounts (prevents flash)
applyTheme('obsidian', 'azure', 'comfortable')
