import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'

import { ApiCodes } from '../api/api-codes'
import { restoreSessionToken, setSessionExpiredHandler } from '../api/client'
import { feedback } from '../api/feedback-sink'
import { decodeJwt } from '../api/jwt'
import { tokenStorage } from '../api/token-storage'
import { ApiError } from '../api/types'
import type { ProfileDto } from '../api/types'
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

    restoreSessionToken()
      .then(async (token) => {
        if (!token) {
          clearSession()
          return
        }

        const role = decodeJwt(token)?.role ?? null
        const [user, access] = await Promise.all([authService.fetchMe(), loadAccess()])
        setState({ status: 'authenticated', user, role: user.role || role, access })
      })
      .catch(() => clearSession())
  }, [clearSession])

  const openSession = useCallback(
    async (token: string) => {
      const role = decodeJwt(token)?.role ?? null

      tokenStorage.saveToken(token)

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

      await openSession(tokens.token)
    },
    [openSession],
  )

  const acceptToken = useCallback(
    async (token: string) => {
      setState((current) => ({ ...current, status: 'authenticating' }))
      await openSession(token)
    },
    [openSession],
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

  const replaceUser = useCallback((user: ProfileDto) => {
    setState((current) => (current.status === 'authenticated' ? { ...current, user } : current))
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ ...state, login, acceptToken, logout, refreshAccess, refreshUser, replaceUser }),
    [state, login, acceptToken, logout, refreshAccess, refreshUser, replaceUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
