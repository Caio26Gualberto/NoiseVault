import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { useNotifications } from '@/layout/use-notifications'
import { BandVerificationStatus } from '@/pages/bandas/band-verification-status'
import type { BandVerificationDto } from '@/pages/bandas/services/bandas-service'

import { getVerificationSummary, getVerifications } from '../services/bandas-admin-service'
import type { BandVerificationSummaryDto } from '../services/bandas-admin-service'

export function useBandasAdminPage() {
  const { version: notificationsVersion } = useNotifications()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<BandVerificationStatus>(BandVerificationStatus.pending)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [result, setResult] = useState<PagedResult<BandVerificationDto>>(() => emptyPage(defaultPageSize))
  const [summary, setSummary] = useState<BandVerificationSummaryDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getVerifications({ page, pageSize: defaultPageSize, status, search: debouncedSearch })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => {
        if (!cancelled) setResult(emptyPage(defaultPageSize))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, status, debouncedSearch, notificationsVersion, reloadToken])

  useEffect(() => {
    let cancelled = false
    getVerificationSummary()
      .then((data) => {
        if (!cancelled) setSummary(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [notificationsVersion, reloadToken])

  const resetting =
    <T,>(setter: (value: T) => void) =>
    (value: T) => {
      setter(value)
      setPage(1)
    }

  const refresh = useCallback(() => setReloadToken((token) => token + 1), [])

  return {
    ...result,
    loading,
    summary,
    status,
    search,
    setPage,
    refresh,
    changeStatus: resetting(setStatus),
    changeSearch: resetting(setSearch),
  }
}
