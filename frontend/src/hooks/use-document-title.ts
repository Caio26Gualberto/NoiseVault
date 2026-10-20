import { useEffect } from 'react'

const siteName = 'Noise Vault'

export function useDocumentTitle(title: string | null) {
  useEffect(() => {
    if (!title) return
    const previous = document.title
    document.title = `${title} · ${siteName}`
    return () => {
      document.title = previous
    }
  }, [title])
}
