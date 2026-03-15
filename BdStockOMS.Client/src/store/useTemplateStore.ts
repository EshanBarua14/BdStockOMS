// @ts-nocheck
// src/store/useTemplateStore.ts
// Day 55 — Multi-instance widget system, full page CRUD, export/import

import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { WIDGET_REGISTRY } from '../components/widgets/registry'

// ─── Types ────────────────────────────────────────────────────
export interface LayoutItem {
  i: string; x: number; y: number; w: number; h: number
  [key: string]: any
}

export interface WidgetInstance {
  instanceId: string   // unique e.g. "watchlist-1", "watchlist-2"
  widgetId: string     // registry key e.g. "watchlist"
  colorGroup: string | null
}

export interface DashboardPage {
  id: string
  name: string
  icon: string
  layout: LayoutItem[]
  instances: WidgetInstance[]
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

const PRESET_LAYOUTS: Record<string, LayoutItem[]> = {
  Trading: [
    { i:'ticker',    x:0,  y:0,  w:48, h:4  },
    { i:'index',     x:0,  y:4,  w:10, h:14 },
    { i:'chart',     x:10, y:4,  w:24, h:30 },
    { i:'orderbook', x:34, y:4,  w:14, h:20 },
    { i:'order',     x:0,  y:18, w:10, h:20 },
    { i:'executions',x:34, y:24, w:14, h:14 },
    { i:'rms',       x:0,  y:38, w:24, h:12 },
    { i:'notif',     x:24, y:38, w:24, h:12 },
  ],
  Research: [
    { i:'ticker',   x:0,  y:0,  w:48, h:4  },
    { i:'movers',   x:0,  y:4,  w:14, h:22 },
    { i:'chart',    x:14, y:4,  w:20, h:30 },
    { i:'heatmap',  x:34, y:4,  w:14, h:20 },
    { i:'news',     x:0,  y:26, w:14, h:20 },
    { i:'ai',       x:14, y:34, w:20, h:16 },
    { i:'watchlist',x:34, y:24, w:14, h:22 },
  ],
  Portfolio: [
    { i:'ticker',    x:0,  y:0,  w:48, h:4  },
    { i:'portfolio', x:0,  y:4,  w:24, h:24 },
    { i:'executions',x:24, y:4,  w:24, h:24 },
    { i:'rms',       x:0,  y:28, w:24, h:14 },
    { i:'order',     x:24, y:28, w:24, h:14 },
  ],
}

// Convert preset layout items (no suffix) into instances
function layoutToInstances(layout: LayoutItem[]): WidgetInstance[] {
  return layout.map(l => ({
    instanceId: l.i,
    widgetId: l.i.replace(/-\d+$/, ''),
    colorGroup: null,
  }))
}

function createDefaultPage(name = 'Main', presetKey = 'Trading', empty = false): DashboardPage {
  const layout = empty ? [] : (PRESET_LAYOUTS[presetKey] ?? PRESET_LAYOUTS.Trading)
  return {
    id: uid(),
    name,
    icon: '📊',
    layout,
    instances: layoutToInstances(layout),
  }
}

function createDefaultTemplate(): DashboardTemplate {
  const mainPage = createDefaultPage('My Dashboard', 'Trading', true) // empty by default
  return {
    id: uid(),
    name: 'My Workspace',
    description: 'Default workspace',
    pages: [mainPage],
    activePageId: mainPage.id,
    createdAt: now(),
    updatedAt: now(),
  }
}

// Find next available instance number for a widget on a page
function nextInstanceId(page: DashboardPage, widgetId: string): string {
  const existing = page.instances
    .filter(i => i.widgetId === widgetId)
    .map(i => {
      const m = i.instanceId.match(/-(\d+)$/)
      return m ? parseInt(m[1]) : 1
    })
  if (existing.length === 0) return widgetId
  const max = Math.max(...existing)
  return `${widgetId}-${max + 1}`
}

// Find a free grid position (bottom of current layout)
function findFreePosition(layout: LayoutItem[], w: number, h: number): { x: number; y: number } {
  if (layout.length === 0) return { x: 0, y: 0 }
  const COLS = 48
  // Try to find an empty rectangular region
  const maxY = Math.max(...layout.map(l => l.y + l.h), 0)
  for (let y = 0; y <= maxY; y++) {
    for (let x = 0; x <= COLS - w; x++) {
      // Check if rect (x,y,w,h) is free
      const occupied = layout.some(l =>
        x < l.x + l.w && x + w > l.x &&
        y < l.y + l.h && y + h > l.y
      )
      if (!occupied) return { x, y }
    }
  }
  // Fallback: append at bottom
  return { x: 0, y: maxY }
}

// ─── Store interface ───────────────────────────────────────────
interface TemplateStore {
  templates: DashboardTemplate[]
  activeTemplateId: string | null

  // Template CRUD
  createTemplate: (name: string, description?: string) => string
  deleteTemplate: (id: string) => void
  renameTemplate: (id: string, name: string) => void
  setActiveTemplate: (id: string) => void
  duplicateTemplate: (id: string) => string

  // Page CRUD
  addPage: (name?: string, preset?: string, layout?: any[], instances?: any[]) => string
  deletePage: (pageId: string) => void
  renamePage: (pageId: string, name: string) => void
  setPageIcon: (pageId: string, icon: string) => void
  setActivePage: (pageId: string) => void
  reorderPages: (pageIds: string[]) => void

  // Widget instance management
  addWidgetInstance: (widgetId: string) => string | null
  removeWidgetInstance: (instanceId: string) => void
  setWidgetColor: (instanceId: string, color: string | null) => void
  // Legacy compat
  setWidgetVisible: (widgetId: string, visible: boolean) => void

  // Layout
  updateLayout: (layout: LayoutItem[]) => void
  applyPreset: (presetKey: string) => void

  // Export / Import
  exportTemplate: (id: string) => ExportedTemplate | null
  exportAllTemplates: () => ExportedTemplate[]
  importTemplate: (data: ExportedTemplate) => string | null

  // Computed
  getActiveTemplate: () => DashboardTemplate | null
  getActivePage: () => DashboardPage | null
  getVisibleLayout: () => LayoutItem[]
}

export const useTemplateStore = create<TemplateStore>()(
  persist(
    (set, get) => {
      const updateActive = (fn: (t: DashboardTemplate) => DashboardTemplate) => {
        set(s => {
          const idx = s.templates.findIndex(t => t.id === s.activeTemplateId)
          if (idx < 0) return s
          const updated = [...s.templates]
          updated[idx] = { ...fn(updated[idx]), updatedAt: now() }
          return { templates: updated }
        })
      }

      const updateActivePage = (fn: (p: DashboardPage) => DashboardPage) => {
        updateActive(t => {
          const pages = t.pages.map(p => p.id === t.activePageId ? fn(p) : p)
          return { ...t, pages }
        })
      }

      return {
        templates: [createDefaultTemplate()],
        activeTemplateId: null,

        // ── Template CRUD ──────────────────────────────────────
        createTemplate: (name, description = '') => {
          const t = createDefaultTemplate()
          t.name = name; t.description = description
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
            return { templates: filtered, activeTemplateId: s.activeTemplateId === id ? filtered[0].id : s.activeTemplateId }
          })
        },

        renameTemplate: (id, name) => {
          set(s => ({ templates: s.templates.map(t => t.id === id ? { ...t, name, updatedAt: now() } : t) }))
        },

        setActiveTemplate: (id) => set({ activeTemplateId: id }),

        duplicateTemplate: (id) => {
          const src = get().templates.find(t => t.id === id)
          if (!src) return ''
          const dup: DashboardTemplate = {
            ...JSON.parse(JSON.stringify(src)),
            id: uid(), name: `${src.name} (Copy)`, createdAt: now(), updatedAt: now(),
          }
          dup.pages = dup.pages.map(p => ({ ...p, id: uid() }))
          dup.activePageId = dup.pages[0]?.id ?? ''
          set(s => ({ templates: [...s.templates, dup], activeTemplateId: dup.id }))
          return dup.id
        },

        // ── Page CRUD ──────────────────────────────────────────
        addPage: (name = 'New Page', preset = 'Trading', layout?: any[], instances?: any[]) => {
          const page = createDefaultPage(name, preset, true)
          if (layout) page.layout = JSON.parse(JSON.stringify(layout))
          if (instances) page.instances = JSON.parse(JSON.stringify(instances))
          updateActive(t => ({ ...t, pages: [...t.pages, page], activePageId: page.id }))
          return page.id
        },

        deletePage: (pageId) => {
          updateActive(t => {
            if (t.pages.length <= 1) return t
            const pages = t.pages.filter(p => p.id !== pageId)
            return { ...t, pages, activePageId: t.activePageId === pageId ? pages[0].id : t.activePageId }
          })
        },

        renamePage: (pageId, name) => {
          updateActive(t => ({ ...t, pages: t.pages.map(p => p.id === pageId ? { ...p, name } : p) }))
        },

        setPageIcon: (pageId, icon) => {
          updateActive(t => ({ ...t, pages: t.pages.map(p => p.id === pageId ? { ...p, icon } : p) }))
        },

        setActivePage: (pageId) => {
          updateActive(t => ({ ...t, activePageId: pageId }))
        },

        reorderPages: (pageIds) => {
          updateActive(t => {
            const ordered = pageIds.map(id => t.pages.find(p => p.id === id)).filter(Boolean) as DashboardPage[]
            t.pages.forEach(p => { if (!ordered.find(o => o.id === p.id)) ordered.push(p) })
            return { ...t, pages: ordered }
          })
        },

        // ── Widget instance management ─────────────────────────
        addWidgetInstance: (widgetId) => {
          const reg = WIDGET_REGISTRY[widgetId]
          if (!reg) return null
          let newId: string | null = null
          updateActivePage(p => {
            const instanceId = nextInstanceId(p, widgetId)
            newId = instanceId
            const minW = reg.minW ?? 4
            const minH = reg.minH ?? 6
            const w = Math.min(reg.defaultW ?? minW, 8)
            const h = Math.min(reg.defaultH ?? minH, 10)
            const pos = findFreePosition(p.layout, w, h)
            const newLayout: LayoutItem = { i: instanceId, x: pos.x, y: pos.y, w, h, minW, minH }
            const newInstance: WidgetInstance = { instanceId, widgetId, colorGroup: null }
            return {
              ...p,
              layout: [...p.layout, newLayout],
              instances: [...p.instances, newInstance],
            }
          })
          return newId
        },

        removeWidgetInstance: (instanceId) => {
          updateActivePage(p => ({
            ...p,
            layout: p.layout.filter(l => l.i !== instanceId),
            instances: p.instances.filter(i => i.instanceId !== instanceId),
          }))
        },

        setWidgetColor: (instanceId, color) => {
          updateActivePage(p => ({
            ...p,
            instances: p.instances.map(i => i.instanceId === instanceId ? { ...i, colorGroup: color } : i),
          }))
        },

        // Legacy compat: toggling visibility — now adds/removes instance
        setWidgetVisible: (widgetId, visible) => {
          if (visible) {
            get().addWidgetInstance(widgetId)
          } else {
            // Remove first instance of this widgetId
            const page = get().getActivePage()
            if (!page) return
            const inst = page.instances.find(i => i.widgetId === widgetId)
            if (inst) get().removeWidgetInstance(inst.instanceId)
          }
        },

        // ── Layout ────────────────────────────────────────────
        updateLayout: (layout) => {
          updateActivePage(p => ({ ...p, layout }))
        },

        applyPreset: (presetKey) => {
          const presetLayout = PRESET_LAYOUTS[presetKey]
          if (!presetLayout) return
          updateActivePage(p => ({
            ...p,
            layout: presetLayout,
            instances: layoutToInstances(presetLayout),
          }))
        },

        // ── Export / Import ───────────────────────────────────
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
            ...data.template, id: uid(), createdAt: now(), updatedAt: now(),
          }
          t.pages = t.pages.map(p => ({
            ...p,
            id: uid(),
            instances: p.instances ?? layoutToInstances(p.layout),
          }))
          t.activePageId = t.pages[0]?.id ?? ''
          set(s => ({ templates: [...s.templates, t], activeTemplateId: t.id }))
          return t.id
        },

        // ── Computed ──────────────────────────────────────────
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
          // Migrate old format (widgets array) to instances
          return page.layout
        },
      }
    },
    {
      name: 'bd_oms_templates_v1',
      onRehydrateStorage: () => (state, error) => {
        // Guard: clear corrupted storage and reset to default
        if (error || !state) {
          console.warn('[TemplateStore] Rehydration failed, resetting storage', error)
          try { localStorage.removeItem('bd-oms-templates') } catch {}
          return
        }
        try {
          if (!Array.isArray(state.templates) || state.templates.length === 0) {
            const def = createDefaultTemplate()
            state.templates = [def]
            state.activeTemplateId = def.id
            return
          }
          if (!state.activeTemplateId || !state.templates.find(t => t.id === state.activeTemplateId)) {
            state.activeTemplateId = state.templates[0].id
          }
          // Migrate pages that have widgets[] (old format) to instances[]
          state.templates = state.templates.map(t => ({
            ...t,
            pages: (t.pages ?? []).map(p => {
              if (!p.instances) {
                return { ...p, instances: layoutToInstances(p.layout ?? []) }
              }
              // Guard: ensure layout items have required fields
              return {
                ...p,
                layout: (p.layout ?? []).filter(l => l && l.i && typeof l.x === 'number'),
                instances: (p.instances ?? []).filter(i => i && i.instanceId && i.widgetId),
              }
            })
          }))
        } catch (e) {
          console.warn('[TemplateStore] Migration error, resetting', e)
          try { localStorage.removeItem('bd-oms-templates') } catch {}
          const def = createDefaultTemplate()
          state.templates = [def]
          state.activeTemplateId = def.id
        }
      },
    }
  )
)
