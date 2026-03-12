import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type ThemeId =
  | 'obsidian' | 'midnight' | 'slate' | 'aurora' | 'arctic'
  | 'dhaka'    | 'crimson'  | 'forest' | 'carbon' | 'navy'
  | 'sand'     | 'eclipse'  | 'plasma' | 'glacier'

export type AccentId  = 'teal' | 'azure' | 'emerald' | 'amber' | 'rose' | 'violet' | 'cyan' | 'gold'
export type DensityId = 'compact' | 'comfortable' | 'spacious'

export interface ThemeOption {
  id: ThemeId; label: string; emoji: string; dark: boolean
  category: 'Dark' | 'Light' | 'Special'; desc: string
  bg: string; surface: string; panel: string; elevated: string
  text1: string; text2: string; text3: string
  border: string; hoverBg: string
}

export interface AccentOption { id: AccentId; label: string; color: string; glow: string }
export interface DensityOption { id: DensityId; label: string; desc: string }

export const THEMES: ThemeOption[] = [
  // ── DARK (6) ──
  { id: 'obsidian', label: 'Obsidian', emoji: '⬛', dark: true, category: 'Dark',
    desc: 'Deep black — max contrast, pro default',
    bg: '#080C14', surface: '#0D1320', panel: '#111827', elevated: '#1a2332',
    text1: '#E2E8F0', text2: '#8b99b0', text3: '#4a5568',
    border: 'rgba(255,255,255,0.06)', hoverBg: 'rgba(255,255,255,0.04)' },
  { id: 'midnight', label: 'Midnight', emoji: '🌑', dark: true, category: 'Dark',
    desc: 'Classic dark blue-black',
    bg: '#0A0F1E', surface: '#111827', panel: '#1E293B', elevated: '#273549',
    text1: '#CBD5E1', text2: '#7d8da0', text3: '#475569',
    border: 'rgba(255,255,255,0.07)', hoverBg: 'rgba(255,255,255,0.05)' },
  { id: 'carbon', label: 'Carbon', emoji: '🖤', dark: true, category: 'Dark',
    desc: 'Pure neutral — zero color bias',
    bg: '#111111', surface: '#1A1A1A', panel: '#222222', elevated: '#2c2c2c',
    text1: '#D4D4D4', text2: '#888888', text3: '#555555',
    border: 'rgba(255,255,255,0.08)', hoverBg: 'rgba(255,255,255,0.05)' },
  { id: 'slate', label: 'Slate', emoji: '🌫', dark: true, category: 'Dark',
    desc: 'Cool blue-grey professional',
    bg: '#0F172A', surface: '#1E293B', panel: '#273549', elevated: '#334155',
    text1: '#CBD5E1', text2: '#7d8da0', text3: '#475569',
    border: 'rgba(255,255,255,0.07)', hoverBg: 'rgba(255,255,255,0.05)' },
  { id: 'navy', label: 'Navy', emoji: '🌊', dark: true, category: 'Dark',
    desc: 'Deep ocean — Bloomberg inspired',
    bg: '#030D1C', surface: '#071428', panel: '#0c1e38', elevated: '#132d4f',
    text1: '#BAE6FD', text2: '#6b9cc3', text3: '#3a6b96',
    border: 'rgba(100,180,255,0.08)', hoverBg: 'rgba(100,180,255,0.05)' },
  { id: 'eclipse', label: 'Eclipse', emoji: '🌘', dark: true, category: 'Dark',
    desc: 'Deep purple-black, violet tones',
    bg: '#0E0A1A', surface: '#150F2A', panel: '#1e1638', elevated: '#271f48',
    text1: '#DDD6FE', text2: '#9985c4', text3: '#5e4d8a',
    border: 'rgba(160,120,255,0.08)', hoverBg: 'rgba(160,120,255,0.05)' },

  // ── SPECIAL (6) ──
  { id: 'dhaka', label: 'Dhaka Night', emoji: '🏙', dark: true, category: 'Special',
    desc: 'Green-tinted — DSE inspired',
    bg: '#0A1A0A', surface: '#0D200D', panel: '#132813', elevated: '#1a341a',
    text1: '#BBF7D0', text2: '#6bbc82', text3: '#3a7a4e',
    border: 'rgba(0,200,100,0.08)', hoverBg: 'rgba(0,200,100,0.05)' },
  { id: 'crimson', label: 'Crimson', emoji: '🔴', dark: true, category: 'Special',
    desc: 'Bold red-dark — aggressive mode',
    bg: '#1A0505', surface: '#200A0A', panel: '#2c1010', elevated: '#381818',
    text1: '#FCA5A5', text2: '#b86060', text3: '#7a3535',
    border: 'rgba(255,80,80,0.08)', hoverBg: 'rgba(255,80,80,0.05)' },
  { id: 'forest', label: 'Forest', emoji: '🌲', dark: true, category: 'Special',
    desc: 'Deep green — calming natural',
    bg: '#0A1208', surface: '#0F1A0C', panel: '#162412', elevated: '#1e3018',
    text1: '#A7F3D0', text2: '#5eaa80', text3: '#357050',
    border: 'rgba(0,180,100,0.08)', hoverBg: 'rgba(0,180,100,0.05)' },
  { id: 'aurora', label: 'Aurora', emoji: '🌌', dark: true, category: 'Special',
    desc: 'Northern lights — cyan & purple',
    bg: '#070D1A', surface: '#0C1428', panel: '#121e38', elevated: '#182a4a',
    text1: '#A5F3FC', text2: '#5eb0c0', text3: '#357080',
    border: 'rgba(0,200,255,0.08)', hoverBg: 'rgba(0,200,255,0.05)' },
  { id: 'plasma', label: 'Plasma', emoji: '⚡', dark: true, category: 'Special',
    desc: 'Electric violet — high energy',
    bg: '#0D0815', surface: '#150E22', panel: '#1e1530', elevated: '#281e40',
    text1: '#E9D5FF', text2: '#9a7acc', text3: '#6a4a9a',
    border: 'rgba(180,100,255,0.08)', hoverBg: 'rgba(180,100,255,0.05)' },
  { id: 'glacier', label: 'Glacier', emoji: '🧊', dark: true, category: 'Special',
    desc: 'Icy blue-teal — cold precision',
    bg: '#071318', surface: '#0C1C24', panel: '#122830', elevated: '#18343e',
    text1: '#B8E8F5', text2: '#6aaabe', text3: '#3a7088',
    border: 'rgba(0,180,220,0.08)', hoverBg: 'rgba(0,180,220,0.05)' },

  // ── LIGHT (2) ──
  { id: 'arctic', label: 'Arctic', emoji: '☀️', dark: false, category: 'Light',
    desc: 'Clean professional light',
    bg: '#F0F4F8', surface: '#FFFFFF', panel: '#E8EDF2', elevated: '#dde4ec',
    text1: '#1E293B', text2: '#64748B', text3: '#94A3B8',
    border: 'rgba(0,0,0,0.08)', hoverBg: 'rgba(0,0,0,0.04)' },
  { id: 'sand', label: 'Sand', emoji: '🏖', dark: false, category: 'Light',
    desc: 'Warm off-white — gentle',
    bg: '#FAF7F0', surface: '#FFFFFF', panel: '#F0EBE0', elevated: '#e6dfd2',
    text1: '#292524', text2: '#78716C', text3: '#A8A29E',
    border: 'rgba(0,0,0,0.07)', hoverBg: 'rgba(0,0,0,0.03)' },
]

export const ACCENTS: AccentOption[] = [
  { id: 'teal',    label: 'Teal',    color: '#00D4AA', glow: 'rgba(0,212,170,0.3)'  },
  { id: 'azure',   label: 'Azure',   color: '#3B82F6', glow: 'rgba(59,130,246,0.3)' },
  { id: 'cyan',    label: 'Cyan',    color: '#06B6D4', glow: 'rgba(6,182,212,0.3)'  },
  { id: 'emerald', label: 'Emerald', color: '#10B981', glow: 'rgba(16,185,129,0.3)' },
  { id: 'violet',  label: 'Violet',  color: '#8B5CF6', glow: 'rgba(139,92,246,0.3)' },
  { id: 'rose',    label: 'Rose',    color: '#F43F5E', glow: 'rgba(244,63,94,0.3)'  },
  { id: 'amber',   label: 'Amber',   color: '#F59E0B', glow: 'rgba(245,158,11,0.3)' },
  { id: 'gold',    label: 'Gold',    color: '#EAB308', glow: 'rgba(234,179,8,0.3)'  },
]

export const DENSITIES: DensityOption[] = [
  { id: 'compact',     label: 'Compact',     desc: 'Max data density' },
  { id: 'comfortable', label: 'Comfortable', desc: 'Balanced default' },
  { id: 'spacious',    label: 'Spacious',    desc: 'Relaxed reading'  },
]

// ═══════════════════════════════════════════════════════════
// applyTheme — sets 20+ CSS vars on :root
// ═══════════════════════════════════════════════════════════
export function applyTheme(
  themeId: ThemeId, accentId: AccentId, densityId: DensityId,
  buyColor = '#00e676', sellColor = '#ff1744'
) {
  const t = THEMES.find(x => x.id === themeId)
  const a = ACCENTS.find(x => x.id === accentId)
  if (!t || !a) return
  const r = document.documentElement

  r.setAttribute('data-theme', themeId)
  r.setAttribute('data-accent', accentId)
  r.setAttribute('data-density', densityId)

  r.style.setProperty('--t-bg',       t.bg)
  r.style.setProperty('--t-surface',  t.surface)
  r.style.setProperty('--t-panel',    t.panel)
  r.style.setProperty('--t-elevated', t.elevated)
  r.style.setProperty('--t-text1',    t.text1)
  r.style.setProperty('--t-text2',    t.text2)
  r.style.setProperty('--t-text3',    t.text3)
  r.style.setProperty('--t-border',   t.border)
  r.style.setProperty('--t-hover',    t.hoverBg)
  r.style.setProperty('--t-accent',   a.color)
  r.style.setProperty('--t-accent-glow', a.glow)
  r.style.setProperty('--t-buy',      buyColor)
  r.style.setProperty('--t-sell',     sellColor)
  r.style.setProperty('--color-scheme', t.dark ? 'dark' : 'light')
  r.style.colorScheme = t.dark ? 'dark' : 'light'

  // Legacy compat
  r.style.setProperty('--bg', t.bg)
  r.style.setProperty('--surface', t.surface)
  r.style.setProperty('--text', t.text1)
  r.style.setProperty('--accent', a.color)
  r.style.setProperty('--accent-glow', a.glow)
}

// ═══════════════════════════════════════════════════════════
// Zustand Store
// ═══════════════════════════════════════════════════════════
interface ThemeState {
  theme: ThemeId; accent: AccentId; density: DensityId
  sidebarCollapsed: boolean; tickerEnabled: boolean
  pendingTheme: ThemeId | null; pendingAccent: AccentId | null
  buyColor: string; sellColor: string
  setTheme: (t: ThemeId) => void; setAccent: (a: AccentId) => void
  setDensity: (d: DensityId) => void
  previewTheme: (t: ThemeId) => void; previewAccent: (a: AccentId) => void
  confirmTheme: () => void; cancelPreview: () => void
  toggleSidebar: () => void; setSidebarCollapsed: (v: boolean) => void
  toggleTicker: () => void
  setBuyColor: (c: string) => void; setSellColor: (c: string) => void
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'obsidian', accent: 'teal', density: 'comfortable',
      sidebarCollapsed: false, tickerEnabled: true,
      pendingTheme: null, pendingAccent: null,
      buyColor: '#00e676', sellColor: '#ff1744',

      // Instant apply (for density, buy/sell)
      setTheme:  (theme)  => { set({ theme, pendingTheme: null }); applyTheme(theme, get().accent, get().density, get().buyColor, get().sellColor) },
      setAccent: (accent) => { set({ accent, pendingAccent: null }); applyTheme(get().theme, accent, get().density, get().buyColor, get().sellColor) },
      setDensity:(d) => { set({ density: d }); applyTheme(get().theme, get().accent, d, get().buyColor, get().sellColor) },

      // Preview (CSS changes but store doesn't save yet)
      previewTheme:  (t) => { set({ pendingTheme: t });  applyTheme(t, get().pendingAccent ?? get().accent, get().density, get().buyColor, get().sellColor) },
      previewAccent: (a) => { set({ pendingAccent: a }); applyTheme(get().pendingTheme ?? get().theme, a, get().density, get().buyColor, get().sellColor) },

      // Confirm = save pending → actual (persists to localStorage)
      confirmTheme: () => {
        const s = get()
        const nt = s.pendingTheme ?? s.theme
        const na = s.pendingAccent ?? s.accent
        set({ theme: nt, accent: na, pendingTheme: null, pendingAccent: null })
        applyTheme(nt, na, s.density, s.buyColor, s.sellColor)
      },

      // Cancel = revert CSS to saved theme
      cancelPreview: () => {
        const s = get()
        set({ pendingTheme: null, pendingAccent: null })
        applyTheme(s.theme, s.accent, s.density, s.buyColor, s.sellColor)
      },

      toggleSidebar: () => set(s => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setSidebarCollapsed: (v) => set({ sidebarCollapsed: v }),
      toggleTicker: () => set(s => ({ tickerEnabled: !s.tickerEnabled })),
      setBuyColor:  (c) => { set({ buyColor: c });  applyTheme(get().theme, get().accent, get().density, c, get().sellColor) },
      setSellColor: (c) => { set({ sellColor: c }); applyTheme(get().theme, get().accent, get().density, get().buyColor, c) },
    }),
    {
      name: 'bd_oms_theme_v5',
      onRehydrateStorage: () => (state) => {
        // This runs AFTER localStorage is loaded — applies the saved theme
        if (state) {
          applyTheme(state.theme, state.accent, state.density, state.buyColor, state.sellColor)
        }
      },
    }
  )
)

// NOTE: No applyTheme() call here! The onRehydrateStorage callback handles it.
// Calling it here would override the saved theme on every page load.
