// @ts-nocheck
// src/components/layout/DashboardLayout.tsx
// Premium OMS Layout — Ambient glow + glass background

import { Outlet } from "react-router-dom"
import { Sidebar } from "./Sidebar"
import { Topbar }  from "./Topbar"

export function DashboardLayout() {
  return (
    <div style={{
      display: 'flex', minHeight: '100vh',
      background: 'var(--oms-bg-base, #080C14)',
      fontFamily: "'Outfit', sans-serif",
      position: 'relative', overflow: 'hidden',
    }}>
      {/* Ambient glow blobs — creates depth */}
      <div style={{
        position: 'fixed', top: -200, right: -200,
        width: 500, height: 500, borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(0,212,170,0.04) 0%, transparent 70%)',
        pointerEvents: 'none', zIndex: 0,
      }} />
      <div style={{
        position: 'fixed', bottom: -150, left: -100,
        width: 400, height: 400, borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(68,138,255,0.03) 0%, transparent 70%)',
        pointerEvents: 'none', zIndex: 0,
      }} />
      <div style={{
        position: 'fixed', top: '40%', left: '50%',
        width: 600, height: 600, borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(124,77,255,0.02) 0%, transparent 70%)',
        pointerEvents: 'none', zIndex: 0,
        transform: 'translate(-50%, -50%)',
      }} />

      <Sidebar />
      <div style={{
        flex: 1, display: 'flex', flexDirection: 'column',
        minWidth: 0, overflow: 'hidden', position: 'relative', zIndex: 1,
      }}>
        <Topbar />
        <main style={{
          flex: 1, overflow: 'auto',
          background: 'linear-gradient(180deg, rgba(8,12,20,0) 0%, rgba(6,10,18,0.5) 100%)',
        }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
