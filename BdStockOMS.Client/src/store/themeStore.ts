import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type ThemeId =
  | 'obsidian' | 'midnight' | 'slate' | 'aurora' | 'arctic'
  | 'dhaka'    | 'crimson'  | 'forest' | 'carbon' | 'navy'
  | 'sand'     | 'eclipse'

export type AccentId  = 'teal' | 'azure' | 'emerald' | 'amber' | 'rose' | 'violet' | 'cyan' | 'gold'
export type DensityId = 'compact' | 'comfortable' | 'spacious'

export interface ThemeOption {
  id:      ThemeId
  label:   string
  emoji:   string
  dark:    boolean
  bg:      string   // preview swatch
  surface: string
  text:    string
  desc:    string
  category: 'Dark' | 'Light' | 'Special'
}

export interface AccentOption {
  id:    AccentId
  label: string
  color: string
  glow:  string
}

export interface DensityOption {
  id:    DensityId
  label: string
  desc:  string
}

export const THEMES: ThemeOption[] = [
  // ── Dark ──
  { id: 'obsidian', label: 'Obsidian',   emoji: '⬛', dark: true,  category: 'Dark',
    bg: '#080C14', surface: '#0D1320', text: '#E2E8F0',
    desc: 'Deep black — maximum contrast' },
  { id: 'midnight', label: 'Midnight',   emoji: '🌑', dark: true,  category: 'Dark',
    bg: '#0A0F1E', surface: '#111827', text: '#CBD5E1',
    desc: 'Classic dark blue-black' },
  { id: 'carbon',   label: 'Carbon',     emoji: '🖤', dark: true,  category: 'Dark',
    bg: '#111111', surface: '#1A1A1A', text: '#D4D4D4',
    desc: 'Pure neutral dark' },
  { id: 'eclipse',  label: 'Eclipse',    emoji: '🌘', dark: true,  category: 'Dark',
    bg: '#0E0A1A', surface: '#150F2A', text: '#DDD6FE',
    desc: 'Deep purple-black' },
  { id: 'slate',    label: 'Slate',      emoji: '🌫',  dark: true,  category: 'Dark',
    bg: '#0F172A', surface: '#1E293B', text: '#CBD5E1',
    desc: 'Blue-grey tones' },
  { id: 'navy',     label: 'Navy',       emoji: '🌊', dark: true,  category: 'Dark',
    bg: '#030D1C', surface: '#071428', text: '#BAE6FD',
    desc: 'Deep ocean blue' },
  // ── Special ──
  { id: 'dhaka',    label: 'Dhaka Night',emoji: '🏙', dark: true,  category: 'Special',
    bg: '#0A1A0A', surface: '#0D200D', text: '#BBF7D0',
    desc: 'Green-tinted — DSE inspired' },
  { id: 'crimson',  label: 'Crimson',    emoji: '🔴', dark: true,  category: 'Special',
    bg: '#1A0505', surface: '#200A0A', text: '#FCA5A5',
    desc: 'Bold red-dark theme' },
  { id: 'forest',   label: 'Forest',     emoji: '🌲', dark: true,  category: 'Special',
    bg: '#0A1208', surface: '#0F1A0C', text: '#A7F3D0',
    desc: 'Deep forest green' },
  { id: 'aurora',   label: 'Aurora',     emoji: '🌌', dark: true,  category: 'Special',
    bg: '#070D1A', surface: '#0C1428', text: '#A5F3FC',
    desc: 'Northern lights gradient' },
  // ── Light ──
  { id: 'arctic',   label: 'Arctic',     emoji: '☀️', dark: false, category: 'Light',
    bg: '#F0F4F8', surface: '#FFFFFF', text: '#1E293B',
    desc: 'Clean professional light' },
  { id: 'sand',     label: 'Sand',       emoji: '🏖', dark: false, category: 'Light',
    bg: '#FAF7F0', surface: '#FFFFFF', text: '#292524',
    desc: 'Warm off-white' },
]

export const ACCENTS: AccentOption[] = [
  { id: 'teal',    label: 'Teal',    color: '#00D4AA', glow: 'rgba(0,212,170,0.3)'   },
  { id: 'azure',   label: 'Azure',   color: '#3B82F6', glow: 'rgba(59,130,246,0.3)'  },
  { id: 'cyan',    label: 'Cyan',    color: '#06B6D4', glow: 'rgba(6,182,212,0.3)'   },
  { id: 'emerald', label: 'Emerald', color: '#10B981', glow: 'rgba(16,185,129,0.3)'  },
  { id: 'violet',  label: 'Violet',  color: '#8B5CF6', glow: 'rgba(139,92,246,0.3)'  },
  { id: 'rose',    label: 'Rose',    color: '#F43F5E', glow: 'rgba(244,63,94,0.3)'   },
  { id: 'amber',   label: 'Amber',   color: '#F59E0B', glow: 'rgba(245,158,11,0.3)'  },
  { id: 'gold',    label: 'Gold',    color: '#EAB308', glow: 'rgba(234,179,8,0.3)'   },
]

export const DENSITIES: DensityOption[] = [
  { id: 'compact',     label: 'Compact',     desc: 'Max data density'  },
  { id: 'comfortable', label: 'Comfortable', desc: 'Balanced default'  },
  { id: 'spacious',    label: 'Spacious',    desc: 'Relaxed reading'   },
]

interface ThemeState {
  theme:            ThemeId
  accent:           AccentId
  density:          DensityId
  sidebarCollapsed: boolean
  tickerEnabled:    boolean
  pendingTheme:     ThemeId   | null
  pendingAccent:    AccentId  | null
  setTheme:         (t: ThemeId)   => void
  setAccent:        (a: AccentId)  => void
  setDensity:       (d: DensityId) => void
  previewTheme:     (t: ThemeId)   => void
  previewAccent:    (a: AccentId)  => void
  confirmTheme:     () => void
  cancelPreview:    () => void
  toggleSidebar:    () => void
  setSidebarCollapsed: (v: boolean) => void
  toggleTicker:     () => void
}

export function applyTheme(theme: ThemeId, accent: AccentId, density: DensityId) {
  const t = THEMES.find(x => x.id === theme)!
  const a = ACCENTS.find(x => x.id === accent)!
  const root = document.documentElement
  root.setAttribute('data-theme',   theme)
  root.setAttribute('data-accent',  accent)
  root.setAttribute('data-density', density)
  root.style.setProperty('--bg',       t.bg)
  root.style.setProperty('--surface',  t.surface)
  root.style.setProperty('--text',     t.text)
  root.style.setProperty('--accent',   a.color)
  root.style.setProperty('--accent-glow', a.glow)
  root.style.setProperty('--color-scheme', t.dark ? 'dark' : 'light')
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme:            'obsidian',
      accent:           'teal',
      density:          'comfortable',
      sidebarCollapsed: false,
      tickerEnabled:    true,
      pendingTheme:     null,
      pendingAccent:    null,

      setTheme:  (theme)   => { set({ theme,  pendingTheme: null  }); applyTheme(theme,        get().accent,  get().density) },
      setAccent: (accent)  => { set({ accent, pendingAccent: null }); applyTheme(get().theme,  accent,        get().density) },
      setDensity:(density) => { set({ density }); applyTheme(get().theme, get().accent, density) },

      previewTheme:  (t) => { set({ pendingTheme: t  }); applyTheme(t,            get().accent,        get().density) },
      previewAccent: (a) => { set({ pendingAccent: a }); applyTheme(get().theme,  a,                   get().density) },

      confirmTheme: () => {
        const { pendingTheme, pendingAccent, theme, accent, density } = get()
        const newTheme  = pendingTheme  ?? theme
        const newAccent = pendingAccent ?? accent
        set({ theme: newTheme, accent: newAccent, pendingTheme: null, pendingAccent: null })
        applyTheme(newTheme, newAccent, density)
      },
      cancelPreview: () => {
        const { theme, accent, density } = get()
        set({ pendingTheme: null, pendingAccent: null })
        applyTheme(theme, accent, density)
      },

      toggleSidebar:       () => set(s => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setSidebarCollapsed: (v) => set({ sidebarCollapsed: v }),
      toggleTicker:        () => set(s => ({ tickerEnabled: !s.tickerEnabled })),
    }),
    {
      name: 'bd_oms_theme_v2',
      onRehydrateStorage: () => (state) => {
        if (state) applyTheme(state.theme, state.accent, state.density)
      },
    }
  )
)

applyTheme('obsidian', 'teal', 'comfortable')
