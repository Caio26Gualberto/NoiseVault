import { apiDelete, apiGet, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { CityDto } from '@/pages/perfil/city'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type {
  DayPeriod,
  Instrument,
  InterestStatus,
  MusicianCommitment,
  MusicianGoal,
  OpeningStatus,
  Reach,
  SkillLevel,
  WeekDay,
} from '../ensaio-status'

export type MusicianInstrumentDto = {
  instrument: Instrument
  otherName: string | null
  level: SkillLevel
}

export type AvailabilitySlotDto = { day: WeekDay; period: DayPeriod }

export type MusicianProfileDto = {
  exists: boolean
  isMine: boolean
  person: ProfileSummaryDto
  photoUrl: string | null
  city: CityDto | null
  cityName: string | null
  isLookingForBand: boolean
  lookingSince: string | null
  goal: MusicianGoal
  commitment: MusicianCommitment
  reach: Reach
  hasGear: boolean
  hasTransport: boolean
  hasRehearsalSpace: boolean
  influences: string | null
  instruments: MusicianInstrumentDto[]
  genres: MusicGenre[]
  availability: AvailabilitySlotDto[]
  sampleLinks: string[]
}

export type MusicianCardDto = {
  person: ProfileSummaryDto
  photoUrl: string | null
  city: CityDto | null
  cityName: string | null
  instrument: MusicianInstrumentDto | null
  genres: MusicGenre[]
  hasSamples: boolean
  lookingSince: string | null
}

export type MusicianInputDto = {
  isLookingForBand: boolean
  goal: MusicianGoal
  commitment: MusicianCommitment
  reach: Reach
  hasGear: boolean
  hasTransport: boolean
  hasRehearsalSpace: boolean
  influences: string | null
  instruments: MusicianInstrumentDto[]
  genres: MusicGenre[]
  availability: AvailabilitySlotDto[]
  sampleLinks: string[]
}

export type EnsaioFilters = {
  instrument: Instrument | null
  city: CityDto | null
  abc: boolean
  genre: MusicGenre | null
  withSamples: boolean
}

export const emptyFilters: EnsaioFilters = { instrument: null, city: null, abc: false, genre: null, withSamples: false }

export type BandOpeningDto = {
  id: number
  band: BandSummaryDto
  city: CityDto | null
  cityName: string | null
  instrument: Instrument
  otherName: string | null
  description: string
  goal: MusicianGoal | null
  status: OpeningStatus
  isExpired: boolean
  expiresAt: string
  createdAt: string
  canManage: boolean
  myInterest: { status: InterestStatus; message: string | null } | null
  interestCount: number | null
}

export type BandOpeningSummaryDto = {
  id: number
  instrument: Instrument
  otherName: string | null
  description: string
  status: OpeningStatus
  isExpired: boolean
}

export type OpeningInterestDto = {
  id: number
  musician: MusicianCardDto
  message: string | null
  status: InterestStatus
  createdAt: string
}

export type BandOpeningInputDto = {
  instrument: Instrument
  otherName: string | null
  description: string
  goal: MusicianGoal | null
}

function filterParams(query: PagedRequest & { filters: EnsaioFilters }, withSamples: boolean) {
  const { filters } = query
  return {
    page: query.page,
    pageSize: query.pageSize,
    instrument: filters.instrument ?? undefined,
    cityId: filters.abc ? undefined : filters.city?.id,
    abc: filters.abc || undefined,
    genre: filters.genre ?? undefined,
    withSamples: withSamples && filters.withSamples ? true : undefined,
  }
}

export function getMusicians(query: PagedRequest & { filters: EnsaioFilters }) {
  return apiGet<PagedResult<MusicianCardDto>>('/api/musicians', filterParams(query, true))
}

export function getMyMusicianProfile() {
  return apiGet<MusicianProfileDto>('/api/musicians/me')
}

export function saveMyMusicianProfile(input: MusicianInputDto) {
  return apiPut<MusicianProfileDto>('/api/musicians/me', input, { toastSuccess: true })
}

export function setLookingForBand(isLookingForBand: boolean) {
  return apiPost<MusicianProfileDto>('/api/musicians/me/looking', { isLookingForBand }, { toastSuccess: true })
}

export function uploadMusicianPhoto(file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<MusicianProfileDto>('/api/musicians/me/photo', form, { toastSuccess: true })
}

export function removeMusicianPhoto() {
  return apiDelete<MusicianProfileDto>('/api/musicians/me/photo', { toastSuccess: true })
}

export function getMusician(nickname: string) {
  return apiGet<MusicianProfileDto>(`/api/musicians/${encodeURIComponent(nickname)}`, undefined, { toastError: false })
}

export function getOpenings(query: PagedRequest & { filters: EnsaioFilters }) {
  return apiGet<PagedResult<BandOpeningDto>>('/api/openings', filterParams(query, false))
}

export function getBandOpenings(slug: string, query: PagedRequest) {
  return apiGet<PagedResult<BandOpeningDto>>(`/api/bands/${encodeURIComponent(slug)}/openings`, query)
}

export function getOpening(id: number) {
  return apiGet<BandOpeningDto>(`/api/openings/${id}`, undefined, { toastError: false })
}

export function createOpening(slug: string, input: BandOpeningInputDto) {
  return apiPost<BandOpeningDto>(`/api/bands/${encodeURIComponent(slug)}/openings`, input, { toastSuccess: true })
}

export function updateOpening(id: number, input: BandOpeningInputDto) {
  return apiPut<BandOpeningDto>(`/api/openings/${id}`, input, { toastSuccess: true })
}

export function closeOpening(id: number, filled: boolean) {
  return apiPost<BandOpeningDto>(`/api/openings/${id}/close`, { filled }, { toastSuccess: true })
}

export function renewOpening(id: number) {
  return apiPost<BandOpeningDto>(`/api/openings/${id}/renew`, undefined, { toastSuccess: true })
}

export function deleteOpening(id: number) {
  return apiDelete<void>(`/api/openings/${id}`, { toastSuccess: true })
}

export function expressInterest(id: number, message: string | null) {
  return apiPost<BandOpeningDto>(`/api/openings/${id}/interest`, { message }, { toastSuccess: true })
}

export function withdrawInterest(id: number) {
  return apiDelete<BandOpeningDto>(`/api/openings/${id}/interest`, { toastSuccess: true })
}

export function getOpeningInterests(id: number, query: PagedRequest) {
  return apiGet<PagedResult<OpeningInterestDto>>(`/api/openings/${id}/interests`, query)
}

export function inviteInterested(id: number, interestId: number, instrument: string) {
  return apiPost<OpeningInterestDto>(`/api/openings/${id}/interests/${interestId}/invite`, { instrument }, { toastSuccess: true })
}

export function dismissInterested(id: number, interestId: number) {
  return apiPost<OpeningInterestDto>(`/api/openings/${id}/interests/${interestId}/dismiss`, undefined, { toastSuccess: true })
}
