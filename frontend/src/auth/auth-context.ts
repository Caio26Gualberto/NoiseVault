import { createContext } from 'react'

import type { ProfileDto } from '../api/types'
import type { MyAccess } from './auth-service'

export type AuthStatus = 'unknown' | 'unauthenticated' | 'authenticating' | 'authenticated'

export type AuthState = {
  status: AuthStatus
  user: ProfileDto | null
  role: string | null
  access: MyAccess | null
}

export type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<void>
  acceptToken: (token: string) => Promise<void>
  logout: () => Promise<void>
  refreshAccess: () => Promise<void>
  refreshUser: () => Promise<void>
  replaceUser: (user: ProfileDto) => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
