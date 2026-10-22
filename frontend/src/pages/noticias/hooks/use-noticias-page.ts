import { useEffect, useState } from 'react'

import { defaultPageSize } from '@/components/app-pagination'

import { getNews } from '../services/noticias-service'
import type { NewsArticleDto } from '../services/noticias-service'

const todayPageSize = 50

export function useNoticiasPage() {
  const [today, setToday] = useState<NewsArticleDto[]>([])
  const [todayLoading, setTodayLoading] = useState(true)
  const [earlier, setEarlier] = useState<NewsArticleDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const [earlierLoading, setEarlierLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    getNews({ scope: 'today', page: 1, pageSize: todayPageSize })
      .then((data) => {
        if (!cancelled) setToday(data.items)
      })
      .catch(() => {
        if (!cancelled) setToday([])
      })
      .finally(() => {
        if (!cancelled) setTodayLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    getNews({ scope: 'earlier', page, pageSize: defaultPageSize })
      .then((data) => {
        if (cancelled) return
        setEarlier((previous) => {
          if (page === 1) return data.items
          const known = new Set(previous.map((item) => item.id))
          return [...previous, ...data.items.filter((item) => !known.has(item.id))]
        })
        setTotalPages(data.totalPages)
        setTotalCount(data.totalCount)
      })
      .catch(() => {
        if (!cancelled && page === 1) setEarlier([])
      })
      .finally(() => {
        if (!cancelled) setEarlierLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [page])

  const loadPage = (next: number) => {
    setEarlierLoading(true)
    setPage(next)
  }

  return {
    today,
    todayLoading,
    earlier,
    earlierLoading,
    page,
    totalPages,
    totalCount,
    setPage: loadPage,
    loading: todayLoading || (earlierLoading && page === 1),
  }
}
