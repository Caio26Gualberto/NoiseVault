import { EllipsisIcon, HeartIcon, MessageCircleIcon, PencilIcon, Trash2Icon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppRichText } from '@/components/app-rich-text'
import { toRichMentions } from '@/pages/feed/mention'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppMediaGrid } from '@/components/app-media-grid'
import { AppShareButton } from '@/components/app-share-button'
import type { MediaItem } from '@/components/app-media-grid'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import { formatCompact, formatRelative } from '@/lib/format'

import { MediaKind } from '../media-kind'
import { MediaStatus } from '../media-status'
import { PostStatus } from '../post-status'
import { photoTagging, toAppTags } from '../photo-tags'
import { deletePost, getPost } from '../services/feed-service'
import { ArchiveEmbed } from './archive-embed'
import { EventEmbed } from './event-embed'
import type { PostDto } from '../services/feed-service'
import type { MediaDto } from '../services/media-service'
import { PostEditDialog } from './post-edit-dialog'

type PostCardProps = {
  post: PostDto
  onLike: (post: PostDto) => void
  onUpdated: (post: PostDto) => void
  onDeleted: (id: number) => void
  detail?: boolean
}

function toMediaItem(media: MediaDto, published: boolean): MediaItem {
  return {
    id: media.id,
    kind: media.kind === MediaKind.video ? 'video' : 'photo',
    url: media.url,
    posterUrl: media.posterUrl,
    durationSeconds: media.durationSeconds,
    state: media.status === MediaStatus.ready ? 'ready' : media.status === MediaStatus.failed ? 'failed' : 'processing',
    tags: toAppTags(media),
    taggable: published && media.kind === MediaKind.photo && media.status === MediaStatus.ready,
  }
}

export function PostCard({ post, onLike, onUpdated, onDeleted, detail = false }: PostCardProps) {
  const { t } = useTranslation('feed')
  const confirm = useAppConfirm()
  const [editing, setEditing] = useState(false)
  const published = post.status === PostStatus.published

  const remove = () =>
    confirm({
      title: t('post.deleteTitle'),
      message: t('post.deleteMessage'),
      confirmLabel: t('post.delete'),
      danger: true,
      onConfirm: async () => {
        await deletePost(post.id)
        onDeleted(post.id)
      },
    })

  return (
    <article className="flex flex-col gap-3 rounded-xl border bg-card p-4">
      <header className="flex items-center gap-3">
        {post.band ? (
          <Link to={`/bandas/${post.band.slug}`}>
            <AppAvatar name={post.band.name} src={post.band.avatarUrl} size="lg" verified={post.band.isVerified} />
          </Link>
        ) : (
          <Link to={`/perfil/${post.author.nickname}`}>
            <AppAvatar name={post.author.name} src={post.author.avatarUrl} size="lg" />
          </Link>
        )}
        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex flex-wrap items-baseline gap-x-2">
            {post.band ? (
              <Link to={`/bandas/${post.band.slug}`} className="truncate font-medium hover:underline">
                {post.band.name}
              </Link>
            ) : (
              <>
                <Link to={`/perfil/${post.author.nickname}`} className="truncate font-medium hover:underline">
                  {post.author.name}
                </Link>
                <span className="app-meta">@{post.author.nickname}</span>
              </>
            )}
          </div>
          <div className="app-meta flex flex-wrap items-center gap-x-2">
            {post.band ? (
              <>
                <Link to={`/perfil/${post.author.nickname}`} className="hover:underline">
                  {t('post.by', { nickname: post.author.nickname })}
                </Link>
                <span aria-hidden>·</span>
              </>
            ) : null}
            {post.archive ? (
              <>
                <span>{t('archive.published')}</span>
                <span aria-hidden>·</span>
              </>
            ) : null}
            {post.event ? (
              <>
                <span>{t('event.published')}</span>
                <span aria-hidden>·</span>
              </>
            ) : null}
            <Link to={`/feed/${post.id}`} className="hover:underline">
              <time dateTime={post.createdAt}>{formatRelative(post.createdAt)}</time>
            </Link>
            {post.editedAt ? (
              <>
                <span aria-hidden>·</span>
                <span>{t('post.edited')}</span>
              </>
            ) : null}
          </div>
        </div>
        {post.status === PostStatus.processing ? <AppBadge tone="info">{t('post.processing')}</AppBadge> : null}
        {post.status === PostStatus.failed ? <AppBadge tone="danger">{t('post.failed')}</AppBadge> : null}
        {post.canEdit || post.canDelete ? (
          <DropdownMenu>
            <DropdownMenuTrigger
              aria-label={t('post.actions')}
              className="flex size-8 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <EllipsisIcon className="size-4" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {post.canEdit ? (
                <DropdownMenuItem onSelect={() => setEditing(true)}>
                  <PencilIcon />
                  {t('post.edit')}
                </DropdownMenuItem>
              ) : null}
              {post.canDelete ? (
                <DropdownMenuItem variant="destructive" onSelect={remove}>
                  <Trash2Icon />
                  {t('post.delete')}
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}
      </header>

      {post.content ? (
        <AppRichText text={post.content} mentions={toRichMentions(post.mentions)} className={cn('text-[15px] leading-relaxed', !detail && 'line-clamp-6')} />
      ) : null}

      <AppMediaGrid
        items={post.media.map((media) => toMediaItem(media, post.status === PostStatus.published))}
        tagging={photoTagging(async () => onUpdated(await getPost(post.id)))}
      />

      {post.event ? <EventEmbed event={post.event} /> : null}

      {post.archive ? <ArchiveEmbed entry={post.archive} /> : null}

      {published ? (
        <footer className="flex items-center gap-1 border-t pt-2">
          <AppButton
            intent="ghost"
            size="sm"
            onClick={() => onLike(post)}
            aria-pressed={post.likedByMe}
            aria-label={post.likedByMe ? t('post.unlike') : t('post.like')}
            className={cn('tabular-nums', post.likedByMe && 'text-primary')}
          >
            <HeartIcon className={cn(post.likedByMe && 'fill-current')} />
            {formatCompact(post.likeCount)}
          </AppButton>
          <AppButton intent="ghost" size="sm" className="tabular-nums" asChild>
            <Link to={`/feed/${post.id}`} aria-label={t('post.comments')}>
              <MessageCircleIcon />
              {formatCompact(post.commentCount)}
            </Link>
          </AppButton>
          <div className="ml-auto">
            <AppShareButton path={`/feed/${post.id}`} title={t('shareTitle', { name: post.band?.name ?? post.author.name })} />
          </div>
        </footer>
      ) : (
        <p className="app-meta border-t pt-2">
          {post.status === PostStatus.failed ? t('post.failedHint') : t('post.processingHint')}
        </p>
      )}

      <PostEditDialog post={post} open={editing} onOpenChange={setEditing} onSaved={onUpdated} />
    </article>
  )
}
