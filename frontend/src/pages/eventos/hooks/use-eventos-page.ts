import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'

import { addInterest, getEvents, removeInterest } from '../services/eventos-service'
import type { EventDto, EventScope } from '../services/eventos-service'

export function useEventosPage() {
  const [page, setPage] = useState(1)
  const [scope, setScope] = useState<EventScope>('upcoming')
  const [interested, setInterested] = useState(false)
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
    getEvents({ page, pageSize: defaultPageSize, scope, interested, genre, city, search: debouncedSearch })
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
  }, [page, scope, interested, genre, city, debouncedSearch, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const changeScope = (value: EventScope) => {
    setScope(value)
    setPage(1)
  }

  const changeInterested = (value: boolean) => {
    setInterested(value)
    setPage(1)
  }

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

  const toggle = async (event: EventDto) => {
    await (event.interested ? removeInterest(event.id) : addInterest(event.id))
    refresh()
  }

  return {
    ...result,
    loading,
    scope,
    interested,
    genre,
    city,
    search,
    setPage,
    changeScope,
    changeInterested,
    changeGenre,
    changeCity,
    changeSearch,
    toggle,
    refresh,
  }
}
