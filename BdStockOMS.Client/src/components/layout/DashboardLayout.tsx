// @ts-nocheck
import { Outlet } from "react-router-dom"
import { Sidebar } from "./Sidebar"
import { Topbar }  from "./Topbar"

export function DashboardLayout() {
  return (
    <div style={{
      display: 'flex', minHeight: '100vh',
      background: 'var(--t-bg)',
      fontFamily: "'Outfit', sans-serif",
      position: 'relative', overflow: 'hidden',
    }}>
      <Sidebar />
      <div style={{
        flex: 1, display: 'flex', flexDirection: 'column',
        minWidth: 0, overflow: 'hidden', position: 'relative', zIndex: 1,
      }}>
        <Topbar />
        <main style={{ flex: 1, overflow: 'auto', background: 'var(--t-bg)' }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
