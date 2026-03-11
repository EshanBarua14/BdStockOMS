import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Topbar } from './Topbar'
import { MarketTickerBar } from '@/components/widgets/MarketTickerBar'
import { useThemeStore } from '@/store/themeStore'

export function DashboardLayout() {
  const { tickerEnabled } = useThemeStore()

  return (
    <div style={{
      display:'flex', flexDirection:'column',
      height:'100vh', overflow:'hidden',
      background:'var(--bg-base)',
    }}>
      {/* Market ticker bar (top) */}
      {tickerEnabled && <MarketTickerBar />}

      {/* Body */}
      <div style={{ display:'flex', flex:1, overflow:'hidden' }}>
        <Sidebar />

        <div style={{ display:'flex', flexDirection:'column', flex:1, minWidth:0, overflow:'hidden' }}>
          <Topbar />
          <main style={{
            flex:1, overflowY:'auto',
            padding:24,
            background:'var(--bg-base)',
            transition:'background 400ms',
          }}>
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  )
}
