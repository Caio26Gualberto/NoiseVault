import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'

import { ApiCodes } from '../api/api-codes'
import { setSessionExpiredHandler } from '../api/client'
import { feedback } from '../api/feedback-sink'
import { decodeJwt } from '../api/jwt'
import { tokenStorage } from '../api/token-storage'
import { ApiError } from '../api/types'
import { AuthContext } from './auth-context'
import type { AuthContextValue, AuthState } from './auth-context'
import * as authService from './auth-service'
import { emptyAccess } from './auth-service'

const initialState: AuthState = { status: 'unknown', user: null, role: null, access: null }

const loggedOutState: AuthState = { status: 'unauthenticated', user: null, role: null, access: null }

function loadAccess() {
  return authService.fetchMyAccess().catch(() => emptyAccess)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(initialState)
  const bootstrapped = useRef(false)

  const clearSession = useCallback(() => {
    tokenStorage.clear()
    setState(loggedOutState)
  }, [])

  useEffect(() => {
    setSessionExpiredHandler(() => {
      feedback.error(ApiCodes.sessionExpired, '')
      clearSession()
    })
  }, [clearSession])

  useEffect(() => {
    if (bootstrapped.current) return
    bootstrapped.current = true

    const token = tokenStorage.readToken()
    const role = (token ? decodeJwt(token)?.role : null) ?? null

    if (!token) {
      clearSession()
      return
    }

    Promise.all([authService.fetchMe(), loadAccess()])
      .then(([user, access]) => setState({ status: 'authenticated', user, role: user.role || role, access }))
      .catch(() => clearSession())
  }, [clearSession])

  const login = useCallback(
    async (email: string, password: string) => {
      setState((current) => ({ ...current, status: 'authenticating' }))

      let tokens
      try {
        const response = await authService.login(email, password)
        tokens = response.tokens
      } catch (error) {
        setState(loggedOutState)
        throw error
      }

      if (!tokens?.token) {
        setState(loggedOutState)
        feedback.error(ApiCodes.unknownError, '')
        throw new ApiError(ApiCodes.unknownError)
      }

      const role = decodeJwt(tokens.token)?.role ?? null

      tokenStorage.saveTokens(tokens.token, tokens.refreshToken)
      tokenStorage.saveEmail(email)

      try {
        const [user, access] = await Promise.all([authService.fetchMe(), loadAccess()])
        setState({ status: 'authenticated', user, role: user.role || role, access })
      } catch (error) {
        clearSession()
        feedback.error(ApiCodes.unknownError, '')
        throw error
      }
    },
    [clearSession],
  )

  const logout = useCallback(async () => {
    await authService.logout().catch(() => null)
    clearSession()
  }, [clearSession])

  const refreshAccess = useCallback(async () => {
    const access = await loadAccess()
    setState((current) => (current.status === 'authenticated' ? { ...current, access } : current))
  }, [])

  const refreshUser = useCallback(async () => {
    const user = await authService.fetchMe().catch(() => null)
    if (user) {
      setState((current) => (current.status === 'authenticated' ? { ...current, user } : current))
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, login, logout, refreshAccess, refreshUser }),
    [state, login, logout, refreshAccess, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
