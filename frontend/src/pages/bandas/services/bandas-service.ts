import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { ProfileSummaryDto, SocialLinksDto } from '@/pages/perfil/services/perfil-service'

import type { BandMemberRole } from '../band-member-role'
import * as mocks from './mocks'

export type BandMemberDto = {
  user: ProfileSummaryDto
  instrument: string
  role: BandMemberRole
  joinedAt: string
}

export type BandProfileDto = {
  id: number
  name: string
  slug: string
  description: string
  city: AbcCity
  genres: MusicGenre[]
  foundedAt: string | null
  avatarUrl: string | null
  bannerUrl: string | null
  bannerPosition: number
  isVerified: boolean
  socialLinks: SocialLinksDto
  members: BandMemberDto[]
  stats: { posts: number; events: number; followers: number }
}

export const getBandProfile = mocks.getBandProfile
export const getSuggestedBands = mocks.getSuggestedBands
