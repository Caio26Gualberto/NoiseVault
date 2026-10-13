import type { PagedRequest, PagedResult } from '@/api/types'
import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { EventStatus } from '../event-status'
import * as mocks from './mocks'

export type EventDto = {
  id: number
  title: string
  description: string
  startsAt: string
  endsAt: string | null
  venue: string
  city: AbcCity
  bannerUrl: string | null
  bannerPosition: number
  genres: MusicGenre[]
  ticketsUrl: string | null
  price: number | null
  capacity: number | null
  status: EventStatus
  organizer: ProfileSummaryDto
  bands: BandSummaryDto[]
  interestedCount: number
  interested: boolean
}

export type EventListQuery = PagedRequest & {
  genre: MusicGenre | null
  city: AbcCity | null
  search: string
}

export type EventInputDto = {
  title: string
  description: string
  startsAt: string
  endsAt: string | null
  venue: string
  city: AbcCity
  genres: MusicGenre[]
  ticketsUrl: string | null
  price: number | null
  capacity: number | null
}

export type EventCommentDto = {
  id: number
  author: ProfileSummaryDto
  text: string
  createdAt: string
}

export const getEvents: (query: EventListQuery) => Promise<PagedResult<EventDto>> = mocks.getEvents
export const getEvent = mocks.getEvent
export const getUpcomingEvents = mocks.getUpcomingEvents
export const toggleInterest = mocks.toggleInterest
export const createEvent = mocks.createEvent
export const updateEvent = mocks.updateEvent
export const getEventComments = mocks.getEventComments
export const addEventComment = mocks.addEventComment
