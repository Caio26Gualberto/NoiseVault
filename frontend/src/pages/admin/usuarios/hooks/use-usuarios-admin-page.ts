import { useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'

import { getSignupSummary, getUsers } from '../services/usuarios-admin-service'
import type { AdminUserDto, UserSignupSummaryDto } from '../services/usuarios-admin-service'

export function useUsuariosAdminPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [result, setResult] = useState<PagedResult<AdminUserDto>>(() => emptyPage(defaultPageSize))
  const [summary, setSummary] = useState<UserSignupSummaryDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getUsers({ page, pageSize: defaultPageSize, search: debouncedSearch })
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
  }, [page, debouncedSearch])

  useEffect(() => {
    let cancelled = false
    getSignupSummary()
      .then((data) => {
        if (!cancelled) setSummary(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  const changeSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  return {
    ...result,
    loading,
    summary,
    search,
    setPage,
    changeSearch,
  }
}
