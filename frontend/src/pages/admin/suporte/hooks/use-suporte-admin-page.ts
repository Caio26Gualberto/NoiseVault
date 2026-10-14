import { useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import { useNotifications } from '@/layout/use-notifications'
import { SupportTicketStatus } from '@/pages/reportar-problema/support-enums'
import type { SupportArea, SupportTag, SupportTicketPriority } from '@/pages/reportar-problema/support-enums'

import { getSummary, getTickets } from '../services/suporte-admin-service'
import type { SupportSort, SupportSummaryDto, SupportTicketAdminDto } from '../services/suporte-admin-service'

export type StatusTab = SupportTicketStatus | 'all'

export function useSuporteAdminPage() {
  const { version: notificationsVersion } = useNotifications()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<StatusTab>(SupportTicketStatus.open)
  const [tag, setTag] = useState<SupportTag | null>(null)
  const [area, setArea] = useState<SupportArea | null>(null)
  const [priority, setPriority] = useState<SupportTicketPriority | null>(null)
  const [sort, setSort] = useState<SupportSort>('activity')
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [result, setResult] = useState<PagedResult<SupportTicketAdminDto>>(() => emptyPage(defaultPageSize))
  const [summary, setSummary] = useState<SupportSummaryDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getTickets({
      page,
      pageSize: defaultPageSize,
      status: status === 'all' ? null : status,
      tag,
      area,
      priority,
      search: debouncedSearch,
      sort,
    })
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
  }, [page, status, tag, area, priority, sort, debouncedSearch, notificationsVersion])

  useEffect(() => {
    let cancelled = false
    getSummary()
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
    tag,
    area,
    priority,
    sort,
    search,
    setPage,
    changeStatus: resetting(setStatus),
    changeTag: resetting(setTag),
    changeArea: resetting(setArea),
    changePriority: resetting(setPriority),
    changeSort: resetting(setSort),
    changeSearch: resetting(setSearch),
  }
}
