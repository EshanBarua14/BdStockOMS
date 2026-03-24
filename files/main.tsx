// src/main.tsx
// Fixed Day 61 audit: startGlobalMarketHub called when auth token is available
// This ensures SignalR market hub connects on app start (not just per-component)

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { startGlobalMarketHub } from './hooks/useSignalR'
import { useAuthStore } from './store/authStore'

// Start SignalR market hub as soon as we have a token
function initSignalR() {
  const state = useAuthStore.getState()
  const token = state.user?.token
  if (token) {
    startGlobalMarketHub(token)
  }
  // Also subscribe to future auth changes (login/logout)
  useAuthStore.subscribe((newState, prevState) => {
    const newToken = newState.user?.token
    const prevToken = prevState.user?.token
    if (newToken && newToken !== prevToken) {
      startGlobalMarketHub(newToken)
    }
  })
}

initSignalR()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
