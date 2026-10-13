import { HeartIcon, MessageCircleIcon, Share2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppMediaGrid } from '@/components/app-media-grid'
import { cn } from '@/lib/utils'
import { formatCompact, formatRelative } from '@/lib/format'

import type { PostDto } from '../services/feed-service'

type PostCardProps = {
  post: PostDto
  onLike: (id: number) => void
  detail?: boolean
}

export function PostCard({ post, onLike, detail = false }: PostCardProps) {
  const { t } = useTranslation('feed')

  return (
    <article className="flex flex-col gap-3 rounded-xl border bg-card p-4">
      <header className="flex items-center gap-3">
        <Link to={`/perfil/${post.author.nickname}`}>
          <AppAvatar name={post.author.name} src={post.author.avatarUrl} size="lg" />
        </Link>
        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex flex-wrap items-baseline gap-x-2">
            <Link to={`/perfil/${post.author.nickname}`} className="truncate font-medium hover:underline">
              {post.author.name}
            </Link>
            <span className="app-meta">@{post.author.nickname}</span>
          </div>
          <div className="app-meta flex flex-wrap items-center gap-x-2">
            {post.band ? (
              <>
                <Link to={`/bandas/${post.band.slug}`} className="text-primary hover:underline">
                  {post.band.name}
                </Link>
                <span aria-hidden>·</span>
              </>
            ) : null}
            <Link to={`/feed/${post.id}`} className="hover:underline">
              <time dateTime={post.createdAt}>{formatRelative(post.createdAt)}</time>
            </Link>
          </div>
        </div>
      </header>

      <p className={cn('whitespace-pre-line text-[15px] leading-relaxed', !detail && 'line-clamp-6')}>{post.content}</p>

      <AppMediaGrid items={post.media} />

      <footer className="flex items-center gap-1 border-t pt-2">
        <AppButton
          intent="ghost"
          size="sm"
          onClick={() => onLike(post.id)}
          aria-pressed={post.likedByMe}
          className={cn('tabular-nums', post.likedByMe && 'text-primary')}
        >
          <HeartIcon className={cn(post.likedByMe && 'fill-current')} />
          {formatCompact(post.likeCount)}
        </AppButton>
        <AppButton intent="ghost" size="sm" className="tabular-nums" asChild>
          <Link to={`/feed/${post.id}`}>
            <MessageCircleIcon />
            {formatCompact(post.commentCount)}
          </Link>
        </AppButton>
        <div className="ml-auto">
          <AppIconButton label={t('share')} size="sm" disabled>
            <Share2Icon />
          </AppIconButton>
        </div>
      </footer>
    </article>
  )
}
