import i18n from '@/i18n'

import { isEnsaioHost } from './frame'

export function applyEnsaioFrame() {
  if (!isEnsaioHost) return

  document.documentElement.classList.add('dark', 'ensaio')
  document.title = i18n.t('ensaio:documentTitle')
  document.querySelector('link[rel="manifest"]')?.setAttribute('href', '/manifest-ensaio.webmanifest')
  document.querySelector('meta[name="apple-mobile-web-app-title"]')?.setAttribute('content', i18n.t('ensaio:title'))
}
