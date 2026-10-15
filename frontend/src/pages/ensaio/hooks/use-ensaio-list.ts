import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedRequest, PagedResult } from '@/api/types'

import { emptyFilters } from '../services/ensaio-service'
import type { EnsaioFilters } from '../services/ensaio-service'

export const ensaioPageSize = 12

type Fetcher<T> = (query: PagedRequest & { filters: EnsaioFilters }) => Promise<PagedResult<T>>

export function useEnsaioList<T>(fetcher: Fetcher<T>) {
  const [page, setPage] = useState(1)
  const [filters, setFilters] = useState<EnsaioFilters>(emptyFilters)
  const [result, setResult] = useState<PagedResult<T>>(() => emptyPage(ensaioPageSize))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    fetcher({ page, pageSize: ensaioPageSize, filters })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => {
        if (!cancelled) setResult(emptyPage(ensaioPageSize))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [fetcher, page, filters, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const changeFilters = (value: EnsaioFilters) => {
    setFilters(value)
    setPage(1)
  }

  return { ...result, loading, filters, setPage, changeFilters, refresh }
}
