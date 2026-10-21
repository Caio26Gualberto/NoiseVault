import { useCallback, useEffect, useState } from 'react'

import type { PagedRequest, PagedResult } from '@/api/types'
import { defaultPageSize } from '@/components/app-pagination'
import { usePolling } from '@/hooks/use-polling'

import { PostStatus } from '../post-status'
import { getPost, likePost, unlikePost } from '../services/feed-service'
import type { PostDto } from '../services/feed-service'

export type PostListLoader = (query: PagedRequest) => Promise<PagedResult<PostDto>>

const processingPollMs = 5000

export function usePostList(load: PostListLoader) {
  const [items, setItems] = useState<PostDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)
  const [loadedWith, setLoadedWith] = useState(() => load)
  const [stale, setStale] = useState(false)

  if (loadedWith !== load) {
    setLoadedWith(() => load)
    setPage(1)
    setStale(true)
    setLoading(true)
  }

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    load({ page, pageSize: defaultPageSize })
      .then((data) => {
        if (cancelled) return
        setItems((previous) => {
          if (page === 1) return data.items
          const known = new Set(previous.map((post) => post.id))
          return [...previous, ...data.items.filter((post) => !known.has(post.id))]
        })
        setTotalPages(data.totalPages)
        setTotalCount(data.totalCount)
        setStale(false)
      })
      .catch(() => {
        if (cancelled || page !== 1) return
        setItems([])
        setStale(false)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [load, page, version])

  const reload = useCallback(() => {
    setPage(1)
    setVersion((value) => value + 1)
  }, [])

  const replace = useCallback((post: PostDto) => {
    setItems((previous) => previous.map((item) => (item.id === post.id ? post : item)))
  }, [])

  const remove = useCallback((id: number) => {
    setItems((previous) => previous.filter((item) => item.id !== id))
    setTotalCount((value) => Math.max(0, value - 1))
  }, [])

  const like = useCallback(
    async (post: PostDto) => {
      const updated = post.likedByMe ? await unlikePost(post.id) : await likePost(post.id)
      replace(updated)
    },
    [replace],
  )

  const processing = items.filter((post) => post.status === PostStatus.processing && post.canEdit)

  usePolling(async () => {
    for (const post of processing) {
      const fresh = await getPost(post.id).catch(() => null)
      if (fresh && fresh.status !== post.status) replace(fresh)
    }
  }, processing.length > 0 ? processingPollMs : 0)

  return { items, page, totalPages, totalCount, loading, stale, setPage, reload, replace, remove, like }
}

export type PostList = ReturnType<typeof usePostList>
