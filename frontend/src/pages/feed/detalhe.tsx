import { ArrowLeftIcon, Trash2Icon } from 'lucide-react'
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { AppAvatar } from '@/components/app-avatar'
import { AppRichText } from '@/components/app-rich-text'
import { toRichMentions } from '@/pages/feed/mention'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { useShellRail } from '@/layout/use-shell-rail'
import { formatRelative } from '@/lib/format'
import { CommentComposer } from '@/pages/eventos/components/comment-composer'

import { FeedRail } from './components/feed-rail'
import { PostCard } from './components/post-card'
import { usePostDetail } from './hooks/use-post-detail'
import { PostStatus } from './post-status'

export function PostDetalhePage() {
  const { t } = useTranslation('feed')
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const detail = usePostDetail(Number(id))
  const { state, comments } = detail

  useShellRail(useMemo(() => <FeedRail />, []))

  const removeComment = (commentId: number) =>
    confirm({
      title: t('comments.deleteTitle'),
      confirmLabel: t('comments.delete'),
      danger: true,
      onConfirm: () => detail.removeComment(commentId),
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-4">
      <AppButton intent="ghost" size="sm" className="self-start" asChild>
        <Link to={homePath}>
          <ArrowLeftIcon />
          {t('backToFeed')}
        </Link>
      </AppButton>

      {state.status === 'loading' ? (
        <AppSkeleton preset="card" count={1} />
      ) : state.status !== 'found' ? (
        <AppEmptyState title={t('notFound.title')} description={t('notFound.description')} />
      ) : (
        <>
          <PostCard
            post={state.post}
            detail
            onLike={(target) => void detail.like(target)}
            onUpdated={detail.replace}
            onDeleted={() => navigate(homePath)}
          />
          {state.post.status === PostStatus.published ? (
            <AppCard title={t('comments.title', { count: comments.totalCount })}>
              <div className="flex flex-col gap-4">
                <CommentComposer onSubmit={detail.comment} />
                {comments.items.length === 0 ? (
                  <p className="text-sm text-muted-foreground">{t('comments.empty')}</p>
                ) : (
                  <ul className="flex flex-col gap-4">
                    {comments.items.map((comment) => (
                      <li key={comment.id} className="flex gap-3">
                        <Link to={`/perfil/${comment.author.nickname}`} aria-label={comment.author.name} className="shrink-0 self-start rounded-full">
                          <AppAvatar name={comment.author.name} src={comment.author.avatarUrl} size="md" />
                        </Link>
                        <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                          <span className="text-sm">
                            <Link to={`/perfil/${comment.author.nickname}`} className="font-medium hover:underline">
                              {comment.author.name}
                            </Link>{' '}
                            <span className="app-meta">{formatRelative(comment.createdAt)}</span>
                          </span>
                          <AppRichText text={comment.text} mentions={toRichMentions(comment.mentions)} className="text-sm" />
                        </div>
                        {comment.canDelete ? (
                          <AppIconButton label={t('comments.delete')} size="sm" onClick={() => removeComment(comment.id)}>
                            <Trash2Icon />
                          </AppIconButton>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                )}
                <AppPagination
                  page={comments.page}
                  totalPages={comments.totalPages}
                  totalCount={comments.totalCount}
                  onChange={detail.loadMoreComments}
                  mode="loadMore"
                />
              </div>
            </AppCard>
          ) : null}
        </>
      )}
    </div>
  )
}
