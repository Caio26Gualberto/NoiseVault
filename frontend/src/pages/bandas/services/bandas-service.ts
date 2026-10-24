import { apiDelete, apiGet, apiPatch, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { CityDto } from '@/pages/perfil/city'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { ProfileSummaryDto, SocialLinksDto } from '@/pages/perfil/services/perfil-service'

import type { BandInvitationStatus } from '../band-invitation-status'
import type { BandKind } from '../band-kind'
import type { BandMemberRole } from '../band-member-role'
import type { BandPartnershipDirection, BandPartnershipStatus } from '../band-partnership-status'
import type { BandVerificationStatus } from '../band-verification-status'
import type { ReleaseKind, ReleaseSource } from '../release-kind'

export type BandSummaryDto = {
  id: number
  name: string
  slug: string
  kind?: BandKind
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
  kind?: BandKind
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
  verification: BandVerificationStateDto | null
  postCount: number
  upcomingEventCount: number
  followerCount: number
  followedByMe: boolean
}

export type BandInput = {
  kind: BandKind
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

export function searchBands(search: string, pageSize = 6, kind?: BandKind) {
  return apiGet<PagedResult<BandSummaryDto>>('/api/bands/search', { search, page: 1, pageSize, kind }, { toastError: false })
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

export function followBand(slug: string) {
  return apiPut<BandProfileDto>(`${bandPath(slug)}/follow`, undefined, { toastSuccess: true })
}

export function unfollowBand(slug: string) {
  return apiDelete<BandProfileDto>(`${bandPath(slug)}/follow`, { toastSuccess: true })
}

export function getFollowedBands(query: PagedRequest) {
  return apiGet<PagedResult<BandSummaryDto>>('/api/bands/following', query)
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

export type BandVerificationStateDto = {
  status: BandVerificationStatus
  requestedAt: string
  reviewedAt: string | null
  rejectionReason: string | null
}

export type BandVerificationDto = {
  id: number
  band: BandSummaryDto
  bandSocialLinks: SocialLinksDto
  bandMemberCount: number
  requestedBy: ProfileSummaryDto
  message: string
  contactPhone: string | null
  status: BandVerificationStatus
  createdAt: string
  reviewedBy: ProfileSummaryDto | null
  reviewedAt: string | null
  rejectionReason: string | null
}

export type BandVerificationRequestInput = {
  message: string
  contactPhone: string | null
}

export const bandVerificationMessageMinLength = 20
export const bandVerificationMessageMaxLength = 1000
export const bandVerificationPhoneMaxLength = 30

export function requestBandVerification(slug: string, input: BandVerificationRequestInput) {
  return apiPost<BandVerificationDto>(`${bandPath(slug)}/verification`, input, { toastSuccess: true })
}

export type BandTrackDto = {
  id: number
  title: string
  discNumber: number
  number: number
  durationSeconds: number | null
  spotifyTrackId: string | null
  externalUrl: string | null
  isFeatured: boolean
}

export type BandReleaseDto = {
  id: number
  title: string
  kind: ReleaseKind
  year: number
  releaseDate: string | null
  coverUrl: string | null
  source: ReleaseSource
  spotifyAlbumId: string | null
  externalUrl: string | null
  order: number
  isHidden: boolean
  tracks: BandTrackDto[]
}

export type FeaturedTrackDto = {
  track: BandTrackDto
  releaseId: number
  releaseTitle: string
  coverUrl: string | null
}

export type BandDiscographyDto = {
  syncedAt: string | null
  spotifyArtistId: string | null
  spotifyArtistUrl: string | null
  canEdit: boolean
  canImport: boolean
  featuredTrack: FeaturedTrackDto | null
  releases: BandReleaseDto[]
}

export type BandTrackInput = {
  title: string
  externalUrl: string | null
  durationSeconds: number | null
}

export type BandReleaseInput = {
  title: string
  kind: ReleaseKind
  year: number
  externalUrl: string | null
  tracks: BandTrackInput[]
}

export const releaseTitleMaxLength = 200
export const releaseMinYear = 1950
export const maxTracksPerRelease = 100

const discographyPath = (slug: string) => `${bandPath(slug)}/discography`

export function getBandDiscography(slug: string) {
  return apiGet<BandDiscographyDto>(discographyPath(slug))
}

export function importDiscography(slug: string) {
  return apiPost<BandDiscographyDto>(`${discographyPath(slug)}/import`, undefined, { toastSuccess: true })
}

export function createRelease(slug: string, input: BandReleaseInput) {
  return apiPost<BandDiscographyDto>(`${discographyPath(slug)}/releases`, input, { toastSuccess: true })
}

export function updateRelease(slug: string, releaseId: number, input: BandReleaseInput) {
  return apiPut<BandDiscographyDto>(`${discographyPath(slug)}/releases/${releaseId}`, input, { toastSuccess: true })
}

export function deleteRelease(slug: string, releaseId: number) {
  return apiDelete<BandDiscographyDto>(`${discographyPath(slug)}/releases/${releaseId}`, { toastSuccess: true })
}

export function setReleaseCover(slug: string, releaseId: number, file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<BandDiscographyDto>(`${discographyPath(slug)}/releases/${releaseId}/cover`, form, { toastSuccess: true })
}

export function reorderDiscography(slug: string, releaseIds: number[]) {
  return apiPut<BandDiscographyDto>(`${discographyPath(slug)}/order`, { releaseIds }, { toastSuccess: true })
}

export function setReleaseHidden(slug: string, releaseId: number, hidden: boolean) {
  return apiPatch<BandDiscographyDto>(`${discographyPath(slug)}/releases/${releaseId}/visibility`, { hidden })
}

export function setFeaturedTrack(slug: string, trackId: number | null) {
  return apiPut<BandDiscographyDto>(`${discographyPath(slug)}/featured`, { trackId }, { toastSuccess: true })
}

export type BandPartnershipDto = {
  id: number
  partner: BandSummaryDto
  direction: BandPartnershipDirection
  status: BandPartnershipStatus
  expiresAt: string
  createdAt: string
  respondedAt: string | null
}

export function getBandPartners(slug: string, query: PagedRequest) {
  return apiGet<PagedResult<BandPartnershipDto>>(`${bandPath(slug)}/partnerships`, query)
}

export function getPendingBandPartnerships(slug: string, query: PagedRequest) {
  return apiGet<PagedResult<BandPartnershipDto>>(`${bandPath(slug)}/partnerships/pending`, query)
}

export function inviteBandPartner(slug: string, targetSlug: string) {
  return apiPost<BandPartnershipDto>(`${bandPath(slug)}/partnerships`, { targetSlug }, { toastSuccess: true })
}

export function acceptBandPartnership(id: number) {
  return apiPost<BandPartnershipDto>(`/api/band-partnerships/${id}/accept`, undefined, { toastSuccess: true })
}

export function declineBandPartnership(id: number) {
  return apiPost<BandPartnershipDto>(`/api/band-partnerships/${id}/decline`, undefined, { toastSuccess: true })
}

export function removeBandPartnership(id: number) {
  return apiDelete<null>(`/api/band-partnerships/${id}`, { toastSuccess: true })
}
