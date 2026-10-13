import { NewspaperIcon } from 'lucide-react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useShellRail } from '@/layout/use-shell-rail'

import { FeedRail } from './components/feed-rail'
import { PostCard } from './components/post-card'
import { PostComposer } from './components/post-composer'
import { useFeedPage } from './hooks/use-feed-page'

export function FeedPage() {
  const { t } = useTranslation('feed')
  const feed = useFeedPage()

  useShellRail(useMemo(() => <FeedRail />, []))

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        className="mb-2"
        actions={
          <Tabs value={feed.scope} onValueChange={(value) => feed.changeScope(value as 'all' | 'following')}>
            <TabsList>
              <TabsTrigger value="all">{t('scope.all')}</TabsTrigger>
              <TabsTrigger value="following">{t('scope.following')}</TabsTrigger>
            </TabsList>
          </Tabs>
        }
      />

      <PostComposer onPublish={feed.publish} />

      {feed.loading ? (
        <AppSkeleton preset="card" count={3} />
      ) : feed.items.length === 0 ? (
        <AppEmptyState icon={NewspaperIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <div className="flex flex-col gap-4">
          {feed.items.map((post) => (
            <PostCard key={post.id} post={post} onLike={(id) => void feed.like(id)} />
          ))}
        </div>
      )}

      <AppPagination page={feed.page} totalPages={feed.totalPages} totalCount={feed.totalCount} onChange={feed.setPage} mode="loadMore" loading={feed.loading} />
    </div>
  )
}
