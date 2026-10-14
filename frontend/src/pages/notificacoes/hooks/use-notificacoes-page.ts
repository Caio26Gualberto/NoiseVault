import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { useNotifications } from '@/layout/use-notifications'

import { getNotifications } from '../services/notificacoes-service'
import type { NotificationDto } from '../services/notificacoes-service'

const pageSize = 20

export function useNotificacoesPage() {
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
        if (!cancelled) setResult(data)
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

  const refresh = useCallback(() => setReload((v) => v + 1), [])

  return { result, loading, unreadOnly, changeUnreadOnly, setPage, refresh }
}
