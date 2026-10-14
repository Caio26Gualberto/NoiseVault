import { apiDelete, apiGet, apiPut } from '@/api/base-service'
import type { RequestOptions } from '@/api/base-service'
import type { PagedRequest, PagedResult } from '@/api/types'
import type { BandInvitationStatus } from '@/pages/bandas/band-invitation-status'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

import type { NotificationKind } from '../notification-kind'

export type NotificationDto = {
  id: number
  kind: NotificationKind
  actor: ProfileSummaryDto | null
  actorCount: number
  post: { id: number; excerpt: string } | null
  event: { id: number; title: string; startsAt: string } | null
  archive: { id: number; title: string } | null
  band: BandSummaryDto | null
  invitation: { id: number; status: BandInvitationStatus; instrument: string | null; expiresAt: string } | null
  readAt: string | null
  lastActivityAt: string
}

export type NotificationsQuery = PagedRequest & { unreadOnly?: boolean }

export function getNotifications(query: NotificationsQuery, options?: RequestOptions) {
  return apiGet<PagedResult<NotificationDto>>('/api/notifications', query, options)
}

export async function getUnreadCount() {
  const data = await apiGet<{ count: number }>('/api/notifications/unread-count', undefined, { toastError: false })
  return data.count
}

export function markRead(id: number) {
  return apiPut<void>(`/api/notifications/${id}/read`)
}

export function markAllRead() {
  return apiPut<void>('/api/notifications/read-all')
}

export function remove(id: number) {
  return apiDelete<void>(`/api/notifications/${id}`)
}
