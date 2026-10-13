import type { AbcCity } from '../cities'
import type { MusicGenre } from '../genres'
import * as mocks from './mocks'

export type ProfileSummaryDto = {
  id: number
  name: string
  nickname: string
  avatarUrl: string | null
  isVerified: boolean
}

export type SocialLinksDto = {
  instagram: string | null
  youtube: string | null
  spotify: string | null
}

export type BandSummaryDto = {
  id: number
  name: string
  slug: string
  avatarUrl: string | null
  isVerified: boolean
  genres: MusicGenre[]
}

export type UserProfileDto = ProfileSummaryDto & {
  bio: string | null
  city: AbcCity
  instrument: string | null
  genres: MusicGenre[]
  bannerUrl: string | null
  bannerPosition: number
  socialLinks: SocialLinksDto
  bands: BandSummaryDto[]
  stats: { posts: number; photos: number; videos: number }
  memberSince: string
}

export const getUserProfile = mocks.getUserProfile
export const getProfileSummary = mocks.getProfileSummary
