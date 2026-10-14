import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { useNotifications } from '@/layout/use-notifications'

import { getNotifications } from '../services/notificacoes-service'
import type { NotificationDto } from '../services/notificacoes-service'

const pageSize = 20

export function useNotificationsPanel() {
  const { version } = useNotifications()
  const [page, setPage] = useState(1)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [result, setResult] = useState<PagedResult<NotificationDto>>(() => emptyPage(pageSize))
  const [loading, setLoading] = useState(true)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getNotifications({ page, pageSize, unreadOnly })
      .then((data) => {
        if (cancelled) return
        setResult((previous) => (page === 1 ? data : { ...data, items: [...previous.items, ...data.items] }))
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, unreadOnly, version, reload])

  const changeUnreadOnly = useCallback((value: boolean) => {
    setUnreadOnly(value)
    setPage(1)
  }, [])

  const refresh = useCallback(() => {
    setPage(1)
    setReload((v) => v + 1)
  }, [])

  return { result, loading, unreadOnly, changeUnreadOnly, loadMore: setPage, refresh }
}
