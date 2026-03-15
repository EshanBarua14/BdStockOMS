import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';
export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: { '@': path.resolve(__dirname, './src') },
    },
    server: {
        port: 5173,
        headers: {
            'Content-Security-Policy': "default-src 'self' 'unsafe-inline' data: blob: https:; script-src 'self' 'unsafe-eval' 'unsafe-inline' blob: https:; connect-src 'self' https://localhost:* http://localhost:* ws://localhost:*; worker-src blob: 'self';",
        },
        proxy: {
            '/api': { target: 'https://localhost:7219', changeOrigin: true, secure: false },
            '/hubs': { target: 'https://localhost:7219', changeOrigin: true, secure: false, ws: true },
        },
    },
    test: {
        globals: true,
        environment: 'jsdom',
        setupFiles: ['./src/test/setup.ts'],
    },
});
