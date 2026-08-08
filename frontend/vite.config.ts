// Se importa de 'vitest/config' y no de 'vite' porque este archivo tambien configura
// los tests: el defineConfig de vite no conoce la clave `test` y tsc lo rechaza.
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // El front pega a /api y Vite lo redirige al backend, asi no hay CORS en dev.
    proxy: {
      '/api': {
        target: 'http://localhost:5157',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/setupTests.ts',
  },
})
