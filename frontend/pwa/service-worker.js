const version = '__VERSION__'
const precache = __PRECACHE__
const shellCache = `noisevault-shell-${version}`
const offlinePage = '/offline.html'

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(shellCache).then((cache) => cache.addAll(precache)))
})

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((key) => key.startsWith('noisevault-shell-') && key !== shellCache).map((key) => caches.delete(key))))
      .then(() => self.clients.claim()),
  )
})

self.addEventListener('message', (event) => {
  if (event.data === 'skip-waiting') self.skipWaiting()
})

self.addEventListener('fetch', (event) => {
  const request = event.request
  if (request.method !== 'GET') return

  const url = new URL(request.url)
  if (url.origin !== self.location.origin) return

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).catch(() => caches.match(offlinePage)))
    return
  }

  if (precache.includes(url.pathname)) {
    event.respondWith(caches.match(url.pathname).then((cached) => cached ?? fetch(request)))
  }
})
