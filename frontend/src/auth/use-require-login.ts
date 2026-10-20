import { useCallback } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'

import { loginPath } from './routes'
import { useAuth } from './use-auth'

export function useIsVisitor() {
  const { status } = useAuth()
  return status === 'unauthenticated'
}

export function useRequireLogin() {
  const visitor = useIsVisitor()
  const navigate = useNavigate()
  const location = useLocation()

  const requireLogin = useCallback(
    () => navigate(loginPath, { state: { from: `${location.pathname}${location.search}` } }),
    [location.pathname, location.search, navigate],
  )

  return { visitor, requireLogin }
}
