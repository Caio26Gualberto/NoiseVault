import { useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { useNotifications } from '@/layout/use-notifications'
import { ArchiveEntryStatus } from '@/pages/mural/archive-status'
import type { ArchiveEntryDto } from '@/pages/mural/services/mural-service'

import { getSubmissionSummary, getSubmissions } from '../services/mural-admin-service'
import type { ArchiveSubmissionSummaryDto } from '../services/mural-admin-service'

export function useMuralAdminPage() {
  const { version: notificationsVersion } = useNotifications()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<ArchiveEntryStatus>(ArchiveEntryStatus.pending)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [result, setResult] = useState<PagedResult<ArchiveEntryDto>>(() => emptyPage(defaultPageSize))
  const [summary, setSummary] = useState<ArchiveSubmissionSummaryDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getSubmissions({ page, pageSize: defaultPageSize, status, search: debouncedSearch })
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
  }, [page, status, debouncedSearch, notificationsVersion])

  useEffect(() => {
    let cancelled = false
    getSubmissionSummary()
      .then((data) => {
        if (!cancelled) setSummary(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [notificationsVersion])

  const resetting =
    <T,>(setter: (value: T) => void) =>
    (value: T) => {
      setter(value)
      setPage(1)
    }

  return {
    ...result,
    loading,
    summary,
    status,
    search,
    setPage,
    changeStatus: resetting(setStatus),
    changeSearch: resetting(setSearch),
  }
}
