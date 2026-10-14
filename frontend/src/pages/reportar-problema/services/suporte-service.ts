import { apiGet, apiPost, apiPostForm } from '@/api/base-service'
import type { UploadProgress } from '@/api/multipart-upload'
import type { PagedRequest, PagedResult } from '@/api/types'
import { uploadVideo } from '@/pages/feed/services/media-service'
import type { MediaDto } from '@/pages/feed/services/media-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { SupportArea, SupportTag, SupportTicketPriority, SupportTicketStatus } from '../support-enums'

export type SupportTicketDto = {
  id: number
  protocol: string
  title: string
  description: string
  status: SupportTicketStatus
  priority: SupportTicketPriority
  tags: SupportTag[]
  areas: SupportArea[]
  photoCount: number
  videoCount: number
  media: MediaDto[] | null
  messageCount: number
  awaitingStaff: boolean
  createdAt: string
  lastActivityAt: string
  closedAt: string | null
}

export type SupportMessageDto = {
  id: number
  text: string
  fromStaff: boolean
  mine: boolean
  author: ProfileSummaryDto
  createdAt: string
}

export type SupportTicketInputDto = {
  title: string
  description: string
  tags: SupportTag[]
  areas: SupportArea[]
  mediaIds: number[]
  originPath: string | null
  userAgent: string | null
  viewportWidth: number | null
  viewportHeight: number | null
}

export const supportVideoStartPath = '/api/support/media/videos'

export function createTicket(input: SupportTicketInputDto) {
  return apiPost<SupportTicketDto>('/api/support/tickets', input)
}

export function getMyTickets(query: PagedRequest) {
  return apiGet<PagedResult<SupportTicketDto>>('/api/support/tickets', query)
}

export function getMyTicket(id: number) {
  return apiGet<SupportTicketDto>(`/api/support/tickets/${id}`)
}

export function getMyMessages(id: number, query: PagedRequest) {
  return apiGet<PagedResult<SupportMessageDto>>(`/api/support/tickets/${id}/messages`, query)
}

export function replyToMyTicket(id: number, text: string) {
  return apiPost<SupportMessageDto>(`/api/support/tickets/${id}/messages`, { text })
}

export function uploadSupportPhoto(file: File) {
  const form = new FormData()
  form.append('file', file)
  return apiPostForm<MediaDto>('/api/support/media/photos', form)
}

export function uploadSupportVideo(file: File, onProgress: UploadProgress, signal: AbortSignal) {
  return uploadVideo(file, onProgress, signal, supportVideoStartPath)
}
