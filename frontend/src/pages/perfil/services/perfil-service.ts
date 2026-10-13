import { apiGet } from '@/api/base-service'
import type { PagedResult } from '@/api/types'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'

import type { AbcCity } from '../cities'
import type { MusicGenre } from '../genres'

export type ProfileSummaryDto = {
  id: number
  name: string
  nickname: string
  avatarUrl: string | null
}

export type SocialLinksDto = {
  instagram: string | null
  youtube: string | null
  spotify: string | null
}

export type UserProfileDto = ProfileSummaryDto & {
  bannerUrl: string | null
  bannerPosition: number
  bio: string | null
  city: AbcCity | null
  mainInstrument: string | null
  genres: MusicGenre[]
  socialLinks: SocialLinksDto
  memberSince: string
  bands: BandSummaryDto[]
  postCount: number
}

export const peopleSearchMinLength = 2

export function getUserProfile(nickname: string) {
  return apiGet<UserProfileDto>(`/api/users/${encodeURIComponent(nickname)}`, undefined, { toastError: false })
}

export function searchPeople(search: string, pageSize = 8) {
  return apiGet<PagedResult<ProfileSummaryDto>>('/api/users/search', { search, page: 1, pageSize }, { toastError: false })
}
