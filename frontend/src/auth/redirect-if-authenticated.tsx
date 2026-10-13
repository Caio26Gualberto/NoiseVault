import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'

import { AppSkeleton } from '@/components/app-skeleton'

import { homePath } from './routes'
import { useAuth } from './use-auth'

export function RedirectIfAuthenticated({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'unknown') {
    return <AppSkeleton preset="page" />
  }

  if (status === 'authenticated') {
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from ?? homePath} replace />
  }

  return <>{children}</>
}
