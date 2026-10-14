import { readFileSync } from 'node:fs'
import path from 'node:path'
import type { Plugin } from 'vite'

const shellFiles = ['/offline.html', '/manifest.webmanifest', '/favicon.svg', '/icons/icon-192.png', '/icons/icon-512.png', '/icons/apple-touch-icon.png']

function isShellAsset(file: string) {
  if (file.endsWith('.js') || file.endsWith('.css')) return true
  return file.endsWith('.woff2') && /-latin(-ext)?-/.test(file)
}

export function serviceWorker(): Plugin {
  return {
    name: 'noisevault-service-worker',
    apply: 'build',
    generateBundle(_, bundle) {
      const assets = Object.entries(bundle)
        .filter(([file, output]) => file.startsWith('assets/') && isShellAsset(file) && !(output.type === 'chunk' && output.isDynamicEntry))
        .map(([file]) => `/${file}`)
      const template = readFileSync(path.resolve(import.meta.dirname, 'service-worker.js'), 'utf8')
      const source = template
        .replace('__VERSION__', Date.now().toString(36))
        .replace('__PRECACHE__', JSON.stringify([...shellFiles, ...assets]))
      this.emitFile({ type: 'asset', fileName: 'sw.js', source })
    },
  }
}
