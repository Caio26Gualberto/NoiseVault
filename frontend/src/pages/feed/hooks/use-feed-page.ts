import { useCallback, useEffect, useState } from 'react'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'

import { createPost, getFeed, toggleLike } from '../services/feed-service'
import type { FeedQuery, PostDto } from '../services/feed-service'

export function useFeedPage() {
  const [scope, setScope] = useState<FeedQuery['scope']>('all')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<PostDto>>(() => emptyPage(defaultPageSize))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getFeed({ page, pageSize: defaultPageSize, scope })
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
  }, [page, scope, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  const changeScope = (value: FeedQuery['scope']) => {
    setScope(value)
    setPage(1)
  }

  const like = async (id: number) => {
    await toggleLike(id)
    refresh()
  }

  const publish = async (content: string) => {
    await createPost(content)
    setPage(1)
    refresh()
  }

  return { ...result, loading, scope, changeScope, setPage, like, publish, refresh }
}
