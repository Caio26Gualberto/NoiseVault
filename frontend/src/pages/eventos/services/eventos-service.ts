import { apiDelete, apiGet, apiPatch, apiPost, apiPostForm, apiPut } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { EventStatus } from '../event-status'

export type EventLineupItemDto = {
  band: BandSummaryDto | null
  name: string
}

export type EventDto = {
  id: number
  title: string
  description: string | null
  startsAt: string
  endsAt: string | null
  venue: string
  city: AbcCity
  cityName: string | null
  bannerUrl: string | null
  bannerPosition: number
  genres: MusicGenre[]
  ticketsUrl: string | null
  price: number | null
  capacity: number | null
  status: EventStatus
  cancelledAt: string | null
  hasEnded: boolean
  organizer: ProfileSummaryDto
  band: BandSummaryDto | null
  lineup: EventLineupItemDto[]
  interestedCount: number
  interested: boolean
  commentCount: number
  canManage: boolean
  createdAt: string
}

export type EventSummaryDto = {
  id: number
  title: string
  bannerUrl: string | null
  bannerPosition: number
  startsAt: string
  venue: string
  city: AbcCity
  cityName: string | null
  status: EventStatus
  hasEnded: boolean
}

export type EventScope = 'upcoming' | 'past'

export type EventListQuery = PagedRequest & {
  scope: EventScope
  genre: MusicGenre | null
  city: AbcCity | null
  search: string
  interested: boolean
}

export type EventLineupInput = {
  bandId: number | null
  name: string | null
}

export type EventInputDto = {
  title: string
  description: string | null
  startsAt: string
  endsAt: string | null
  venue: string
  city: AbcCity
  cityName: string | null
  genres: MusicGenre[]
  ticketsUrl: string | null
  price: number | null
  capacity: number | null
  lineup: EventLineupInput[]
}

export type CreateEventInputDto = EventInputDto & {
  bandSlug: string | null
}

export type EventCommentDto = {
  id: number
  author: ProfileSummaryDto
  text: string
  createdAt: string
  canDelete: boolean
}

function listParams(query: EventListQuery) {
  return {
    page: query.page,
    pageSize: query.pageSize,
    scope: query.scope,
    genre: query.genre ?? undefined,
    city: query.city ?? undefined,
    search: query.search || undefined,
    interested: query.interested || undefined,
  }
}

export function getEvents(query: EventListQuery): Promise<PagedResult<EventDto>> {
  return apiGet<PagedResult<EventDto>>('/api/events', listParams(query))
}

export function getBandEvents(slug: string, query: EventListQuery): Promise<PagedResult<EventDto>> {
  return apiGet<PagedResult<EventDto>>(`/api/bands/${encodeURIComponent(slug)}/events`, listParams(query))
}

export function getUpcomingEvents(take = 3): Promise<EventDto[]> {
  return apiGet<EventDto[]>('/api/events/upcoming', { take })
}

export function getEvent(id: number): Promise<EventDto> {
  return apiGet<EventDto>(`/api/events/${id}`, undefined, { toastError: false })
}

export function createEvent(input: CreateEventInputDto): Promise<EventDto> {
  return apiPost<EventDto>('/api/events', input, { toastSuccess: true })
}

export function updateEvent(id: number, input: EventInputDto): Promise<EventDto> {
  return apiPut<EventDto>(`/api/events/${id}`, input, { toastSuccess: true })
}

export function cancelEvent(id: number): Promise<EventDto> {
  return apiPost<EventDto>(`/api/events/${id}/cancel`, undefined, { toastSuccess: true })
}

export function deleteEvent(id: number): Promise<void> {
  return apiDelete<void>(`/api/events/${id}`, { toastSuccess: true })
}

export function addInterest(id: number): Promise<EventDto> {
  return apiPut<EventDto>(`/api/events/${id}/interest`)
}

export function removeInterest(id: number): Promise<EventDto> {
  return apiDelete<EventDto>(`/api/events/${id}/interest`)
}

export function uploadEventBanner(id: number, file: File, position: number): Promise<EventDto> {
  const form = new FormData()
  form.append('file', file)
  form.append('position', String(position))
  return apiPostForm<EventDto>(`/api/events/${id}/banner`, form)
}

export function updateEventBannerPosition(id: number, position: number): Promise<EventDto> {
  return apiPatch<EventDto>(`/api/events/${id}/banner-position`, { position })
}

export function removeEventBanner(id: number): Promise<EventDto> {
  return apiDelete<EventDto>(`/api/events/${id}/banner`)
}

export function getEventComments(id: number, query: PagedRequest): Promise<PagedResult<EventCommentDto>> {
  return apiGet<PagedResult<EventCommentDto>>(`/api/events/${id}/comments`, query)
}

export function addEventComment(id: number, text: string): Promise<EventCommentDto> {
  return apiPost<EventCommentDto>(`/api/events/${id}/comments`, { text })
}

export function deleteEventComment(id: number): Promise<void> {
  return apiDelete<void>(`/api/event-comments/${id}`, { toastSuccess: true })
}
