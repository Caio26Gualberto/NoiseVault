import {
  ArrowLeftIcon,
  CalendarIcon,
  CircleAlertIcon,
  EllipsisIcon,
  HeartIcon,
  LoaderCircleIcon,
  MapPinIcon,
  PencilIcon,
  PlayIcon,
  Trash2Icon,
  UsersRoundIcon,
} from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppMediaViewer } from '@/components/app-media-viewer'
import type { ViewerItem } from '@/components/app-media-viewer'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import { formatDuration, formatRelative } from '@/lib/format'
import { CommentComposer } from '@/pages/eventos/components/comment-composer'
import { formatCity } from '@/pages/perfil/city'
import { MediaKind } from '@/pages/feed/media-kind'
import { MediaStatus } from '@/pages/feed/media-status'

import { formatArchiveDate } from './archive-date'
import { AttendeesDialog } from './components/attendees-dialog'
import { useArchiveDetail } from './hooks/use-archive-detail'
import { deleteArchiveEntry } from './services/mural-service'

export function MuralDetalhePage() {
  const { t } = useTranslation('mural')
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const detail = useArchiveDetail(Number(id))
  const [viewerIndex, setViewerIndex] = useState<number | null>(null)
  const [attendeesOpen, setAttendeesOpen] = useState(false)
  const { state, comments } = detail

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-5xl" />
  }

  if (state.status !== 'found') {
    return <AppEmptyState title={t('notFound.title')} description={t('notFound.description')} />
  }

  const { entry } = state
  const where = [entry.venue, formatCity(entry)].filter(Boolean).join(' · ')
  const ready = entry.media.filter((item) => item.media.status === MediaStatus.ready && item.media.url)
  const viewerItems: ViewerItem[] = ready.map((item) => ({
    id: item.media.id,
    kind: item.media.kind === MediaKind.video ? 'video' : 'photo',
    url: item.media.url!,
    posterUrl: item.media.posterUrl,
    caption: item.caption,
  }))

  const remove = () =>
    confirm({
      title: t('manage.deleteTitle'),
      message: t('manage.deleteMessage'),
      confirmLabel: t('manage.delete'),
      danger: true,
      onConfirm: async () => {
        await deleteArchiveEntry(entry.id)
        navigate('/mural', { replace: true })
      },
    })

  const removeComment = (commentId: number) =>
    confirm({
      title: t('comments.deleteTitle'),
      confirmLabel: t('comments.delete'),
      danger: true,
      onConfirm: () => detail.removeComment(commentId),
    })

  const others = Math.max(0, entry.attendanceCount - entry.attendees.length)

  return (
    <div className="app-page-enter mx-auto flex max-w-5xl flex-col gap-6">
      <AppButton intent="ghost" size="sm" className="self-start" asChild>
        <Link to="/mural">
          <ArrowLeftIcon />
          {t('back')}
        </Link>
      </AppButton>

      <header className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-3">
          <div className="flex flex-col gap-2">
            <span className="inline-flex items-center gap-2 font-heading text-sm tracking-widest text-primary uppercase">
              <CalendarIcon className="size-4" />
              {formatArchiveDate(entry)}
            </span>
            <h1 className="app-h1 text-balance">{entry.title}</h1>
            {where ? (
              <p className="inline-flex items-center gap-1.5 text-muted-foreground">
                <MapPinIcon className="size-4" />
                {where}
              </p>
            ) : null}
          </div>
          {entry.canEdit || entry.canDelete ? (
            <DropdownMenu>
              <DropdownMenuTrigger
                aria-label={t('manage.actions')}
                className="flex size-9 shrink-0 items-center justify-center rounded-lg border text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <EllipsisIcon className="size-4" />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {entry.canEdit ? (
                  <DropdownMenuItem asChild>
                    <Link to={`/mural/${entry.id}/editar`}>
                      <PencilIcon />
                      {t('manage.edit')}
                    </Link>
                  </DropdownMenuItem>
                ) : null}
                {entry.canDelete ? (
                  <DropdownMenuItem variant="destructive" onSelect={remove}>
                    <Trash2Icon />
                    {t('manage.delete')}
                  </DropdownMenuItem>
                ) : null}
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </div>

        {entry.bands.length > 0 ? (
          <ul className="flex flex-wrap gap-2">
            {entry.bands.map((band, index) => (
              <li key={`${index}-${band.name}`}>
                {band.band ? (
                  <Link to={`/bandas/${band.band.slug}`} className="flex items-center gap-2 rounded-full border py-1 pr-3 pl-1 text-sm transition-colors hover:bg-accent">
                    <AppAvatar name={band.band.name} src={band.band.avatarUrl} size="sm" verified={band.band.isVerified} />
                    {band.band.name}
                  </Link>
                ) : (
                  <span className="flex items-center rounded-full border border-dashed px-3 py-1.5 text-sm">{band.name}</span>
                )}
              </li>
            ))}
          </ul>
        ) : null}
      </header>

      <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
        {entry.media.map((item) => {
          const viewer = viewerItems.findIndex((candidate) => candidate.id === item.media.id)
          const preview = item.media.kind === MediaKind.video ? item.media.posterUrl : item.media.url
          const status = item.media.status
          return (
            <li key={item.media.id}>
              <button
                type="button"
                disabled={viewer < 0}
                onClick={() => setViewerIndex(viewer)}
                aria-label={item.caption ?? (item.media.kind === MediaKind.video ? t('media.playVideo') : t('media.openPhoto'))}
                className="group relative block aspect-square w-full overflow-hidden rounded-lg bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                {preview && status === MediaStatus.ready ? (
                  <img src={preview} alt="" loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-[1.04]" />
                ) : (
                  <span className="flex size-full flex-col items-center justify-center gap-2 text-xs text-muted-foreground">
                    {status === MediaStatus.failed ? <CircleAlertIcon className="size-6 text-destructive" /> : <LoaderCircleIcon className="size-6 animate-spin" />}
                    {status === MediaStatus.failed ? t('media.failed') : t('media.processing')}
                  </span>
                )}
                {item.media.kind === MediaKind.video && status === MediaStatus.ready ? (
                  <span className="absolute inset-0 flex items-center justify-center bg-scrim/25">
                    <span className="flex size-11 items-center justify-center rounded-full bg-primary text-primary-foreground">
                      <PlayIcon className="ml-0.5 size-5 fill-current" />
                    </span>
                    {item.media.durationSeconds ? (
                      <span className="absolute right-2 bottom-2 rounded bg-scrim/70 px-1.5 py-0.5 text-xs text-on-scrim tabular-nums">
                        {formatDuration(item.media.durationSeconds)}
                      </span>
                    ) : null}
                  </span>
                ) : null}
                {item.caption ? (
                  <span className="absolute inset-x-0 bottom-0 translate-y-full bg-gradient-to-t from-scrim/90 to-transparent px-2 pt-6 pb-2 text-left text-xs text-on-scrim transition-transform duration-200 group-hover:translate-y-0 group-focus-visible:translate-y-0">
                    {item.caption}
                  </span>
                ) : null}
              </button>
            </li>
          )
        })}
      </ul>

      <section className="flex flex-col gap-4 rounded-xl border bg-card p-4 md:p-6">
        <div className="flex flex-wrap items-center gap-3">
          <AppButton
            intent={entry.attendedByMe ? 'primary' : 'neutral'}
            aria-pressed={entry.attendedByMe}
            onClick={() => void detail.toggleAttendance(entry)}
          >
            <UsersRoundIcon />
            {entry.attendedByMe ? t('attendance.attended') : t('attendance.action')}
          </AppButton>
          <AppButton
            intent="ghost"
            aria-pressed={entry.likedByMe}
            onClick={() => void detail.toggleLike(entry)}
            className={cn('tabular-nums', entry.likedByMe && 'text-primary')}
          >
            <HeartIcon className={cn(entry.likedByMe && 'fill-current')} />
            {entry.likeCount}
          </AppButton>
          {entry.attendanceCount > 0 ? (
            <button
              type="button"
              onClick={() => setAttendeesOpen(true)}
              className="flex items-center gap-2 rounded-full py-1 pr-2 text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              <span className="flex -space-x-2">
                {entry.attendees.map((person) => (
                  <AppAvatar key={person.id} name={person.name} src={person.avatarUrl} size="sm" className="ring-2 ring-card" />
                ))}
              </span>
              {others > 0
                ? t('attendance.summaryMore', { names: entry.attendees.slice(0, 2).map((person) => person.name.split(' ')[0]).join(', '), count: others })
                : t('attendance.summary', { names: entry.attendees.map((person) => person.name.split(' ')[0]).join(', '), count: entry.attendanceCount })}
            </button>
          ) : null}
        </div>

        {entry.description ? <p className="whitespace-pre-line text-[15px] leading-relaxed">{entry.description}</p> : null}

        <p className="app-meta">
          {t('publishedBy')}{' '}
          <Link to={`/perfil/${entry.author.nickname}`} className="hover:underline">
            @{entry.author.nickname}
          </Link>{' '}
          · {formatRelative(entry.createdAt)}
        </p>
      </section>

      <AppCard title={t('comments.title', { count: comments.totalCount })}>
        <div className="flex flex-col gap-4">
          <CommentComposer onSubmit={detail.comment} />
          {comments.items.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('comments.empty')}</p>
          ) : (
            <ul className="flex flex-col gap-4">
              {comments.items.map((comment) => (
                <li key={comment.id} className="flex gap-3">
                  <AppAvatar name={comment.author.name} src={comment.author.avatarUrl} size="md" />
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <span className="text-sm">
                      <Link to={`/perfil/${comment.author.nickname}`} className="font-medium hover:underline">
                        {comment.author.name}
                      </Link>{' '}
                      <span className="app-meta">{formatRelative(comment.createdAt)}</span>
                    </span>
                    <p className="whitespace-pre-line text-sm break-words">{comment.text}</p>
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
          <AppPagination page={comments.page} totalPages={comments.totalPages} totalCount={comments.totalCount} onChange={detail.loadMoreComments} mode="loadMore" />
        </div>
      </AppCard>

      <AppMediaViewer items={viewerItems} index={viewerIndex} onIndexChange={setViewerIndex} />
      <AttendeesDialog entryId={entry.id} open={attendeesOpen} onOpenChange={setAttendeesOpen} />
    </div>
  )
}
