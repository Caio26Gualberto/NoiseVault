import { createContext } from 'react'

export type NotificationsContextValue = {
  unreadCount: number
  version: number
  refresh: () => Promise<void>
  panelOpen: boolean
  setPanelOpen: (open: boolean) => void
}

export const NotificationsContext = createContext<NotificationsContextValue | null>(null)
