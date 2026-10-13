import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'

import { AppPageHeader } from '@/components/app-page-header'
import { useShellRail } from '@/layout/use-shell-rail'

import { FeedRail } from './components/feed-rail'
import { PostComposer } from './components/post-composer'
import { PostFeed } from './components/post-feed'
import { usePostList } from './hooks/use-post-list'
import { getFeed } from './services/feed-service'

export function FeedPage() {
  const { t } = useTranslation('feed')
  const feed = usePostList(getFeed)

  useShellRail(useMemo(() => <FeedRail />, []))

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
      <AppPageHeader title={t('title')} description={t('description')} className="mb-2" />

      <PostComposer onPublished={feed.reload} />

      <PostFeed list={feed} emptyTitle={t('empty.title')} emptyDescription={t('empty.description')} />
    </div>
  )
}
