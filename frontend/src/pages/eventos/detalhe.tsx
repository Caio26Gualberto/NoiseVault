import {
  BanIcon,
  CalendarIcon,
  ClockIcon,
  EllipsisIcon,
  MapPinIcon,
  PencilIcon,
  StarIcon,
  TicketIcon,
  Trash2Icon,
  UsersIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppRichText } from '@/components/app-rich-text'
import { toRichMentions } from '@/pages/feed/mention'
import { AppBadge } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { cn } from '@/lib/utils'
import { formatDateTime, formatPrice, formatRelative, formatTime, formatWeekday } from '@/lib/format'

import { CommentComposer } from './components/comment-composer'
import { EventStatus } from './event-status'
import { formatCity } from '@/pages/perfil/city'
import { useEventDetail } from './hooks/use-event-detail'
import { deleteEvent } from './services/eventos-service'

export function EventoDetalhePage() {
  const { t } = useTranslation('eventos')
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const detail = useEventDetail(Number(id))
  const { state, comments } = detail

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  if (state.status !== 'found') {
    return <AppEmptyState title={t('notFound.title')} description={t('notFound.description')} />
  }

  const { event } = state
  const price = formatPrice(event.price)
  const cancelled = event.status === EventStatus.cancelled
  const closed = cancelled || event.hasEnded

  const facts = [
    { icon: CalendarIcon, label: t('facts.when'), value: formatWeekday(event.startsAt) },
    { icon: ClockIcon, label: t('facts.time'), value: event.endsAt ? `${formatTime(event.startsAt)} – ${formatTime(event.endsAt)}` : formatTime(event.startsAt) },
    { icon: MapPinIcon, label: t('facts.where'), value: `${event.venue} · ${formatCity(event)}` },
    { icon: TicketIcon, label: t('facts.price'), value: price ? t('priceFrom', { price }) : t('free') },
    ...(event.capacity ? [{ icon: UsersIcon, label: t('facts.capacity'), value: t('facts.people', { count: event.capacity }) }] : []),
  ]

  const cancelEvent = () =>
    confirm({
      title: t('manage.cancelTitle'),
      message: t('manage.cancelMessage'),
      confirmLabel: t('manage.cancel'),
      danger: true,
      onConfirm: () => detail.cancel(event),
    })

  const removeEvent = () =>
    confirm({
      title: t('manage.deleteTitle'),
      message: t('manage.deleteMessage'),
      confirmLabel: t('manage.delete'),
      danger: true,
      onConfirm: async () => {
        await deleteEvent(event.id)
        navigate('/eventos', { replace: true })
      },
    })

  const removeComment = (commentId: number) =>
    confirm({
      title: t('comments.deleteTitle'),
      confirmLabel: t('comments.delete'),
      danger: true,
      onConfirm: () => detail.removeComment(commentId),
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      {cancelled ? (
        <div role="status" className="flex items-center gap-3 rounded-xl border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          <BanIcon className="size-5 shrink-0" />
          <span>
            <strong className="font-semibold">{t('status.cancelledBanner')}</strong>
            {event.cancelledAt ? ` ${t('status.cancelledAt', { date: formatDateTime(event.cancelledAt) })}` : null}
          </span>
        </div>
      ) : null}

      <section className="overflow-hidden rounded-xl border bg-card">
        <div className="relative aspect-video w-full bg-muted md:aspect-[21/9]">
          {event.bannerUrl ? (
            <img
              src={event.bannerUrl}
              alt=""
              className={cn('size-full object-cover', closed && 'grayscale-[60%]')}
              style={{ objectPosition: `center ${event.bannerPosition}%` }}
            />
          ) : null}
          <div className="absolute inset-x-0 bottom-0 h-2/3 bg-gradient-to-t from-card via-card/70 to-transparent" />
          <div className="absolute inset-x-0 bottom-0 flex flex-col gap-3 p-4 md:p-6">
            <div className="flex flex-wrap items-center gap-2">
              <AppGenreBadges genres={event.genres} max={4} tone="brand" />
              {event.hasEnded && !cancelled ? <AppBadge tone="neutral">{t('status.ended')}</AppBadge> : null}
            </div>
            <h1 className={cn('app-h1 text-balance', cancelled && 'line-through decoration-4')}>{event.title}</h1>
          </div>
        </div>

        <div className="flex flex-col gap-5 p-4 md:p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            {event.band ? (
              <Link to={`/bandas/${event.band.slug}`} className="flex items-center gap-3">
                <AppAvatar name={event.band.name} src={event.band.avatarUrl} size="md" verified={event.band.isVerified} />
                <span className="flex flex-col">
                  <span className="app-meta">{t('organizedBy')}</span>
                  <span className="text-sm font-medium">{event.band.name}</span>
                  <span className="app-meta">{t('publishedBy', { nickname: event.organizer.nickname })}</span>
                </span>
              </Link>
            ) : (
              <Link to={`/perfil/${event.organizer.nickname}`} className="flex items-center gap-3">
                <AppAvatar name={event.organizer.name} src={event.organizer.avatarUrl} size="md" />
                <span className="flex flex-col">
                  <span className="app-meta">{t('organizedBy')}</span>
                  <span className="text-sm font-medium">{event.organizer.name}</span>
                </span>
              </Link>
            )}
            <div className="flex flex-wrap items-center gap-2">
              {event.ticketsUrl && !closed ? (
                <AppButton intent="secondary" asChild>
                  <a href={event.ticketsUrl} target="_blank" rel="noreferrer">
                    <TicketIcon />
                    {t('tickets')}
                  </a>
                </AppButton>
              ) : null}
              <AppButton
                intent={event.interested ? 'primary' : 'neutral'}
                aria-pressed={event.interested}
                disabled={closed && !event.interested}
                onClick={() => void detail.toggleInterest(event)}
              >
                <StarIcon className={cn(event.interested && 'fill-current')} />
                {t('interested', { count: event.interestedCount })}
              </AppButton>
              {event.canManage ? (
                <DropdownMenu>
                  <DropdownMenuTrigger
                    aria-label={t('manage.actions')}
                    className="flex size-9 items-center justify-center rounded-lg border text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                  >
                    <EllipsisIcon className="size-4" />
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {!cancelled ? (
                      <DropdownMenuItem asChild>
                        <Link to={`/eventos/${event.id}/editar`}>
                          <PencilIcon />
                          {t('edit')}
                        </Link>
                      </DropdownMenuItem>
                    ) : null}
                    {!cancelled ? (
                      <DropdownMenuItem variant="destructive" onSelect={cancelEvent}>
                        <BanIcon />
                        {t('manage.cancel')}
                      </DropdownMenuItem>
                    ) : null}
                    <DropdownMenuItem variant="destructive" onSelect={removeEvent}>
                      <Trash2Icon />
                      {t('manage.delete')}
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              ) : null}
            </div>
          </div>

          <dl className="grid gap-3 sm:grid-cols-2">
            {facts.map((fact) => {
              const Icon = fact.icon
              return (
                <div key={fact.label} className="flex items-start gap-3 rounded-lg border p-3">
                  <Icon className="mt-0.5 size-4 text-primary" />
                  <div className="flex min-w-0 flex-col">
                    <dt className="app-meta">{fact.label}</dt>
                    <dd className="text-sm font-medium first-letter:uppercase">{fact.value}</dd>
                  </div>
                </div>
              )
            })}
          </dl>

          {event.description ? <p className="whitespace-pre-line text-[15px] leading-relaxed">{event.description}</p> : null}

          {event.lineup.length > 0 ? (
            <div className="flex flex-col gap-2">
              <h2 className="app-h3">{t('lineup')}</h2>
              <ol className="flex flex-wrap gap-2">
                {event.lineup.map((item, index) => (
                  <li key={`${index}-${item.name}`}>
                    {item.band ? (
                      <Link
                        to={`/bandas/${item.band.slug}`}
                        className="flex items-center gap-2 rounded-full border py-1 pr-3 pl-1 text-sm transition-colors hover:bg-accent"
                      >
                        <AppAvatar name={item.band.name} src={item.band.avatarUrl} size="sm" verified={item.band.isVerified} />
                        {item.band.name}
                      </Link>
                    ) : (
                      <span className="flex items-center rounded-full border border-dashed px-3 py-1.5 text-sm">{item.name}</span>
                    )}
                  </li>
                ))}
              </ol>
            </div>
          ) : null}
        </div>
      </section>

      <AppCard title={t('comments.title', { count: comments.totalCount })}>
        <div className="flex flex-col gap-4">
          {!cancelled ? <CommentComposer onSubmit={detail.comment} /> : null}
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
    </div>
  )
}
