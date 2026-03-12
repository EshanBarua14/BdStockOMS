import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    headers: {
      'Content-Security-Policy': "script-src 'self' 'unsafe-eval' 'unsafe-inline' blob:; connect-src 'self' http://localhost:5289 ws://localhost:5173 ws://localhost:5289; default-src 'self' 'unsafe-inline' data: blob: https:;",
    },
    proxy: {
      '/api': { target: 'http://localhost:5289', changeOrigin: true, secure: false },
      '/auth': { target: 'http://localhost:5289', changeOrigin: true, secure: false },
      '/hubs': { target: 'http://localhost:5289', changeOrigin: true, secure: false, ws: true },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
