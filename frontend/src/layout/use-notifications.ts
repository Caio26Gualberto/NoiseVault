import { useContext } from 'react'

import { NotificationsContext } from './notifications-context'
import type { NotificationsContextValue } from './notifications-context'

const fallback: NotificationsContextValue = { unreadCount: 0, version: 0, refresh: async () => {} }

export function useNotifications(): NotificationsContextValue {
  return useContext(NotificationsContext) ?? fallback
}
