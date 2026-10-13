import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'

import { AppSkeleton } from '@/components/app-skeleton'

import type { Permission } from './permissions'
import { accountPath, homePath, initialPasswordPath, loginPath, welcomePath } from './routes'
import { useAuth } from './use-auth'
import { hasSeenWelcome } from './welcome-storage'

type RequireAuthProps = {
  children: ReactNode
  permission?: Permission
}

export function RequireAuth({ children, permission }: RequireAuthProps) {
  const { status, access, user } = useAuth()
  const location = useLocation()

  if (status === 'unknown' || status === 'authenticating') {
    return <AppSkeleton preset="page" />
  }

  if (status !== 'authenticated') {
    return <Navigate to={loginPath} replace state={{ from: location.pathname }} />
  }

  if (user?.isNeededChangePassword && location.pathname !== initialPasswordPath) {
    return <Navigate to={initialPasswordPath} replace />
  }

  const welcomeExempt = location.pathname === welcomePath || location.pathname === accountPath

  if (user && !user.isNeededChangePassword && !welcomeExempt && !hasSeenWelcome(user.id)) {
    return <Navigate to={welcomePath} replace />
  }

  if (permission && !(access?.effectivePermissions.includes(permission) ?? false)) {
    return <Navigate to={homePath} replace />
  }

  return <>{children}</>
}
