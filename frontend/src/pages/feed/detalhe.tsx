import { ArrowLeftIcon } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { homePath } from '@/auth/routes'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppSkeleton } from '@/components/app-skeleton'
import { useShellRail } from '@/layout/use-shell-rail'
import { formatRelative } from '@/lib/format'
import { CommentComposer } from '@/pages/eventos/components/comment-composer'

import { FeedRail } from './components/feed-rail'
import { PostCard } from './components/post-card'
import { addComment, getComments, getPost, toggleLike } from './services/feed-service'
import type { CommentDto, PostDto } from './services/feed-service'

export function PostDetalhePage() {
  const { t } = useTranslation('feed')
  const { id = '' } = useParams()
  const postId = Number(id)
  const [post, setPost] = useState<PostDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [comments, setComments] = useState<PagedResult<CommentDto>>(() => emptyPage(50))
  const [version, setVersion] = useState(0)

  useShellRail(useMemo(() => <FeedRail />, []))

  useEffect(() => {
    let cancelled = false
    Promise.all([getPost(postId), getComments(postId, { page: 1, pageSize: 50 })])
      .then(([postResult, commentResult]) => {
        if (cancelled) return
        setPost(postResult)
        setComments(commentResult)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [postId, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
      <AppButton intent="ghost" size="sm" className="self-start" asChild>
        <Link to={homePath}>
          <ArrowLeftIcon />
          {t('backToFeed')}
        </Link>
      </AppButton>

      {loading ? (
        <AppSkeleton preset="card" count={1} />
      ) : !post ? (
        <AppEmptyState title={t('notFound.title')} description={t('notFound.description')} />
      ) : (
        <>
          <PostCard post={post} detail onLike={(target) => void toggleLike(target).then(refresh)} />
          <AppCard title={t('comments.title', { count: comments.totalCount })}>
            <div className="flex flex-col gap-4">
              <CommentComposer onSubmit={(text) => addComment(post.id, text).then(refresh)} />
              {comments.items.length === 0 ? (
                <p className="text-sm text-muted-foreground">{t('comments.empty')}</p>
              ) : (
                <ul className="flex flex-col gap-4">
                  {comments.items.map((comment) => (
                    <li key={comment.id} className="flex gap-3">
                      <AppAvatar name={comment.author.name} src={comment.author.avatarUrl} size="md" />
                      <div className="flex min-w-0 flex-col gap-0.5">
                        <span className="text-sm">
                          <Link to={`/perfil/${comment.author.nickname}`} className="font-medium hover:underline">
                            {comment.author.name}
                          </Link>{' '}
                          <span className="app-meta">{formatRelative(comment.createdAt)}</span>
                        </span>
                        <p className="text-sm">{comment.text}</p>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </AppCard>
        </>
      )}
    </div>
  )
}
