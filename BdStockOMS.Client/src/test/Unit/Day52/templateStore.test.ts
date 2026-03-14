// @ts-nocheck
import { describe, it, expect, beforeEach } from "vitest"
import { useTemplateStore } from "@/store/useTemplateStore"

// Reset store before each test
beforeEach(() => {
  const store = useTemplateStore.getState()
  // Clear all templates and recreate default
  useTemplateStore.setState({
    templates: [],
    activeTemplateId: null,
  })
  // Create fresh default
  store.createTemplate('Test Default', 'test')
})

describe("useTemplateStore", () => {
  it("creates a template with correct structure", () => {
    const s = useTemplateStore.getState()
    expect(s.templates.length).toBeGreaterThanOrEqual(1)
    const t = s.templates[0]
    expect(t.name).toBeTruthy()
    expect(t.id).toBeTruthy()
    expect(t.pages.length).toBeGreaterThanOrEqual(1)
    expect(t.createdAt).toBeTruthy()
    expect(t.updatedAt).toBeTruthy()
  })

  it("createTemplate adds a new template and sets it active", () => {
    const s = useTemplateStore.getState()
    const before = s.templates.length
    const id = s.createTemplate("My Trading Setup", "Custom layout")
    const after = useTemplateStore.getState()
    expect(after.templates.length).toBe(before + 1)
    expect(after.activeTemplateId).toBe(id)
    const t = after.templates.find(t => t.id === id)
    expect(t?.name).toBe("My Trading Setup")
    expect(t?.description).toBe("Custom layout")
  })

  it("deleteTemplate removes template and falls back", () => {
    const s = useTemplateStore.getState()
    s.createTemplate("Second", "")
    const before = useTemplateStore.getState().templates.length
    const idToDelete = useTemplateStore.getState().templates[0].id
    useTemplateStore.getState().deleteTemplate(idToDelete)
    const after = useTemplateStore.getState()
    expect(after.templates.length).toBe(before - 1)
    expect(after.activeTemplateId).toBeTruthy()
  })

  it("deleteTemplate keeps at least one template", () => {
    const s = useTemplateStore.getState()
    // Delete all but should keep one
    const ids = s.templates.map(t => t.id)
    ids.forEach(id => useTemplateStore.getState().deleteTemplate(id))
    const after = useTemplateStore.getState()
    expect(after.templates.length).toBeGreaterThanOrEqual(1)
  })

  it("renameTemplate updates the name", () => {
    const s = useTemplateStore.getState()
    const id = s.templates[0].id
    s.renameTemplate(id, "Renamed Template")
    const t = useTemplateStore.getState().templates.find(t => t.id === id)
    expect(t?.name).toBe("Renamed Template")
  })

  it("duplicateTemplate creates a copy", () => {
    const s = useTemplateStore.getState()
    const srcId = s.templates[0].id
    const before = s.templates.length
    const dupId = s.duplicateTemplate(srcId)
    const after = useTemplateStore.getState()
    expect(after.templates.length).toBe(before + 1)
    const dup = after.templates.find(t => t.id === dupId)
    expect(dup?.name).toContain("(Copy)")
    expect(dup?.id).not.toBe(srcId)
  })

  it("addPage creates a new page in active template", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    const t = useTemplateStore.getState().getActiveTemplate()
    const beforePages = t?.pages.length ?? 0
    useTemplateStore.getState().addPage("Research View", "Research")
    const after = useTemplateStore.getState().getActiveTemplate()
    expect(after?.pages.length).toBe(beforePages + 1)
    expect(after?.pages[after.pages.length - 1].name).toBe("Research View")
  })

  it("deletePage removes page but keeps at least one", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    // Add a second page first
    useTemplateStore.getState().addPage("Temp Page")
    const t = useTemplateStore.getState().getActiveTemplate()!
    expect(t.pages.length).toBeGreaterThanOrEqual(2)
    useTemplateStore.getState().deletePage(t.pages[1].id)
    const after = useTemplateStore.getState().getActiveTemplate()!
    expect(after.pages.length).toBe(t.pages.length - 1)
    // Try deleting last page — should not work
    useTemplateStore.getState().deletePage(after.pages[0].id)
    expect(useTemplateStore.getState().getActiveTemplate()!.pages.length).toBeGreaterThanOrEqual(1)
  })

  it("renamePage updates page name", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    const pageId = useTemplateStore.getState().getActivePage()!.id
    useTemplateStore.getState().renamePage(pageId, "My Custom Page")
    expect(useTemplateStore.getState().getActivePage()!.name).toBe("My Custom Page")
  })

  it("setPageIcon updates page icon", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    const pageId = useTemplateStore.getState().getActivePage()!.id
    useTemplateStore.getState().setPageIcon(pageId, "🚀")
    expect(useTemplateStore.getState().getActivePage()!.icon).toBe("🚀")
  })

  it("setWidgetVisible removes instance when false, adds when true", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    // hide: removes instance
    useTemplateStore.getState().setWidgetVisible("chart", false)
    const page1 = useTemplateStore.getState().getActivePage()!
    const inst1 = page1.instances?.find(i => i.widgetId === "chart")
    expect(inst1).toBeUndefined()
    // show: adds instance back
    useTemplateStore.getState().setWidgetVisible("chart", true)
    const page2 = useTemplateStore.getState().getActivePage()!
    const inst2 = page2.instances?.find(i => i.widgetId === "chart")
    expect(inst2).toBeDefined()
  })

  it("setWidgetColor updates instance color group", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    const page0 = useTemplateStore.getState().getActivePage()!
    const inst = page0.instances?.find(i => i.widgetId === "chart")
    if (inst) {
      useTemplateStore.getState().setWidgetColor(inst.instanceId, "blue")
      const page = useTemplateStore.getState().getActivePage()!
      const updated = page.instances?.find(i => i.instanceId === inst.instanceId)
      expect(updated?.colorGroup).toBe("blue")
    } else {
      // chart not in preset — add it first
      useTemplateStore.getState().addWidgetInstance("chart")
      const page2 = useTemplateStore.getState().getActivePage()!
      const newInst = page2.instances?.find(i => i.widgetId === "chart")!
      useTemplateStore.getState().setWidgetColor(newInst.instanceId, "blue")
      const page3 = useTemplateStore.getState().getActivePage()!
      const updated = page3.instances?.find(i => i.instanceId === newInst.instanceId)
      expect(updated?.colorGroup).toBe("blue")
    }
  })

  it("applyPreset changes layout on active page", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    useTemplateStore.getState().applyPreset("Portfolio")
    const page = useTemplateStore.getState().getActivePage()!
    expect(page.layout.some(l => l.i === "portfolio")).toBe(true)
  })

  it("getVisibleLayout filters hidden widgets", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    useTemplateStore.getState().applyPreset("Trading")
    useTemplateStore.getState().setWidgetVisible("ticker", false)
    const visible = useTemplateStore.getState().getVisibleLayout()
    expect(visible.find(l => l.i === "ticker")).toBeUndefined()
  })

  it("exportTemplate returns valid format", () => {
    const s = useTemplateStore.getState()
    const id = s.templates[0].id
    const exported = s.exportTemplate(id)
    expect(exported).not.toBeNull()
    expect(exported!._format).toBe("bd_oms_template_v1")
    expect(exported!._exportedAt).toBeTruthy()
    expect(exported!.template.name).toBeTruthy()
    expect(exported!.template.pages.length).toBeGreaterThan(0)
  })

  it("importTemplate creates a new template from export", () => {
    const s = useTemplateStore.getState()
    const exported = s.exportTemplate(s.templates[0].id)!
    const before = useTemplateStore.getState().templates.length
    const newId = useTemplateStore.getState().importTemplate(exported)
    expect(newId).toBeTruthy()
    const after = useTemplateStore.getState()
    expect(after.templates.length).toBe(before + 1)
    expect(after.activeTemplateId).toBe(newId)
  })

  it("importTemplate rejects invalid format", () => {
    const result = useTemplateStore.getState().importTemplate({ _format: 'wrong', _exportedAt: '', template: null } as any)
    expect(result).toBeNull()
  })

  it("exportAllTemplates returns array of all templates", () => {
    const s = useTemplateStore.getState()
    s.createTemplate("Extra")
    const all = useTemplateStore.getState().exportAllTemplates()
    expect(all.length).toBe(useTemplateStore.getState().templates.length)
    all.forEach(e => {
      expect(e._format).toBe("bd_oms_template_v1")
    })
  })

  it("multiple pages can coexist with different layouts", () => {
    const s = useTemplateStore.getState()
    s.setActiveTemplate(s.templates[0].id)
    useTemplateStore.getState().applyPreset("Trading")
    useTemplateStore.getState().addPage("Page 2", "Research")
    const t = useTemplateStore.getState().getActiveTemplate()!
    expect(t.pages.length).toBe(2)
    expect(t.pages[0].layout).not.toEqual(t.pages[1].layout)
  })
})
