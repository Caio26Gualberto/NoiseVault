import { apiDelete, apiGet, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { MediaDto } from '@/pages/feed/services/media-service'
import type { CityDto } from '@/pages/perfil/city'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

export type ArchiveBandDto = {
  band: BandSummaryDto | null
  name: string
}

export type ArchiveMediaDto = {
  media: MediaDto
  caption: string | null
}

export type ArchiveDate = {
  year: number
  month: number | null
  day: number | null
}

export type ArchiveEntryDto = ArchiveDate & {
  id: number
  title: string
  description: string | null
  venue: string | null
  city: CityDto | null
  cityName: string | null
  author: ProfileSummaryDto
  bands: ArchiveBandDto[]
  cover: MediaDto | null
  media: ArchiveMediaDto[]
  photoCount: number
  videoCount: number
  likeCount: number
  likedByMe: boolean
  attendanceCount: number
  attendedByMe: boolean
  attendees: ProfileSummaryDto[]
  commentCount: number
  canEdit: boolean
  canDelete: boolean
  createdAt: string
}

export type ArchiveSummaryDto = ArchiveDate & {
  id: number
  title: string
  cover: MediaDto | null
  photoCount: number
  videoCount: number
}

export type ArchiveYearDto = {
  year: number
  count: number
}

export type ArchiveKind = 'photo' | 'video'
export type ArchiveSort = 'newest' | 'oldest'

export type ArchiveListQuery = PagedRequest & {
  year: number | null
  decade: number | null
  search: string
  city: CityDto | null
  kind: ArchiveKind | null
  sort: ArchiveSort
}

export type ArchiveEntryInput = ArchiveDate & {
  title: string
  description: string | null
  venue: string | null
  cityId: number | null
  cityName: string | null
  bands: { bandId: number | null; name: string | null }[]
  media: { mediaId: number; caption: string | null }[]
  coverMediaId: number | null
}

export type ArchiveCommentDto = {
  id: number
  author: ProfileSummaryDto
  text: string
  createdAt: string
  canDelete: boolean
}

function listParams(query: ArchiveListQuery) {
  return {
    page: query.page,
    pageSize: query.pageSize,
    year: query.year ?? undefined,
    decade: query.decade ?? undefined,
    search: query.search || undefined,
    cityId: query.city?.id,
    kind: query.kind ?? undefined,
    sort: query.sort,
  }
}

export function getArchive(query: ArchiveListQuery): Promise<PagedResult<ArchiveEntryDto>> {
  return apiGet<PagedResult<ArchiveEntryDto>>('/api/archive', listParams(query))
}

export function getBandArchive(slug: string, query: ArchiveListQuery): Promise<PagedResult<ArchiveEntryDto>> {
  return apiGet<PagedResult<ArchiveEntryDto>>(`/api/bands/${encodeURIComponent(slug)}/archive`, listParams(query))
}

export function getArchiveYears(): Promise<ArchiveYearDto[]> {
  return apiGet<ArchiveYearDto[]>('/api/archive/years')
}

export function getArchiveEntry(id: number): Promise<ArchiveEntryDto> {
  return apiGet<ArchiveEntryDto>(`/api/archive/${id}`, undefined, { toastError: false })
}

export function getArchiveAttendees(id: number, query: PagedRequest): Promise<PagedResult<ProfileSummaryDto>> {
  return apiGet<PagedResult<ProfileSummaryDto>>(`/api/archive/${id}/attendees`, query)
}

export function createArchiveEntry(input: ArchiveEntryInput): Promise<ArchiveEntryDto> {
  return apiPost<ArchiveEntryDto>('/api/admin/archive', input, { toastSuccess: true })
}

export function updateArchiveEntry(id: number, input: ArchiveEntryInput): Promise<ArchiveEntryDto> {
  return apiPut<ArchiveEntryDto>(`/api/admin/archive/${id}`, input, { toastSuccess: true })
}

export function deleteArchiveEntry(id: number): Promise<void> {
  return apiDelete<void>(`/api/admin/archive/${id}`, { toastSuccess: true })
}

export function likeArchive(id: number): Promise<ArchiveEntryDto> {
  return apiPut<ArchiveEntryDto>(`/api/archive/${id}/like`)
}

export function unlikeArchive(id: number): Promise<ArchiveEntryDto> {
  return apiDelete<ArchiveEntryDto>(`/api/archive/${id}/like`)
}

export function attendArchive(id: number): Promise<ArchiveEntryDto> {
  return apiPut<ArchiveEntryDto>(`/api/archive/${id}/attendance`)
}

export function unattendArchive(id: number): Promise<ArchiveEntryDto> {
  return apiDelete<ArchiveEntryDto>(`/api/archive/${id}/attendance`)
}

export function uploadArchivePhoto(file: File): Promise<MediaDto> {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<MediaDto>('/api/admin/archive/media/photos', form)
}

export function getArchiveComments(id: number, query: PagedRequest): Promise<PagedResult<ArchiveCommentDto>> {
  return apiGet<PagedResult<ArchiveCommentDto>>(`/api/archive/${id}/comments`, query)
}

export function addArchiveComment(id: number, text: string): Promise<ArchiveCommentDto> {
  return apiPost<ArchiveCommentDto>(`/api/archive/${id}/comments`, { text })
}

export function deleteArchiveComment(id: number): Promise<void> {
  return apiDelete<void>(`/api/archive-comments/${id}`, { toastSuccess: true })
}
