import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

import { serviceWorker } from './pwa/vite-plugin-service-worker.ts'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')

  if (!env.VITE_API_BASE_URL) {
    throw new Error('VITE_API_BASE_URL não definida. Configure a variável no .env local (veja .env.example).')
  }

  return {
    plugins: [react(), tailwindcss(), serviceWorker()],
    resolve: {
      alias: { '@': path.resolve(import.meta.dirname, './src') },
    },
  }
})
