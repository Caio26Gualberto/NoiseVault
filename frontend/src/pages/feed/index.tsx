import { useCallback, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

import type { PagedRequest } from '@/api/types'
import { AppPageHeader } from '@/components/app-page-header'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useShellRail } from '@/layout/use-shell-rail'

import { FeedRail } from './components/feed-rail'
import { PostComposer } from './components/post-composer'
import { PostFeed } from './components/post-feed'
import { usePostList } from './hooks/use-post-list'
import { getFeed, getFollowingFeed } from './services/feed-service'

type FeedTab = 'all' | 'following'

export function FeedPage() {
  const { t } = useTranslation('feed')
  const [tab, setTab] = useState<FeedTab>('all')
  const load = useCallback((query: PagedRequest) => (tab === 'following' ? getFollowingFeed(query) : getFeed(query)), [tab])
  const feed = usePostList(load)

  useShellRail(useMemo(() => <FeedRail />, []))

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
      <AppPageHeader title={t('title')} description={t('description')} className="mb-2" />

      <PostComposer onPublished={feed.reload} />

      <Tabs value={tab} onValueChange={(value) => setTab(value as FeedTab)}>
        <TabsList>
          <TabsTrigger value="all">{t('tabs.all')}</TabsTrigger>
          <TabsTrigger value="following">{t('tabs.following')}</TabsTrigger>
        </TabsList>
      </Tabs>

      <PostFeed
        list={feed}
        emptyTitle={tab === 'following' ? t('emptyFollowing.title') : t('empty.title')}
        emptyDescription={tab === 'following' ? t('emptyFollowing.description') : t('empty.description')}
      />
    </div>
  )
}
