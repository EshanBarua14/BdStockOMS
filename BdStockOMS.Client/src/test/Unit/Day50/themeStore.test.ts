import { describe, it, expect } from "vitest"
import { THEMES, ACCENTS, DENSITIES } from "@/store/themeStore"

describe("Theme System", () => {
  it("has 12 themes", () => {
    expect(THEMES).toHaveLength(12)
  })

  it("has 8 accent colors", () => {
    expect(ACCENTS).toHaveLength(8)
  })

  it("has 3 density options", () => {
    expect(DENSITIES).toHaveLength(3)
  })

  it("all themes have required fields", () => {
    THEMES.forEach(t => {
      expect(t.id).toBeTruthy()
      expect(t.label).toBeTruthy()
      expect(t.bg).toMatch(/^#/)
      expect(t.surface).toMatch(/^#/)
      expect(typeof t.dark).toBe("boolean")
    })
  })

  it("all accents have a color and glow", () => {
    ACCENTS.forEach(a => {
      expect(a.color).toMatch(/^#/)
      expect(a.glow).toMatch(/rgba/)
    })
  })

  it("obsidian is darkest theme", () => {
    const obsidian = THEMES.find(t => t.id === "obsidian")
    expect(obsidian?.dark).toBe(true)
    expect(obsidian?.bg).toBe("#080C14")
  })

  it("teal is default accent", () => {
    const teal = ACCENTS.find(a => a.id === "teal")
    expect(teal?.color).toBe("#00D4AA")
  })

  it("theme ids are unique", () => {
    const ids = THEMES.map(t => t.id)
    expect(new Set(ids).size).toBe(ids.length)
  })

  it("accent ids are unique", () => {
    const ids = ACCENTS.map(a => a.id)
    expect(new Set(ids).size).toBe(ids.length)
  })

  it("light themes have dark=false", () => {
    const light = THEMES.filter(t => t.category === "Light")
    light.forEach(t => expect(t.dark).toBe(false))
  })
})
