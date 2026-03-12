// @ts-nocheck
// src/components/dashboard/DashboardTabs.tsx
// Day 52 — Multi-page dashboard tab bar with create/rename/delete/reorder

import { useState, useRef, useEffect } from 'react'
import { useTemplateStore } from '../../store/useTemplateStore'

const PAGE_ICONS = ['📊','📈','💹','🏦','⚡','🎯','🔍','📋','💼','🛡️','📰','🤖']

export function DashboardTabs() {
  const store = useTemplateStore()
  const template = store.getActiveTemplate()
  const activePage = store.getActivePage()

  const [editing, setEditing] = useState<string | null>(null)
  const [editName, setEditName] = useState('')
  const [iconPicker, setIconPicker] = useState<string | null>(null)
  const [contextMenu, setContextMenu] = useState<{ pageId: string; x: number; y: number } | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (editing && inputRef.current) inputRef.current.focus()
  }, [editing])

  useEffect(() => {
    if (!contextMenu) return
    const h = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) setContextMenu(null)
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [contextMenu])

  if (!template) return null
  const pages = template.pages

  const startRename = (pageId: string, currentName: string) => {
    setEditing(pageId)
    setEditName(currentName)
    setContextMenu(null)
  }

  const commitRename = () => {
    if (editing && editName.trim()) {
      store.renamePage(editing, editName.trim())
    }
    setEditing(null)
  }

  const handleContextMenu = (e: React.MouseEvent, pageId: string) => {
    e.preventDefault()
    setContextMenu({ pageId, x: e.clientX, y: e.clientY })
  }

  return (
    <div style={{
      display: 'flex', alignItems: 'center', height: 32, flexShrink: 0,
      background: 'var(--t-panel)', borderBottom: '1px solid var(--t-border)',
      padding: '0 6px', gap: 2, overflow: 'hidden',
    }}>
      {/* Page tabs */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1, overflow: 'auto', minWidth: 0 }}>
        {pages.map(page => {
          const isActive = page.id === activePage?.id
          const isEditing = editing === page.id

          return (
            <div
              key={page.id}
              onClick={() => !isEditing && store.setActivePage(page.id)}
              onContextMenu={e => handleContextMenu(e, page.id)}
              onDoubleClick={() => startRename(page.id, page.name)}
              style={{
                display: 'flex', alignItems: 'center', gap: 5,
                padding: '0 10px', height: 26, borderRadius: 6,
                cursor: isEditing ? 'text' : 'pointer',
                background: isActive ? 'var(--t-surface)' : 'transparent',
                border: isActive ? '1px solid var(--t-border)' : '1px solid transparent',
                transition: 'all 0.12s', flexShrink: 0, position: 'relative',
              }}
              onMouseEnter={e => { if (!isActive) e.currentTarget.style.background = 'var(--t-hover)' }}
              onMouseLeave={e => { if (!isActive) e.currentTarget.style.background = 'transparent' }}
            >
              {/* Icon */}
              <span
                style={{ fontSize: 11, cursor: 'pointer', userSelect: 'none' }}
                onClick={e => { e.stopPropagation(); setIconPicker(iconPicker === page.id ? null : page.id) }}
              >{page.icon}</span>

              {/* Name or edit input */}
              {isEditing ? (
                <input
                  ref={inputRef}
                  value={editName}
                  onChange={e => setEditName(e.target.value)}
                  onBlur={commitRename}
                  onKeyDown={e => { if (e.key === 'Enter') commitRename(); if (e.key === 'Escape') setEditing(null) }}
                  style={{
                    width: Math.max(40, editName.length * 7),
                    background: 'var(--t-hover)', border: '1px solid var(--t-accent)',
                    borderRadius: 3, padding: '1px 4px', outline: 'none',
                    color: 'var(--t-text1)', fontSize: 10, fontWeight: 600,
                    fontFamily: "'JetBrains Mono', monospace",
                  }}
                />
              ) : (
                <span style={{
                  fontSize: 10, fontWeight: isActive ? 700 : 500,
                  color: isActive ? 'var(--t-text1)' : 'var(--t-text3)',
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.02em', whiteSpace: 'nowrap',
                }}>{page.name}</span>
              )}

              {/* Active indicator */}
              {isActive && (
                <div style={{
                  position: 'absolute', bottom: -1, left: '20%', right: '20%', height: 1,
                  background: 'var(--t-accent)', opacity: 0.6,
                }} />
              )}

              {/* Icon picker dropdown */}
              {iconPicker === page.id && (
                <div
                  style={{
                    position: 'absolute', top: 28, left: 0, zIndex: 300,
                    background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
                    borderRadius: 8, padding: 6, display: 'grid',
                    gridTemplateColumns: 'repeat(6, 1fr)', gap: 2,
                    boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
                  }}
                  onClick={e => e.stopPropagation()}
                >
                  {PAGE_ICONS.map(icon => (
                    <button key={icon} onClick={() => { store.setPageIcon(page.id, icon); setIconPicker(null) }}
                      style={{
                        width: 26, height: 26, display: 'flex', alignItems: 'center', justifyContent: 'center',
                        background: page.icon === icon ? 'var(--t-hover)' : 'transparent',
                        border: page.icon === icon ? '1px solid var(--t-accent)' : '1px solid transparent',
                        borderRadius: 4, cursor: 'pointer', fontSize: 13,
                      }}
                    >{icon}</button>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* Add page button */}
      <button
        onClick={() => store.addPage()}
        title="Add new page"
        style={{
          width: 24, height: 24, borderRadius: 5,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          background: 'transparent', border: '1px solid var(--t-border)',
          cursor: 'pointer', color: 'var(--t-text3)', fontSize: 13,
          transition: 'all 0.12s', flexShrink: 0,
        }}
        onMouseEnter={e => { e.currentTarget.style.color = 'var(--t-accent)'; e.currentTarget.style.borderColor = 'var(--t-accent)' }}
        onMouseLeave={e => { e.currentTarget.style.color = 'var(--t-text3)'; e.currentTarget.style.borderColor = 'var(--t-border)' }}
      >+</button>

      {/* Save indicator */}
      <button
        onClick={() => {/* save handled by auto-persist */}}
        title="Auto-saved"
        style={{
          width: 24, height: 24, borderRadius: 5,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          background: 'transparent', border: 'none',
          cursor: 'default', color: 'var(--t-text3)', fontSize: 11,
          flexShrink: 0,
        }}
      >💾</button>

      {/* Context menu */}
      {contextMenu && (
        <div ref={menuRef} style={{
          position: 'fixed', left: contextMenu.x, top: contextMenu.y, zIndex: 9999,
          background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
          borderRadius: 8, padding: 4, minWidth: 140,
          boxShadow: '0 12px 32px rgba(0,0,0,0.6)',
        }}>
          {[
            { label: 'Rename', action: () => {
              const p = pages.find(p => p.id === contextMenu.pageId)
              if (p) startRename(p.id, p.name)
            }},
            { label: 'Duplicate', action: () => {
              const p = pages.find(p => p.id === contextMenu.pageId)
              if (p) {
                const newId = store.addPage(p.name + ' (Copy)')
                // Copy layout from source
                setTimeout(() => {
                  store.setActivePage(newId)
                }, 0)
              }
              setContextMenu(null)
            }},
            { label: 'Delete', action: () => { store.deletePage(contextMenu.pageId); setContextMenu(null) }, danger: true },
          ].map(item => (
            <button key={item.label} onClick={item.action} style={{
              display: 'block', width: '100%', padding: '6px 10px', borderRadius: 5,
              background: 'transparent', border: 'none', cursor: 'pointer', textAlign: 'left',
              color: item.danger ? 'var(--t-sell)' : 'var(--t-text2)', fontSize: 11,
              fontFamily: "'JetBrains Mono', monospace",
            }}
              onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
              onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
            >{item.label}</button>
          ))}
        </div>
      )}
    </div>
  )
}
