// src/components/ui/DataTable.tsx
// Universal table: drag-reorder columns, sort, search, column visibility toggle
// Drop-in for Orders, Executions, Watchlist, Movers — any list widget

import React, { useState, useRef } from "react"
import type { ColumnDef, UseTableStateReturn } from "../../hooks/useTableState"

interface DataTableProps<T> {
  tableState: UseTableStateReturn<T>
  rowKey: (row: T) => string | number
  onRowClick?: (row: T) => void
  rowClassName?: (row: T) => string
  emptyMessage?: string
  maxHeight?: string       // e.g. "calc(100% - 60px)"
  showSearch?: boolean
  showColumnPicker?: boolean
  showPagination?: boolean
  stickyHeader?: boolean
  compact?: boolean
}

// ─── Sort icon ────────────────────────────────────────────────────────────────
function SortIcon({ dir }: { dir: "asc" | "desc" | null }) {
  return (
    <span className="inline-flex flex-col ml-1 opacity-50">
      <svg className={`w-2 h-2 -mb-0.5 ${dir === "asc" ? "opacity-100 text-blue-400" : ""}`} viewBox="0 0 6 4" fill="currentColor">
        <path d="M3 0L6 4H0L3 0z"/>
      </svg>
      <svg className={`w-2 h-2 ${dir === "desc" ? "opacity-100 text-blue-400" : ""}`} viewBox="0 0 6 4" fill="currentColor">
        <path d="M3 4L0 0H6L3 4z"/>
      </svg>
    </span>
  )
}

// ─── Column Picker Popover ─────────────────────────────────────────────────────
function ColumnPicker<T>({ tableState }: { tableState: UseTableStateReturn<T> }) {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (!open) return
    const h = (e: MouseEvent) => { if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false) }
    document.addEventListener("mousedown", h)
    return () => document.removeEventListener("mousedown", h)
  }, [open])

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(p => !p)}
        className="flex items-center gap-1 px-2 py-1 text-[10px] rounded text-zinc-400
                   hover:text-zinc-200 hover:bg-zinc-700/60 border border-zinc-700/50 transition-all"
        title="Configure columns"
      >
        <svg className="w-3 h-3" viewBox="0 0 12 12" fill="none">
          <rect x="1" y="1" width="4" height="10" rx="0.5" stroke="currentColor" strokeWidth="1"/>
          <rect x="7" y="1" width="4" height="10" rx="0.5" stroke="currentColor" strokeWidth="1"/>
        </svg>
        Columns
      </button>
      {open && (
        <div className="absolute right-0 top-7 z-50 bg-zinc-900 border border-zinc-700 rounded-lg shadow-2xl p-2 w-44"
             style={{ minWidth: 160 }}>
          <div className="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-2 px-1">
            Show / Hide · Drag to reorder
          </div>
          {tableState.columns.map(col => (
            <div
              key={col.key}
              className="flex items-center gap-2 px-2 py-1.5 rounded hover:bg-zinc-800 cursor-grab group"
              {...tableState.dragProps(col.key)}
            >
              <span className="text-zinc-600 group-hover:text-zinc-400 transition-colors">
                <svg className="w-2.5 h-2.5" viewBox="0 0 8 12" fill="currentColor">
                  <circle cx="2" cy="2" r="1"/><circle cx="6" cy="2" r="1"/>
                  <circle cx="2" cy="6" r="1"/><circle cx="6" cy="6" r="1"/>
                  <circle cx="2" cy="10" r="1"/><circle cx="6" cy="10" r="1"/>
                </svg>
              </span>
              <input
                type="checkbox"
                checked={col.visible}
                onChange={() => tableState.toggleColumn(col.key)}
                className="w-3 h-3 accent-blue-500 cursor-pointer flex-shrink-0"
                onClick={e => e.stopPropagation()}
              />
              <span className="text-xs text-zinc-300 truncate">{col.label}</span>
            </div>
          ))}
          <div className="border-t border-zinc-700/50 mt-1.5 pt-1.5">
            <button
              onClick={tableState.resetColumns}
              className="w-full text-[10px] text-zinc-500 hover:text-zinc-300 py-1 px-2 text-left hover:bg-zinc-800 rounded transition-colors"
            >
              Reset to default
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

// ─── Main DataTable ────────────────────────────────────────────────────────────
export function DataTable<T>({
  tableState,
  rowKey,
  onRowClick,
  rowClassName,
  emptyMessage = "No data",
  maxHeight = "100%",
  showSearch = true,
  showColumnPicker = true,
  showPagination = true,
  stickyHeader = true,
  compact = true,
}: DataTableProps<T>) {
  const {
    visibleColumns, sortedFiltered, sortKey, sortDir,
    search, setSearch, setSort, page, pageSize, setPage, dragProps,
  } = tableState

  const totalPages = Math.max(1, Math.ceil(sortedFiltered.length / pageSize))
  const pageData = sortedFiltered.slice((page - 1) * pageSize, page * pageSize)
  const cellPad = compact ? "px-2 py-1" : "px-3 py-2"
  const fontSize = compact ? "text-[11px]" : "text-xs"
  const dragOver = useRef<string | null>(null)

  return (
    <div className="flex flex-col h-full min-h-0">
      {/* ── Toolbar ── */}
      {(showSearch || showColumnPicker) && (
        <div className="flex items-center gap-2 px-2 py-1.5 border-b border-zinc-800 flex-shrink-0">
          {showSearch && (
            <div className="flex items-center gap-1.5 flex-1 bg-zinc-800/60 border border-zinc-700/50 rounded px-2 py-1">
              <svg className="w-3 h-3 text-zinc-500 flex-shrink-0" viewBox="0 0 12 12" fill="none">
                <circle cx="5" cy="5" r="3.5" stroke="currentColor" strokeWidth="1.2"/>
                <path d="M7.5 7.5l2.5 2.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
              </svg>
              <input
                type="text"
                value={search}
                onChange={e => setSearch(e.target.value)}
                placeholder="Search…"
                className="flex-1 bg-transparent text-[11px] text-zinc-200 placeholder-zinc-600 outline-none min-w-0"
              />
              {search && (
                <button onClick={() => setSearch("")} className="text-zinc-500 hover:text-zinc-300 transition-colors">
                  <svg className="w-3 h-3" viewBox="0 0 10 10" fill="none">
                    <path d="M1 1l8 8M9 1L1 9" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
                  </svg>
                </button>
              )}
            </div>
          )}
          {showColumnPicker && <ColumnPicker tableState={tableState} />}
          <span className="text-[10px] text-zinc-600 flex-shrink-0">
            {sortedFiltered.length} rows
          </span>
        </div>
      )}

      {/* ── Table ── */}
      <div className="flex-1 overflow-auto min-h-0" style={{ maxHeight }}>
        <table className="w-full border-collapse">
          <thead className={stickyHeader ? "sticky top-0 z-10" : ""}>
            <tr className="bg-zinc-900 border-b border-zinc-700/60">
              {visibleColumns.map(col => (
                <th
                  key={col.key}
                  className={`
                    ${cellPad} ${fontSize} font-semibold text-zinc-400 uppercase tracking-wider
                    text-left whitespace-nowrap select-none border-r border-zinc-800
                    ${col.sortable !== false ? "cursor-pointer hover:text-zinc-200 hover:bg-zinc-800/60" : ""}
                    transition-colors duration-100
                  `}
                  style={col.width ? { width: col.width, minWidth: col.width } : {}}
                  onClick={() => col.sortable !== false && setSort(col.key)}
                  {...dragProps(col.key)}
                  onDragEnter={() => { dragOver.current = col.key }}
                >
                  <span className="flex items-center gap-0.5">
                    {/* drag grip */}
                    <span className="text-zinc-700 mr-1 cursor-grab opacity-0 hover:opacity-100 transition-opacity">
                      <svg className="w-2 h-3 inline" viewBox="0 0 4 8" fill="currentColor">
                        <circle cx="1" cy="1.5" r="0.8"/><circle cx="3" cy="1.5" r="0.8"/>
                        <circle cx="1" cy="4" r="0.8"/><circle cx="3" cy="4" r="0.8"/>
                        <circle cx="1" cy="6.5" r="0.8"/><circle cx="3" cy="6.5" r="0.8"/>
                      </svg>
                    </span>
                    {col.label}
                    {col.sortable !== false && (
                      <SortIcon dir={sortKey === col.key ? sortDir : null} />
                    )}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {pageData.length === 0 ? (
              <tr>
                <td colSpan={visibleColumns.length} className="text-center py-8 text-xs text-zinc-600">
                  {search ? `No results for "${search}"` : emptyMessage}
                </td>
              </tr>
            ) : (
              pageData.map((row, i) => (
                <tr
                  key={rowKey(row)}
                  onClick={() => onRowClick?.(row)}
                  className={`
                    border-b border-zinc-800/60
                    ${i % 2 === 0 ? "bg-zinc-900/40" : "bg-zinc-900/20"}
                    ${onRowClick ? "cursor-pointer hover:bg-zinc-800/60" : ""}
                    transition-colors duration-75
                    ${rowClassName ? rowClassName(row) : ""}
                  `}
                >
                  {visibleColumns.map(col => {
                    const rawVal = col.getValue ? col.getValue(row) : (row as any)[col.key]
                    return (
                      <td key={col.key} className={`${cellPad} ${fontSize} text-zinc-300 border-r border-zinc-800/40 whitespace-nowrap`}>
                        {col.render ? col.render(row, rawVal) : (rawVal ?? "—")}
                      </td>
                    )
                  })}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* ── Pagination ── */}
      {showPagination && totalPages > 1 && (
        <div className="flex items-center justify-between px-2 py-1 border-t border-zinc-800 flex-shrink-0">
          <div className="flex items-center gap-1">
            <button
              onClick={() => setPage(1)}
              disabled={page === 1}
              className="px-1.5 py-0.5 text-[10px] rounded text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800 disabled:opacity-30 transition-colors"
            >«</button>
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-1.5 py-0.5 text-[10px] rounded text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800 disabled:opacity-30 transition-colors"
            >‹</button>
            <span className="text-[10px] text-zinc-500 px-2">
              {page} / {totalPages}
            </span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-1.5 py-0.5 text-[10px] rounded text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800 disabled:opacity-30 transition-colors"
            >›</button>
            <button
              onClick={() => setPage(totalPages)}
              disabled={page === totalPages}
              className="px-1.5 py-0.5 text-[10px] rounded text-zinc-500 hover:text-zinc-200 hover:bg-zinc-800 disabled:opacity-30 transition-colors"
            >»</button>
          </div>
          <select
            value={pageSize}
            onChange={e => { tableState.setPageSize(Number(e.target.value)); setPage(1) }}
            className="text-[10px] bg-zinc-800 border border-zinc-700 rounded px-1 py-0.5 text-zinc-400"
          >
            {[25, 50, 100, 200].map(n => <option key={n} value={n}>{n}/page</option>)}
          </select>
        </div>
      )}
    </div>
  )
}
