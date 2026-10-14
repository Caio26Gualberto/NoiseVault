import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'

export const reportProblemPath = '/reportar-problema'

let lastPath: string | null = null

export function lastVisitedPath() {
  return lastPath
}

export function useRememberLocation() {
  const { pathname } = useLocation()

  useEffect(() => {
    if (pathname !== reportProblemPath) lastPath = pathname
  }, [pathname])
}
