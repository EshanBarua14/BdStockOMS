// src/hooks/useTableState.ts
// Reusable hook: column visibility, drag-reorder, sort, search — all persisted to localStorage

import { useState, useCallback, useEffect, useRef } from "react"

export type SortDir = "asc" | "desc" | null

export interface ColumnDef<T = any> {
  key: string
  label: string
  visible: boolean
  width?: number          // px, optional
  sortable?: boolean
  render?: (row: T, value: any) => React.ReactNode
  getValue?: (row: T) => any   // for sort/filter — defaults to row[key]
}

export interface TableState {
  columns: ColumnDef[]
  sortKey: string | null
  sortDir: SortDir
  search: string
  page: number
  pageSize: number
}

export interface UseTableStateReturn<T> {
  columns: ColumnDef[]
  visibleColumns: ColumnDef[]
  sortKey: string | null
  sortDir: SortDir
  search: string
  page: number
  pageSize: number
  sortedFiltered: T[]
  setSort: (key: string) => void
  setSearch: (s: string) => void
  setPage: (p: number) => void
  setPageSize: (n: number) => void
  toggleColumn: (key: string) => void
  moveColumn: (fromKey: string, toKey: string) => void
  resetColumns: () => void
  dragProps: (key: string) => {
    draggable: true
    onDragStart: (e: React.DragEvent) => void
    onDragOver: (e: React.DragEvent) => void
    onDrop: (e: React.DragEvent) => void
    onDragEnd: () => void
  }
}

const STORAGE_KEY = (id: string) => `bd_oms_table_${id}`

function saveTablePrefs(id: string, cols: ColumnDef[], sortKey: string | null, sortDir: SortDir) {
  try {
    localStorage.setItem(STORAGE_KEY(id), JSON.stringify({
      columns: cols.map(c => ({ key: c.key, visible: c.visible, width: c.width })),
      sortKey,
      sortDir,
    }))
  } catch {}
}

function loadTablePrefs(id: string, defaults: ColumnDef[]): {
  columns: ColumnDef[]
  sortKey: string | null
  sortDir: SortDir
} {
  try {
    const raw = localStorage.getItem(STORAGE_KEY(id))
    if (!raw) return { columns: defaults, sortKey: null, sortDir: null }
    const saved = JSON.parse(raw)

    // Merge: keep render/getValue/sortable from defaults, restore order/visibility from saved
    const savedMap = new Map(saved.columns.map((c: any) => [c.key, c]))
    const merged: ColumnDef[] = []

    // First: saved order (for columns that still exist)
    saved.columns.forEach((s: any) => {
      const def = defaults.find(d => d.key === s.key)
      if (def) merged.push({ ...def, visible: s.visible, width: s.width ?? def.width })
    })

    // Then: any new columns from defaults not in saved
    defaults.forEach(d => {
      if (!savedMap.has(d.key)) merged.push(d)
    })

    return {
      columns: merged,
      sortKey: saved.sortKey ?? null,
      sortDir: saved.sortDir ?? null,
    }
  } catch {
    return { columns: defaults, sortKey: null, sortDir: null }
  }
}

export function useTableState<T>(
  id: string,
  data: T[],
  defaultColumns: ColumnDef<T>[],
  defaultPageSize = 50
): UseTableStateReturn<T> {
  const initialPrefs = () => loadTablePrefs(id, defaultColumns as ColumnDef[])

  const [columns, setColumns] = useState<ColumnDef[]>(() => initialPrefs().columns)
  const [sortKey, setSortKey] = useState<string | null>(() => initialPrefs().sortKey)
  const [sortDir, setSortDir] = useState<SortDir>(() => initialPrefs().sortDir)
  const [search, setSearchRaw] = useState("")
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(defaultPageSize)
  const dragKey = useRef<string | null>(null)

  // Persist column/sort prefs
  useEffect(() => {
    saveTablePrefs(id, columns, sortKey, sortDir)
  }, [id, columns, sortKey, sortDir])

  // Reset page when search changes
  const setSearch = useCallback((s: string) => {
    setSearchRaw(s)
    setPage(1)
  }, [])

  // Sort toggle
  const setSort = useCallback((key: string) => {
    setSortKey(prev => {
      if (prev !== key) { setSortDir("asc"); return key }
      setSortDir(d => d === "asc" ? "desc" : d === "desc" ? null : "asc")
      return key
    })
    setPage(1)
  }, [])

  // Column visibility
  const toggleColumn = useCallback((key: string) => {
    setColumns(prev => prev.map(c => c.key === key ? { ...c, visible: !c.visible } : c))
  }, [])

  // Drag-reorder columns
  const moveColumn = useCallback((fromKey: string, toKey: string) => {
    if (fromKey === toKey) return
    setColumns(prev => {
      const arr = [...prev]
      const fromIdx = arr.findIndex(c => c.key === fromKey)
      const toIdx = arr.findIndex(c => c.key === toKey)
      if (fromIdx < 0 || toIdx < 0) return prev
      const [item] = arr.splice(fromIdx, 1)
      arr.splice(toIdx, 0, item)
      return arr
    })
  }, [])

  const resetColumns = useCallback(() => {
    setColumns(defaultColumns as ColumnDef[])
    setSortKey(null)
    setSortDir(null)
    try { localStorage.removeItem(STORAGE_KEY(id)) } catch {}
  }, [id, defaultColumns])

  // Drag event factory
  const dragProps = useCallback((key: string) => ({
    draggable: true as const,
    onDragStart: (e: React.DragEvent) => {
      dragKey.current = key
      e.dataTransfer.effectAllowed = "move"
    },
    onDragOver: (e: React.DragEvent) => {
      e.preventDefault()
      e.dataTransfer.dropEffect = "move"
    },
    onDrop: (e: React.DragEvent) => {
      e.preventDefault()
      if (dragKey.current && dragKey.current !== key) {
        moveColumn(dragKey.current, key)
      }
      dragKey.current = null
    },
    onDragEnd: () => { dragKey.current = null },
  }), [moveColumn])

  // Sort + filter
  const sortedFiltered: T[] = (() => {
    let result = [...data]

    // Global search across all visible columns
    if (search.trim()) {
      const q = search.toLowerCase()
      result = result.filter(row =>
        columns
          .filter(c => c.visible)
          .some(c => {
            const val = c.getValue ? c.getValue(row) : (row as any)[c.key]
            return String(val ?? "").toLowerCase().includes(q)
          })
      )
    }

    // Sort
    if (sortKey && sortDir) {
      const col = columns.find(c => c.key === sortKey)
      result.sort((a, b) => {
        const av = col?.getValue ? col.getValue(a) : (a as any)[sortKey]
        const bv = col?.getValue ? col.getValue(b) : (b as any)[sortKey]
        const cmp = typeof av === "number" && typeof bv === "number"
          ? av - bv
          : String(av ?? "").localeCompare(String(bv ?? ""))
        return sortDir === "asc" ? cmp : -cmp
      })
    }

    return result
  })()

  const visibleColumns = columns.filter(c => c.visible)

  return {
    columns,
    visibleColumns,
    sortKey,
    sortDir,
    search,
    page,
    pageSize,
    sortedFiltered,
    setSort,
    setSearch,
    setPage,
    setPageSize,
    toggleColumn,
    moveColumn,
    resetColumns,
    dragProps,
  }
}
