// src/components/ui/SaveConfirmToast.tsx

// @ts-nocheck
import React from "react"
import type { SaveStatus } from "../../hooks/useDashboardPersistence"

interface Props {
  show: boolean
  saveStatus: SaveStatus
  onSave: () => void
}

function fmt(iso: string | null) {
  if (!iso) return ""
  return new Date(iso).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", second: "2-digit" })
}

export function SaveConfirmToast({ show, saveStatus, onSave }: Props) {
  return (
    <>
      {/* Persistent toolbar pill */}
      {saveStatus.dirty ? (
        <button
          onClick={onSave}
          className="flex items-center gap-1.5 px-2.5 py-1 rounded text-xs font-medium
                     bg-amber-500/20 text-amber-400 border border-amber-500/40
                     hover:bg-amber-500/30 transition-all animate-pulse"
          title="Unsaved changes"
        >
          <span className="w-1.5 h-1.5 rounded-full bg-amber-400" />
          Save layout
        </button>
      ) : (
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded text-xs text-zinc-500 border border-zinc-800">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
          {saveStatus.timestamp ? `Saved ${fmt(saveStatus.timestamp)}` : "Saved"}
        </div>
      )}

      {/* Floating confirmation toast (bottom-right) */}
      <div className={`
        fixed bottom-6 right-6 z-[9999] flex items-center gap-3 px-4 py-3
        bg-zinc-900 border border-emerald-500/40 rounded-xl shadow-2xl shadow-black/60
        text-sm transition-all duration-300
        ${show ? "opacity-100 translate-y-0 pointer-events-auto" : "opacity-0 translate-y-4 pointer-events-none"}
      `}>
        <div className="w-6 h-6 rounded-full bg-emerald-500/20 border border-emerald-500/50 flex items-center justify-center flex-shrink-0">
          <svg className="w-3.5 h-3.5 text-emerald-400" viewBox="0 0 12 12" fill="none">
            <path d="M2 6l3 3 5-5" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </div>
        <div>
          <div className="text-zinc-100 font-semibold text-xs">Layout saved</div>
          <div className="text-zinc-500 text-[10px] mt-0.5">{saveStatus.timestamp ? `at ${fmt(saveStatus.timestamp)}` : "All changes persisted"}</div>
        </div>
      </div>
    </>
  )
}
