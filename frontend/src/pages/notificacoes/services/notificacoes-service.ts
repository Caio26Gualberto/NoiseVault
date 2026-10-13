import type { PagedRequest, PagedResult } from '@/api/types'

import type { NotificationType } from '../notification-type'
import * as mocks from './mocks'

export type NotificationDto = {
  id: number
  type: NotificationType
  title: string
  message: string
  link: string | null
  imageUrl: string | null
  readAt: string | null
  createdAt: string
}

export const getNotifications: (query: PagedRequest) => Promise<PagedResult<NotificationDto>> = mocks.getNotifications
export const getUnreadCount = mocks.getUnreadCount
export const markRead = mocks.markRead
export const markAllRead = mocks.markAllRead
export const remove = mocks.remove
