import { NewspaperIcon } from 'lucide-react'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { cn } from '@/lib/utils'

import type { PostList } from '../hooks/use-post-list'
import { PostCard } from './post-card'

type PostFeedProps = {
  list: PostList
  emptyTitle: string
  emptyDescription?: string
}

export function PostFeed({ list, emptyTitle, emptyDescription }: PostFeedProps) {
  if (list.loading && list.items.length === 0) return <AppSkeleton preset="card" count={3} />

  if (list.items.length === 0) return <AppEmptyState icon={NewspaperIcon} title={emptyTitle} description={emptyDescription} />

  return (
    <div className={cn('flex flex-col gap-4 transition-opacity', list.stale && 'pointer-events-none opacity-50')} aria-busy={list.stale}>
      {list.items.map((post) => (
        <PostCard key={post.id} post={post} onLike={(target) => void list.like(target)} onUpdated={list.replace} onDeleted={list.remove} />
      ))}
      <AppPagination
        page={list.page}
        totalPages={list.totalPages}
        totalCount={list.totalCount}
        onChange={list.setPage}
        mode="infinite"
        loading={list.loading && !list.stale}
      />
    </div>
  )
}
