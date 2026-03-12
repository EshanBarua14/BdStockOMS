// src/store/useTemplateStore.ts
// Day 52 — Zustand persist store for dashboard templates
// Supports: save/load/delete/rename multiple templates, export/import as JSON

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// ─── Types ────────────────────────────────────────────────────
export interface LayoutItem {
  i: string; x: number; y: number; w: number; h: number
  [key: string]: any
}

export interface WidgetState {
  id: string
  visible: boolean
  colorGroup: string | null
}

export interface DashboardPage {
  id: string
  name: string
  icon: string
  layout: LayoutItem[]
  widgets: WidgetState[]
}

export interface DashboardTemplate {
  id: string
  name: string
  description: string
  pages: DashboardPage[]
  activePageId: string
  createdAt: string
  updatedAt: string
}

export interface ExportedTemplate {
  _format: 'bd_oms_template_v1'
  _exportedAt: string
  template: DashboardTemplate
}

// ─── Helpers ──────────────────────────────────────────────────
const uid = () => `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
const now = () => new Date().toISOString()

// Default widget IDs (matches existing registry)
const ALL_WIDGET_IDS = [
  'ticker','index','movers','watchlist','chart','orderbook',
  'order','portfolio','executions','heatmap','depth','pressure',
  'notif','news','ai','rms'
]

const defaultWidgets = (): WidgetState[] =>
  ALL_WIDGET_IDS.map(id => ({ id, visible: true, colorGroup: null }))

// Default preset layouts (matching existing useDashboardPersistence)
const PRESET_LAYOUTS: Record<string, LayoutItem[]> = {
  Trading: [
    { i:'ticker',x:0,y:0,w:48,h:4 },{ i:'index',x:0,y:4,w:10,h:14 },
    { i:'chart',x:10,y:4,w:24,h:30 },{ i:'orderbook',x:34,y:4,w:14,h:20 },
    { i:'order',x:0,y:18,w:10,h:20 },{ i:'executions',x:34,y:24,w:14,h:14 },
    { i:'rms',x:0,y:38,w:24,h:12 },{ i:'notif',x:24,y:38,w:24,h:12 },
  ],
  Research: [
    { i:'ticker',x:0,y:0,w:48,h:4 },{ i:'movers',x:0,y:4,w:14,h:22 },
    { i:'chart',x:14,y:4,w:20,h:30 },{ i:'heatmap',x:34,y:4,w:14,h:20 },
    { i:'news',x:0,y:26,w:14,h:20 },{ i:'ai',x:14,y:34,w:20,h:16 },
    { i:'watchlist',x:34,y:24,w:14,h:22 },
  ],
  Portfolio: [
    { i:'ticker',x:0,y:0,w:48,h:4 },{ i:'portfolio',x:0,y:4,w:24,h:24 },
    { i:'executions',x:24,y:4,w:24,h:24 },{ i:'rms',x:0,y:28,w:24,h:14 },
    { i:'order',x:24,y:28,w:24,h:14 },
  ],
}

function createDefaultPage(name = 'Main', presetKey = 'Trading'): DashboardPage {
  const layout = PRESET_LAYOUTS[presetKey] ?? PRESET_LAYOUTS.Trading
  return {
    id: uid(),
    name,
    icon: '📊',
    layout,
    widgets: defaultWidgets().map(w => ({
      ...w,
      visible: layout.some(l => l.i === w.id),
    })),
  }
}

function createDefaultTemplate(): DashboardTemplate {
  const mainPage = createDefaultPage('Trading', 'Trading')
  return {
    id: uid(),
    name: 'Default Workspace',
    description: 'Auto-created default template',
    pages: [mainPage],
    activePageId: mainPage.id,
    createdAt: now(),
    updatedAt: now(),
  }
}

// ─── Store ────────────────────────────────────────────────────
interface TemplateStore {
  // State
  templates: DashboardTemplate[]
  activeTemplateId: string | null

  // Template CRUD
  createTemplate: (name: string, description?: string) => string
  deleteTemplate: (id: string) => void
  renameTemplate: (id: string, name: string) => void
  setActiveTemplate: (id: string) => void
  duplicateTemplate: (id: string) => string

  // Page CRUD (within active template)
  addPage: (name?: string, preset?: string) => string
  deletePage: (pageId: string) => void
  renamePage: (pageId: string, name: string) => void
  setPageIcon: (pageId: string, icon: string) => void
  setActivePage: (pageId: string) => void
  reorderPages: (pageIds: string[]) => void

  // Layout operations (on active page)
  updateLayout: (layout: LayoutItem[]) => void
  setWidgetVisible: (widgetId: string, visible: boolean) => void
  setWidgetColor: (widgetId: string, color: string | null) => void
  applyPreset: (presetKey: string) => void

  // Export / Import
  exportTemplate: (id: string) => ExportedTemplate | null
  exportAllTemplates: () => ExportedTemplate[]
  importTemplate: (data: ExportedTemplate) => string | null

  // Computed helpers
  getActiveTemplate: () => DashboardTemplate | null
  getActivePage: () => DashboardPage | null
  getVisibleLayout: () => LayoutItem[]
}

export const useTemplateStore = create<TemplateStore>()(
  persist(
    (set, get) => {
      // Internal helper: update active template
      const updateActive = (fn: (t: DashboardTemplate) => DashboardTemplate) => {
        set(s => {
          const idx = s.templates.findIndex(t => t.id === s.activeTemplateId)
          if (idx < 0) return s
          const updated = [...s.templates]
          updated[idx] = { ...fn(updated[idx]), updatedAt: now() }
          return { templates: updated }
        })
      }

      // Internal helper: update active page within active template
      const updateActivePage = (fn: (p: DashboardPage) => DashboardPage) => {
        updateActive(t => {
          const pages = t.pages.map(p => p.id === t.activePageId ? fn(p) : p)
          return { ...t, pages }
        })
      }

      return {
        templates: [createDefaultTemplate()],
        activeTemplateId: null, // will be set on init

        // ── Template CRUD ──
        createTemplate: (name, description = '') => {
          const t = createDefaultTemplate()
          t.name = name
          t.description = description
          set(s => ({ templates: [...s.templates, t], activeTemplateId: t.id }))
          return t.id
        },

        deleteTemplate: (id) => {
          set(s => {
            const filtered = s.templates.filter(t => t.id !== id)
            if (filtered.length === 0) {
              const def = createDefaultTemplate()
              return { templates: [def], activeTemplateId: def.id }
            }
            const activeId = s.activeTemplateId === id ? filtered[0].id : s.activeTemplateId
            return { templates: filtered, activeTemplateId: activeId }
          })
        },

        renameTemplate: (id, name) => {
          set(s => ({
            templates: s.templates.map(t => t.id === id ? { ...t, name, updatedAt: now() } : t)
          }))
        },

        setActiveTemplate: (id) => set({ activeTemplateId: id }),

        duplicateTemplate: (id) => {
          const s = get()
          const src = s.templates.find(t => t.id === id)
          if (!src) return ''
          const dup: DashboardTemplate = {
            ...JSON.parse(JSON.stringify(src)),
            id: uid(),
            name: `${src.name} (Copy)`,
            createdAt: now(),
            updatedAt: now(),
          }
          // Regenerate page IDs
          dup.pages = dup.pages.map(p => ({ ...p, id: uid() }))
          dup.activePageId = dup.pages[0]?.id ?? ''
          set(s => ({ templates: [...s.templates, dup], activeTemplateId: dup.id }))
          return dup.id
        },

        // ── Page CRUD ──
        addPage: (name = 'New Page', preset = 'Trading') => {
          const page = createDefaultPage(name, preset)
          updateActive(t => ({
            ...t,
            pages: [...t.pages, page],
            activePageId: page.id,
          }))
          return page.id
        },

        deletePage: (pageId) => {
          updateActive(t => {
            if (t.pages.length <= 1) return t // keep at least 1 page
            const pages = t.pages.filter(p => p.id !== pageId)
            const activePageId = t.activePageId === pageId ? pages[0].id : t.activePageId
            return { ...t, pages, activePageId }
          })
        },

        renamePage: (pageId, name) => {
          updateActive(t => ({
            ...t,
            pages: t.pages.map(p => p.id === pageId ? { ...p, name } : p),
          }))
        },

        setPageIcon: (pageId, icon) => {
          updateActive(t => ({
            ...t,
            pages: t.pages.map(p => p.id === pageId ? { ...p, icon } : p),
          }))
        },

        setActivePage: (pageId) => {
          updateActive(t => ({ ...t, activePageId: pageId }))
        },

        reorderPages: (pageIds) => {
          updateActive(t => {
            const ordered = pageIds.map(id => t.pages.find(p => p.id === id)).filter(Boolean) as DashboardPage[]
            // Append any missing pages (safety)
            t.pages.forEach(p => { if (!ordered.find(o => o.id === p.id)) ordered.push(p) })
            return { ...t, pages: ordered }
          })
        },

        // ── Layout operations ──
        updateLayout: (layout) => {
          updateActivePage(p => ({ ...p, layout }))
        },

        setWidgetVisible: (widgetId, visible) => {
          updateActivePage(p => ({
            ...p,
            widgets: p.widgets.map(w => w.id === widgetId ? { ...w, visible } : w),
          }))
        },

        setWidgetColor: (widgetId, color) => {
          updateActivePage(p => ({
            ...p,
            widgets: p.widgets.map(w => w.id === widgetId ? { ...w, colorGroup: color } : w),
          }))
        },

        applyPreset: (presetKey) => {
          const presetLayout = PRESET_LAYOUTS[presetKey]
          if (!presetLayout) return
          updateActivePage(p => ({
            ...p,
            layout: presetLayout,
            widgets: p.widgets.map(w => ({
              ...w,
              visible: presetLayout.some(l => l.i === w.id),
            })),
          }))
        },

        // ── Export / Import ──
        exportTemplate: (id) => {
          const t = get().templates.find(t => t.id === id)
          if (!t) return null
          return { _format: 'bd_oms_template_v1', _exportedAt: now(), template: JSON.parse(JSON.stringify(t)) }
        },

        exportAllTemplates: () => {
          return get().templates.map(t => ({
            _format: 'bd_oms_template_v1' as const,
            _exportedAt: now(),
            template: JSON.parse(JSON.stringify(t)),
          }))
        },

        importTemplate: (data) => {
          if (data?._format !== 'bd_oms_template_v1' || !data?.template) return null
          const t: DashboardTemplate = {
            ...data.template,
            id: uid(),
            createdAt: now(),
            updatedAt: now(),
          }
          // Regenerate page IDs for safety
          t.pages = t.pages.map(p => ({ ...p, id: uid() }))
          t.activePageId = t.pages[0]?.id ?? ''
          set(s => ({ templates: [...s.templates, t], activeTemplateId: t.id }))
          return t.id
        },

        // ── Computed ──
        getActiveTemplate: () => {
          const s = get()
          return s.templates.find(t => t.id === s.activeTemplateId) ?? s.templates[0] ?? null
        },

        getActivePage: () => {
          const t = get().getActiveTemplate()
          if (!t) return null
          return t.pages.find(p => p.id === t.activePageId) ?? t.pages[0] ?? null
        },

        getVisibleLayout: () => {
          const page = get().getActivePage()
          if (!page) return []
          return page.layout.filter(l =>
            page.widgets.find(w => w.id === l.i)?.visible !== false
          )
        },
      }
    },
    {
      name: 'bd_oms_templates_v1',
      onRehydrateStorage: () => (state) => {
        if (state && state.templates.length > 0 && !state.activeTemplateId) {
          state.activeTemplateId = state.templates[0].id
        }
        // Ensure at least one template exists
        if (state && state.templates.length === 0) {
          const def = createDefaultTemplate()
          state.templates = [def]
          state.activeTemplateId = def.id
        }
      },
    }
  )
)
