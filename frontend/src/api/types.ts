import type { CityDto } from '@/pages/perfil/city'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { SocialLinksDto } from '@/pages/perfil/services/perfil-service'

export type ApiEnvelope<T> = {
  isSuccess: boolean
  code: string
  message: string
  data: T | null
}

export class ApiError extends Error {
  readonly code: string
  readonly backendMessage: string

  constructor(code: string, backendMessage = '') {
    super(code)
    this.name = 'ApiError'
    this.code = code
    this.backendMessage = backendMessage
  }
}

export type PagedRequest = {
  page: number
  pageSize: number
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

export function emptyPage<T>(pageSize: number): PagedResult<T> {
  return { items: [], page: 1, pageSize, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false }
}

export type TokensDto = {
  token: string
}

export type LoginResponseDto = {
  tokens: TokensDto | null
  isNeededChangePassword: boolean
}

export type ProfileDto = {
  id: number
  name: string
  nickname: string
  email: string
  role: string | null
  isNeededChangePassword: boolean
  avatarUrl: string | null
  bannerUrl: string | null
  bannerPosition: number
  emailOnBandInvite: boolean
  hasApproxLocation: boolean
  bio: string | null
  city: CityDto | null
  cityName: string | null
  mainInstrument: string | null
  genres: MusicGenre[]
  socialLinks: SocialLinksDto
  createdAt: string
}

export type RegistrationPendingDto = {
  userId: number
  email: string
}

export type ConfirmEmailResultDto = {
  alreadyConfirmed: boolean
}

export const ExternalLoginOutcome = {
  signedIn: 1,
  signupRequired: 2,
  linkRequired: 3,
} as const

export type ExternalLoginOutcome = (typeof ExternalLoginOutcome)[keyof typeof ExternalLoginOutcome]

export type ExternalLoginResponseDto = {
  outcome: ExternalLoginOutcome
  tokens: TokensDto | null
  ticket: string | null
  name: string | null
  email: string | null
  suggestedNickname: string | null
  hasPassword: boolean
}

export type SignInMethodsDto = {
  hasPassword: boolean
  emailConfirmed: boolean
  providers: number[]
}
