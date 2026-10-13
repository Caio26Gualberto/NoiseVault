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
  refreshToken: string
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
  createdAt: string
}

export type RegistrationPendingDto = {
  userId: number
  email: string
}

export type ConfirmEmailResultDto = {
  alreadyConfirmed: boolean
}
