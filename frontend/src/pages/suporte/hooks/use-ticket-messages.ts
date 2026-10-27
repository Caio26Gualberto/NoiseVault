import { useCallback, useEffect, useState } from 'react'

import type { PagedRequest, PagedResult } from '@/api/types'
import type { SupportMessageDto } from '@/pages/ajuda/services/suporte-service'

const messagesPageSize = 100

export function useTicketMessages(
  id: number,
  load: (id: number, query: PagedRequest) => Promise<PagedResult<SupportMessageDto>>,
  send: (id: number, text: string) => Promise<SupportMessageDto>,
) {
  const [items, setItems] = useState<SupportMessageDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    load(id, { page, pageSize: messagesPageSize })
      .then((data) => {
        if (cancelled) return
        setItems((previous) => {
          if (page === 1) return data.items
          const seen = new Set(previous.map((item) => item.id))
          return [...previous, ...data.items.filter((item) => !seen.has(item.id))]
        })
        setTotalPages(data.totalPages)
        setTotalCount(data.totalCount)
      })
      .catch(() => {
        if (!cancelled && page === 1) setItems([])
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [id, load, page, version])

  const reload = useCallback(() => {
    setPage(1)
    setVersion((v) => v + 1)
  }, [])

  const reply = async (text: string) => {
    await send(id, text)
    reload()
  }

  return { items, page, totalPages, totalCount, loading, setPage, reload, reply }
}
