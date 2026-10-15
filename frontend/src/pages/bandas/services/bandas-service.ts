import { apiDelete, apiGet, apiPatch, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { CityDto } from '@/pages/perfil/city'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { ProfileSummaryDto, SocialLinksDto } from '@/pages/perfil/services/perfil-service'

import type { BandInvitationStatus } from '../band-invitation-status'
import type { BandMemberRole } from '../band-member-role'

export type BandSummaryDto = {
  id: number
  name: string
  slug: string
  avatarUrl: string | null
  isVerified: boolean
  genres: MusicGenre[]
}

export type BandMemberDto = {
  user: ProfileSummaryDto
  instrument: string | null
  role: BandMemberRole
  joinedAt: string
}

export type BandProfileDto = {
  id: number
  name: string
  slug: string
  description: string | null
  city: CityDto | null
  cityName: string | null
  genres: MusicGenre[]
  foundedYear: number | null
  avatarUrl: string | null
  bannerUrl: string | null
  bannerPosition: number
  isVerified: boolean
  socialLinks: SocialLinksDto
  members: BandMemberDto[]
  myRole: BandMemberRole | null
  postCount: number
  upcomingEventCount: number
}

export type BandInput = {
  name: string
  description: string | null
  cityId: number | null
  cityName: string | null
  genres: MusicGenre[]
  foundedYear: number | null
  socialLinks: SocialLinksDto
}

export type CreateBandInput = BandInput & { instrument: string | null }

export type BandInvitationDto = {
  id: number
  band: BandSummaryDto
  invitedUser: ProfileSummaryDto
  invitedBy: ProfileSummaryDto
  instrument: string | null
  status: BandInvitationStatus
  expiresAt: string
  createdAt: string
}

const bandPath = (slug: string) => `/api/bands/${encodeURIComponent(slug)}`

export function getBandProfile(slug: string) {
  return apiGet<BandProfileDto>(bandPath(slug), undefined, { toastError: false })
}

export function getSuggestedBands() {
  return apiGet<BandSummaryDto[]>('/api/bands/suggested')
}

export const bandSearchMinLength = 2

export function searchBands(search: string, pageSize = 6) {
  return apiGet<PagedResult<BandSummaryDto>>('/api/bands/search', { search, page: 1, pageSize }, { toastError: false })
}

export function getMyBands() {
  return apiGet<BandSummaryDto[]>('/api/bands/mine')
}

export function getOwnedBands() {
  return apiGet<BandSummaryDto[]>('/api/bands/owned')
}

export function createBand(input: CreateBandInput) {
  return apiPost<BandProfileDto>('/api/bands', input, { toastSuccess: true })
}

export function updateBand(slug: string, input: BandInput) {
  return apiPut<BandProfileDto>(bandPath(slug), input, { toastSuccess: true })
}

export function setBandAvatar(slug: string, file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<BandProfileDto>(`${bandPath(slug)}/avatar`, form, { toastSuccess: true })
}

export function removeBandAvatar(slug: string) {
  return apiDelete<BandProfileDto>(`${bandPath(slug)}/avatar`, { toastSuccess: true })
}

export function setBandBanner(slug: string, file: File, position: number) {
  const form = new FormData()
  form.append('file', file)
  form.append('position', String(position))
  return apiPostForm<BandProfileDto>(`${bandPath(slug)}/banner`, form, { toastSuccess: true })
}

export function setBandBannerPosition(slug: string, position: number) {
  return apiPatch<BandProfileDto>(`${bandPath(slug)}/banner-position`, { position }, { toastSuccess: true })
}

export function removeBandBanner(slug: string) {
  return apiDelete<BandProfileDto>(`${bandPath(slug)}/banner`, { toastSuccess: true })
}

export function removeBandMember(slug: string, userId: number) {
  return apiDelete<BandProfileDto>(`${bandPath(slug)}/members/${userId}`, { toastSuccess: true })
}

export function promoteBandMember(slug: string, userId: number) {
  return apiPost<BandProfileDto>(`${bandPath(slug)}/members/${userId}/promote`, undefined, { toastSuccess: true })
}

export function leaveBand(slug: string) {
  return apiPost<null>(`${bandPath(slug)}/leave`, undefined, { toastSuccess: true })
}

export function getBandInvitations(slug: string, query: PagedRequest) {
  return apiGet<PagedResult<BandInvitationDto>>(`${bandPath(slug)}/invitations`, query)
}

export function inviteBandMember(slug: string, nickname: string, instrument: string | null) {
  return apiPost<BandInvitationDto>(`${bandPath(slug)}/invitations`, { nickname, instrument }, { toastSuccess: true })
}

export function revokeBandInvitation(id: number) {
  return apiDelete<BandInvitationDto>(`/api/band-invitations/${id}`, { toastSuccess: true })
}

export function getMyBandInvitations(query: PagedRequest) {
  return apiGet<PagedResult<BandInvitationDto>>('/api/band-invitations/mine', query)
}

export function acceptBandInvitation(id: number) {
  return apiPost<BandInvitationDto>(`/api/band-invitations/${id}/accept`, undefined, { toastSuccess: true })
}

export function declineBandInvitation(id: number) {
  return apiPost<BandInvitationDto>(`/api/band-invitations/${id}/decline`, undefined, { toastSuccess: true })
}
