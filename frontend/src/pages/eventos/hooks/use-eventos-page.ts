import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'

import { getEvents, toggleInterest } from '../services/eventos-service'
import type { EventDto } from '../services/eventos-service'

export function useEventosPage() {
  const [page, setPage] = useState(1)
  const [genre, setGenre] = useState<MusicGenre | null>(null)
  const [city, setCity] = useState<AbcCity | null>(null)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [result, setResult] = useState<PagedResult<EventDto>>(() => emptyPage(defaultPageSize))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getEvents({ page, pageSize: defaultPageSize, genre, city, search: debouncedSearch })
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
  }, [page, genre, city, debouncedSearch, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const changeGenre = (value: MusicGenre | null) => {
    setGenre(value)
    setPage(1)
  }

  const changeCity = (value: AbcCity | null) => {
    setCity(value)
    setPage(1)
  }

  const changeSearch = (value: string) => {
    setSearch(value)
    setPage(1)
  }

  const toggle = async (id: number) => {
    await toggleInterest(id)
    refresh()
  }

  return { ...result, loading, genre, city, search, setPage, changeGenre, changeCity, changeSearch, toggle, refresh }
}
