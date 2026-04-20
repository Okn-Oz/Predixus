import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      // Geliştirme sırasında /api isteklerini .NET backend'e yönlendir
      '/api': {
        target: 'http://localhost:5119',
        changeOrigin: true,
      },
    },
  },
})
