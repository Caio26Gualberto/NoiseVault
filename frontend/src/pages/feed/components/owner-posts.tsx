import { useCallback } from 'react'

import type { PagedRequest } from '@/api/types'

import { usePostList } from '../hooks/use-post-list'
import { getBandPosts, getUserPosts } from '../services/feed-service'
import { PostFeed } from './post-feed'

type OwnerPostsProps = {
  owner: { nickname: string } | { bandSlug: string }
  title: string
  emptyTitle: string
  emptyDescription?: string
}

export function OwnerPosts({ owner, title, emptyTitle, emptyDescription }: OwnerPostsProps) {
  const key = 'nickname' in owner ? owner.nickname : owner.bandSlug
  const byBand = 'bandSlug' in owner
  const load = useCallback(
    (query: PagedRequest) => (byBand ? getBandPosts(key, query) : getUserPosts(key, query)),
    [byBand, key],
  )
  const list = usePostList(load)

  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-h3">{title}</h2>
      <PostFeed list={list} emptyTitle={emptyTitle} emptyDescription={emptyDescription} />
    </section>
  )
}
