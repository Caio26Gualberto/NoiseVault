import { useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { AbcCity } from '@/pages/perfil/cities'

import type { TimelineSelection } from '../components/timeline'
import { getArchive, getArchiveYears } from '../services/mural-service'
import type { ArchiveEntryDto, ArchiveKind, ArchiveSort, ArchiveYearDto } from '../services/mural-service'

const pageSize = 12

export function useMuralPage() {
  const [page, setPage] = useState(1)
  const [selection, setSelection] = useState<TimelineSelection>({ year: null, decade: null })
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [city, setCity] = useState<AbcCity | null>(null)
  const [kind, setKind] = useState<ArchiveKind | null>(null)
  const [sort, setSort] = useState<ArchiveSort>('newest')
  const [years, setYears] = useState<ArchiveYearDto[]>([])
  const [result, setResult] = useState<PagedResult<ArchiveEntryDto>>(() => emptyPage(pageSize))
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    getArchiveYears()
      .then((data) => {
        if (!cancelled) setYears(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getArchive({ page, pageSize, year: selection.year, decade: selection.decade, search: debouncedSearch, city, kind, sort })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => {
        if (!cancelled) setResult(emptyPage(pageSize))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page, selection, debouncedSearch, city, kind, sort])

  const reset = <T,>(setter: (value: T) => void) => (value: T) => {
    setter(value)
    setPage(1)
  }

  return {
    ...result,
    loading,
    years,
    selection,
    search,
    city,
    kind,
    sort,
    setPage,
    changeSelection: reset(setSelection),
    changeSearch: reset(setSearch),
    changeCity: reset(setCity),
    changeKind: reset(setKind),
    changeSort: reset(setSort),
  }
}
