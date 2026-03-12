// @ts-nocheck
// src/components/dashboard/TemplateManager.tsx
// Day 52 — Template management drawer: save/load/delete/rename/export/import

import { useState, useRef } from 'react'
import { useTemplateStore } from '../../store/useTemplateStore'

export function TemplateManager({ open, onClose }: { open: boolean; onClose: () => void }) {
  const store = useTemplateStore()
  const templates = store.templates
  const activeId = store.activeTemplateId

  const [creating, setCreating] = useState(false)
  const [newName, setNewName] = useState('')
  const [newDesc, setNewDesc] = useState('')
  const [renaming, setRenaming] = useState<string | null>(null)
  const [renameName, setRenameName] = useState('')
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)
  const [importError, setImportError] = useState<string | null>(null)
  const [importSuccess, setImportSuccess] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  if (!open) return null

  const handleCreate = () => {
    if (!newName.trim()) return
    store.createTemplate(newName.trim(), newDesc.trim())
    setNewName(''); setNewDesc(''); setCreating(false)
  }

  const handleExport = (id: string) => {
    const data = store.exportTemplate(id)
    if (!data) return
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `template-${data.template.name.replace(/\s+/g, '-').toLowerCase()}.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  const handleExportAll = () => {
    const all = store.exportAllTemplates()
    const blob = new Blob([JSON.stringify(all, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = 'bd-oms-all-templates.json'; a.click()
    URL.revokeObjectURL(url)
  }

  const handleImport = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setImportError(null); setImportSuccess(false)
    const reader = new FileReader()
    reader.onload = (ev) => {
      try {
        const raw = JSON.parse(ev.target?.result as string)
        // Support single or array
        const items = Array.isArray(raw) ? raw : [raw]
        let imported = 0
        items.forEach(item => {
          const id = store.importTemplate(item)
          if (id) imported++
        })
        if (imported > 0) {
          setImportSuccess(true)
          setTimeout(() => setImportSuccess(false), 3000)
        } else {
          setImportError('Invalid template format')
        }
      } catch {
        setImportError('Failed to parse JSON file')
      }
    }
    reader.readAsText(file)
    if (fileRef.current) fileRef.current.value = ''
  }

  const handleDelete = (id: string) => {
    store.deleteTemplate(id)
    setConfirmDelete(null)
  }

  const commitRename = (id: string) => {
    if (renameName.trim()) store.renameTemplate(id, renameName.trim())
    setRenaming(null)
  }

  const S = {
    overlay: {
      position: 'fixed' as const, inset: 0, zIndex: 9000,
      background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
      display: 'flex', justifyContent: 'flex-end',
    },
    drawer: {
      width: 380, maxWidth: '90vw', height: '100%',
      background: 'var(--t-surface)', borderLeft: '1px solid var(--t-border)',
      display: 'flex', flexDirection: 'column' as const, overflow: 'hidden',
      boxShadow: '-16px 0 48px rgba(0,0,0,0.4)',
    },
    header: {
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '14px 16px', borderBottom: '1px solid var(--t-border)', flexShrink: 0,
    },
    title: {
      fontSize: 13, fontWeight: 700, color: 'var(--t-text1)',
      fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.04em',
    },
    body: { flex: 1, overflow: 'auto', padding: 12 },
    btn: (primary = false) => ({
      padding: '6px 12px', fontSize: 10, borderRadius: 6, fontWeight: 600,
      border: primary ? 'none' : '1px solid var(--t-border)',
      background: primary ? 'var(--t-accent)' : 'var(--t-hover)',
      color: primary ? '#000' : 'var(--t-text2)',
      cursor: 'pointer', fontFamily: "'JetBrains Mono', monospace",
      transition: 'all 0.12s',
    }),
    input: {
      width: '100%', boxSizing: 'border-box' as const, padding: '7px 10px',
      background: 'var(--t-hover)', border: '1px solid var(--t-border)',
      borderRadius: 6, color: 'var(--t-text1)', fontSize: 11, outline: 'none',
      fontFamily: "'JetBrains Mono', monospace",
    },
    card: (isActive: boolean) => ({
      background: isActive ? 'var(--t-hover)' : 'var(--t-panel)',
      border: `1px solid ${isActive ? 'var(--t-accent)' : 'var(--t-border)'}`,
      borderRadius: 10, padding: 12, marginBottom: 8,
      transition: 'all 0.15s', position: 'relative' as const,
    }),
  }

  return (
    <div style={S.overlay} onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div style={S.drawer}>
        {/* Header */}
        <div style={S.header}>
          <span style={S.title}>Dashboard Templates</span>
          <button onClick={onClose} style={{
            background: 'none', border: 'none', color: 'var(--t-text3)',
            cursor: 'pointer', fontSize: 14,
          }}>✕</button>
        </div>

        {/* Actions bar */}
        <div style={{
          display: 'flex', gap: 6, padding: '10px 12px',
          borderBottom: '1px solid var(--t-border)', flexShrink: 0, flexWrap: 'wrap',
        }}>
          <button onClick={() => setCreating(true)} style={S.btn(true)}>+ New Template</button>
          <button onClick={handleExportAll} style={S.btn()}>Export All</button>
          <button onClick={() => fileRef.current?.click()} style={S.btn()}>Import</button>
          <input ref={fileRef} type="file" accept=".json" onChange={handleImport} style={{ display: 'none' }} />
        </div>

        {/* Status messages */}
        {importError && (
          <div style={{ padding: '6px 12px', background: 'rgba(255,23,68,0.08)', color: 'var(--t-sell)', fontSize: 10, fontFamily: "'JetBrains Mono', monospace" }}>
            {importError}
          </div>
        )}
        {importSuccess && (
          <div style={{ padding: '6px 12px', background: 'rgba(0,230,118,0.08)', color: 'var(--t-buy)', fontSize: 10, fontFamily: "'JetBrains Mono', monospace" }}>
            Template imported successfully
          </div>
        )}

        {/* Create form */}
        {creating && (
          <div style={{ padding: 12, borderBottom: '1px solid var(--t-border)', background: 'var(--t-panel)' }}>
            <div style={{ marginBottom: 6 }}>
              <input placeholder="Template name" value={newName} onChange={e => setNewName(e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') handleCreate() }}
                style={S.input} autoFocus />
            </div>
            <div style={{ marginBottom: 8 }}>
              <input placeholder="Description (optional)" value={newDesc} onChange={e => setNewDesc(e.target.value)}
                onKeyDown={e => { if (e.key === 'Enter') handleCreate() }}
                style={S.input} />
            </div>
            <div style={{ display: 'flex', gap: 6 }}>
              <button onClick={handleCreate} style={S.btn(true)}>Create</button>
              <button onClick={() => { setCreating(false); setNewName(''); setNewDesc('') }} style={S.btn()}>Cancel</button>
            </div>
          </div>
        )}

        {/* Template list */}
        <div style={S.body}>
          {templates.length === 0 && (
            <div style={{ textAlign: 'center', color: 'var(--t-text3)', fontSize: 11, padding: 24 }}>
              No templates yet. Create one to get started.
            </div>
          )}

          {templates.map(t => {
            const isActive = t.id === activeId
            return (
              <div key={t.id} style={S.card(isActive)}>
                {/* Active indicator */}
                {isActive && (
                  <div style={{
                    position: 'absolute', top: 0, left: '10%', right: '10%', height: 1,
                    background: 'linear-gradient(90deg, transparent, var(--t-accent), transparent)', opacity: 0.5,
                  }} />
                )}

                {/* Name + badge */}
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                  {renaming === t.id ? (
                    <input value={renameName} onChange={e => setRenameName(e.target.value)}
                      onBlur={() => commitRename(t.id)}
                      onKeyDown={e => { if (e.key === 'Enter') commitRename(t.id); if (e.key === 'Escape') setRenaming(null) }}
                      style={{ ...S.input, width: 180, padding: '3px 8px' }} autoFocus />
                  ) : (
                    <span style={{
                      fontSize: 12, fontWeight: 700, color: isActive ? 'var(--t-accent)' : 'var(--t-text1)',
                    }}>{t.name}</span>
                  )}
                  {isActive && (
                    <span style={{
                      fontSize: 8, fontWeight: 700, padding: '2px 6px', borderRadius: 4,
                      background: 'var(--t-accent)', color: '#000',
                      fontFamily: "'JetBrains Mono', monospace",
                    }}>ACTIVE</span>
                  )}
                </div>

                {/* Meta */}
                <div style={{ fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", marginBottom: 8 }}>
                  {t.pages.length} page{t.pages.length !== 1 ? 's' : ''} · Updated {new Date(t.updatedAt).toLocaleDateString()}
                  {t.description && <span> · {t.description}</span>}
                </div>

                {/* Actions */}
                <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                  {!isActive && (
                    <button onClick={() => store.setActiveTemplate(t.id)} style={S.btn(true)}>Load</button>
                  )}
                  <button onClick={() => store.duplicateTemplate(t.id)} style={S.btn()}>Duplicate</button>
                  <button onClick={() => { setRenaming(t.id); setRenameName(t.name) }} style={S.btn()}>Rename</button>
                  <button onClick={() => handleExport(t.id)} style={S.btn()}>Export</button>
                  {confirmDelete === t.id ? (
                    <>
                      <button onClick={() => handleDelete(t.id)}
                        style={{ ...S.btn(), color: 'var(--t-sell)', borderColor: 'var(--t-sell)' }}>Confirm</button>
                      <button onClick={() => setConfirmDelete(null)} style={S.btn()}>Cancel</button>
                    </>
                  ) : (
                    <button onClick={() => setConfirmDelete(t.id)}
                      style={{ ...S.btn(), color: 'var(--t-sell)' }}>Delete</button>
                  )}
                </div>
              </div>
            )
          })}
        </div>

        {/* Footer */}
        <div style={{
          padding: '10px 16px', borderTop: '1px solid var(--t-border)',
          fontSize: 9, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace",
          textAlign: 'center', flexShrink: 0,
        }}>
          Templates auto-saved · Share via Export/Import
        </div>
      </div>
    </div>
  )
}
