import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'

import { connectNotificationsHub } from '@/api/notifications-hub'
import { usePolling } from '@/hooks/use-polling'
import { notificationKinds, notificationPlainText } from '@/pages/notificacoes/notification-message'
import { getNotifications, getUnreadCount, markRead } from '@/pages/notificacoes/services/notificacoes-service'

import { NotificationsContext } from './notifications-context'

const fallbackPollingMs = 60_000
const reconnectDelayMs = 30_000

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation('notificacoes')
  const navigate = useNavigate()
  const [unreadCount, setUnreadCount] = useState(0)
  const [version, setVersion] = useState(0)
  const [connected, setConnected] = useState(false)
  const [attempt, setAttempt] = useState(0)
  const countRef = useRef<number | null>(null)
  const [panelOpen, setPanelOpen] = useState(false)
  const panelOpenRef = useRef(panelOpen)

  useEffect(() => {
    panelOpenRef.current = panelOpen
  }, [panelOpen])

  const announce = useCallback(async () => {
    if (panelOpenRef.current) return

    const page = await getNotifications({ page: 1, pageSize: 1, unreadOnly: true }, { toastError: false }).catch(() => null)
    const latest = page?.items[0]
    if (!latest) return

    const link = notificationKinds[latest.kind].link(latest)

    toast(notificationPlainText(latest, t), {
      duration: 6000,
      action: {
        label: t('toast.view'),
        onClick: () => {
          void markRead(latest.id).catch(() => undefined)
          if (link) navigate(link)
          else setPanelOpen(true)
        },
      },
    })
  }, [navigate, t])

  const announceRef = useRef(announce)

  useEffect(() => {
    announceRef.current = announce
  }, [announce])

  const apply = useCallback((next: number) => {
    const previous = countRef.current
    countRef.current = next
    setUnreadCount(next)

    if (previous !== null && next !== previous) setVersion((v) => v + 1)
    if (previous !== null && next > previous) void announceRef.current()
  }, [])

  const refresh = useCallback(async () => {
    const next = await getUnreadCount().catch(() => null)
    if (next !== null) apply(next)
  }, [apply])

  useEffect(() => {
    void refresh()
  }, [refresh])

  useEffect(() => {
    const disconnect = connectNotificationsHub({
      onUnread: apply,
      onStateChange: (state) => {
        setConnected(state === 'connected')
        if (state === 'connected') void refresh()
      },
    })
    return disconnect
  }, [apply, refresh, attempt])

  useEffect(() => {
    if (connected) return
    const timer = window.setTimeout(() => setAttempt((a) => a + 1), reconnectDelayMs)
    return () => window.clearTimeout(timer)
  }, [connected, attempt])

  usePolling(refresh, connected ? 0 : fallbackPollingMs)

  const value = useMemo(() => ({ unreadCount, version, refresh, panelOpen, setPanelOpen }), [unreadCount, version, refresh, panelOpen])

  return <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>
}
