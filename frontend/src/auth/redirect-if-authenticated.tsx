import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'

import { AppSkeleton } from '@/components/app-skeleton'
import { ExternalRedirect } from '@/ensaio/external-redirect'
import { returnUrlFrom } from '@/ensaio/frame'

import { homePath } from './routes'
import { useAuth } from './use-auth'

export function RedirectIfAuthenticated({ children }: { children: ReactNode }) {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'unknown') {
    return <AppSkeleton preset="page" />
  }

  if (status === 'authenticated') {
    const external = returnUrlFrom(location.search)
    if (external) return <ExternalRedirect to={external} />
    const from = (location.state as { from?: string } | null)?.from
    return <Navigate to={from ?? homePath} replace />
  }

  return <>{children}</>
}
