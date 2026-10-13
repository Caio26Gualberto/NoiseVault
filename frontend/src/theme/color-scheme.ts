import { useCallback, useEffect, useState } from 'react'

export type ColorScheme = 'dark' | 'light' | 'auto'

const STORAGE_KEY = 'app-color-scheme'
const CHANGE_EVENT = 'app-color-scheme-change'
const mediaQuery = '(prefers-color-scheme: dark)'

export function readColorScheme(): ColorScheme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored === 'light' || stored === 'auto' ? stored : 'dark'
  } catch {
    return 'dark'
  }
}

export function resolveIsDark(scheme: ColorScheme): boolean {
  if (scheme === 'auto') return window.matchMedia(mediaQuery).matches
  return scheme === 'dark'
}

export function applyColorScheme(scheme: ColorScheme) {
  document.documentElement.classList.toggle('dark', resolveIsDark(scheme))
}

export function saveColorScheme(scheme: ColorScheme) {
  try {
    localStorage.setItem(STORAGE_KEY, scheme)
  } catch {
    return
  }
  applyColorScheme(scheme)
  window.dispatchEvent(new Event(CHANGE_EVENT))
}

export function useColorScheme() {
  const [scheme, setScheme] = useState<ColorScheme>(readColorScheme)
  const [isDark, setIsDark] = useState(() => resolveIsDark(readColorScheme()))

  useEffect(() => {
    const sync = () => {
      const current = readColorScheme()
      setScheme(current)
      setIsDark(resolveIsDark(current))
      applyColorScheme(current)
    }

    const media = window.matchMedia(mediaQuery)
    window.addEventListener(CHANGE_EVENT, sync)
    media.addEventListener('change', sync)

    return () => {
      window.removeEventListener(CHANGE_EVENT, sync)
      media.removeEventListener('change', sync)
    }
  }, [])

  const change = useCallback((next: ColorScheme) => saveColorScheme(next), [])

  return { scheme, isDark, change }
}
