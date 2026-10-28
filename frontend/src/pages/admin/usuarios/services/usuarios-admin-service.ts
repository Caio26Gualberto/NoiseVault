import { apiGet } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { ExternalProvider } from '@/auth/external/providers'

export type AdminUserDto = {
  id: number
  name: string
  nickname: string
  email: string
  isActive: boolean
  createdAt: string
  avatarUrl?: string | null
  cityName?: string | null
  emailConfirmed?: boolean
  hasPassword?: boolean
  providers?: ExternalProvider[]
}

export type UserSignupSummaryDto = {
  today: number
  last7Days: number
  last30Days: number
  total: number
  unconfirmed: number
}

export type AdminUserQuery = PagedRequest & {
  search: string
}

export function getUsers(query: AdminUserQuery): Promise<PagedResult<AdminUserDto>> {
  return apiGet<PagedResult<AdminUserDto>>('/api/admin/users', {
    page: query.page,
    pageSize: query.pageSize,
    search: query.search || undefined,
  })
}

export function getSignupSummary(): Promise<UserSignupSummaryDto> {
  return apiGet<UserSignupSummaryDto>('/api/admin/users/summary')
}
