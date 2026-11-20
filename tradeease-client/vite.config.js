import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const wwwroot = path.resolve(__dirname, '../Trade Ease/wwwroot/spa')

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  base: '/spa/',
  build: {
    outDir: wwwroot,
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
    },
  },
}))
