import { toast } from 'sonner'

import i18n from '@/i18n'

const updateCheckMs = 60 * 60 * 1000

let updating = false

function announce(worker: ServiceWorker) {
  toast(i18n.t('layout:pwa.updateTitle'), {
    description: i18n.t('layout:pwa.updateDescription'),
    duration: Infinity,
    action: {
      label: i18n.t('layout:pwa.updateAction'),
      onClick: () => {
        updating = true
        worker.postMessage('skip-waiting')
      },
    },
  })
}

export function registerServiceWorker() {
  if (!import.meta.env.PROD || !('serviceWorker' in navigator)) return

  navigator.serviceWorker.addEventListener('controllerchange', () => {
    if (!updating) return
    updating = false
    window.location.reload()
  })

  window.addEventListener('load', () => {
    navigator.serviceWorker
      .register('/sw.js')
      .then((registration) => {
        if (registration.waiting && navigator.serviceWorker.controller) announce(registration.waiting)

        registration.addEventListener('updatefound', () => {
          const installing = registration.installing
          installing?.addEventListener('statechange', () => {
            if (installing.state === 'installed' && navigator.serviceWorker.controller) announce(installing)
          })
        })

        window.setInterval(() => void registration.update().catch(() => undefined), updateCheckMs)
        document.addEventListener('visibilitychange', () => {
          if (document.visibilityState === 'visible') void registration.update().catch(() => undefined)
        })
      })
      .catch(() => undefined)
  })
}
