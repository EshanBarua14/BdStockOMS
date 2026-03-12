// @ts-nocheck
import { Outlet } from "react-router-dom"
import { Sidebar } from "./Sidebar"
import { Topbar }  from "./Topbar"

export function DashboardLayout() {
  return (
    <div style={{ display: "flex", minHeight: "100vh", background: "#080C14", fontFamily: "'Outfit',sans-serif" }}>
      <Sidebar />
      <div style={{ flex: 1, display: "flex", flexDirection: "column", minWidth: 0, overflow: "hidden" }}>
        <Topbar />
        <main style={{ flex: 1, overflow: "auto" }}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
