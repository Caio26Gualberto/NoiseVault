import { useSyncExternalStore } from 'react'

type BeforeInstallPromptEvent = Event & {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

export type InstallMode = 'prompt' | 'ios' | null

let deferred: BeforeInstallPromptEvent | null = null
const listeners = new Set<() => void>()

function emit() {
  for (const listener of listeners) listener()
}

function isStandalone() {
  return window.matchMedia('(display-mode: standalone)').matches || (navigator as Navigator & { standalone?: boolean }).standalone === true
}

function isIos() {
  return /iphone|ipad|ipod/i.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
}

export function listenInstallPrompt() {
  window.addEventListener('beforeinstallprompt', (event) => {
    event.preventDefault()
    deferred = event as BeforeInstallPromptEvent
    emit()
  })
  window.addEventListener('appinstalled', () => {
    deferred = null
    emit()
  })
}

function currentMode(): InstallMode {
  if (isStandalone()) return null
  if (deferred) return 'prompt'
  return isIos() ? 'ios' : null
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export async function promptInstall() {
  if (!deferred) return
  const event = deferred
  deferred = null
  emit()
  await event.prompt()
  await event.userChoice.catch(() => undefined)
}

export function useInstallMode(): InstallMode {
  return useSyncExternalStore(subscribe, currentMode, () => null)
}
