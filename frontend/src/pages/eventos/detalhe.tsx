import { CalendarIcon, ClockIcon, MapPinIcon, PencilIcon, StarIcon, TicketIcon, UsersIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppSkeleton } from '@/components/app-skeleton'
import { cn } from '@/lib/utils'
import { formatPrice, formatRelative, formatTime, formatWeekday } from '@/lib/format'
import { useAbcCityLabel } from '@/pages/perfil/cities'

import { CommentComposer } from './components/comment-composer'
import { addEventComment, getEvent, getEventComments, toggleInterest } from './services/eventos-service'
import type { EventCommentDto, EventDto } from './services/eventos-service'

export function EventoDetalhePage() {
  const { t } = useTranslation('eventos')
  const { id = '' } = useParams()
  const eventId = Number(id)
  const { user } = useAuth()
  const cityLabel = useAbcCityLabel()
  const [event, setEvent] = useState<EventDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [comments, setComments] = useState<PagedResult<EventCommentDto>>(() => emptyPage(20))
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    getEvent(eventId)
      .then((result) => {
        if (!cancelled) setEvent(result)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [eventId, version])

  useEffect(() => {
    let cancelled = false
    getEventComments(eventId, { page: 1, pageSize: 20 }).then((result) => {
      if (!cancelled) setComments(result)
    })
    return () => {
      cancelled = true
    }
  }, [eventId, version])

  const refresh = useCallback(() => setVersion((v) => v + 1), [])

  if (loading) {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  if (!event) {
    return <AppEmptyState title={t('notFound.title')} description={t('notFound.description')} />
  }

  const price = formatPrice(event.price)
  const isOrganizer = user?.nickname === event.organizer.nickname

  const facts = [
    { icon: CalendarIcon, label: t('facts.when'), value: formatWeekday(event.startsAt) },
    { icon: ClockIcon, label: t('facts.time'), value: event.endsAt ? `${formatTime(event.startsAt)} – ${formatTime(event.endsAt)}` : formatTime(event.startsAt) },
    { icon: MapPinIcon, label: t('facts.where'), value: `${event.venue} · ${cityLabel(event.city)}` },
    { icon: TicketIcon, label: t('facts.price'), value: price ?? t('free') },
    ...(event.capacity ? [{ icon: UsersIcon, label: t('facts.capacity'), value: t('facts.people', { count: event.capacity }) }] : []),
  ]

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      <section className="overflow-hidden rounded-xl border bg-card">
        <div className="relative aspect-video w-full bg-muted md:aspect-[21/9]">
          {event.bannerUrl ? (
            <img src={event.bannerUrl} alt="" className="size-full object-cover" style={{ objectPosition: `center ${event.bannerPosition}%` }} />
          ) : null}
          <div className="absolute inset-x-0 bottom-0 h-2/3 bg-gradient-to-t from-card via-card/70 to-transparent" />
          <div className="absolute inset-x-0 bottom-0 flex flex-col gap-3 p-4 md:p-6">
            <AppGenreBadges genres={event.genres} max={4} tone="brand" />
            <h1 className="app-h1 text-balance">{event.title}</h1>
          </div>
        </div>

        <div className="flex flex-col gap-5 p-4 md:p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <Link to={`/perfil/${event.organizer.nickname}`} className="flex items-center gap-3">
              <AppAvatar name={event.organizer.name} src={event.organizer.avatarUrl} size="md" />
              <span className="flex flex-col">
                <span className="app-meta">{t('organizedBy')}</span>
                <span className="text-sm font-medium">{event.organizer.name}</span>
              </span>
            </Link>
            <div className="flex flex-wrap gap-2">
              {isOrganizer ? (
                <AppButton intent="neutral" asChild>
                  <Link to={`/eventos/${event.id}/editar`}>
                    <PencilIcon />
                    {t('edit')}
                  </Link>
                </AppButton>
              ) : null}
              {event.ticketsUrl ? (
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
                onClick={() => void toggleInterest(event.id).then(refresh)}
              >
                <StarIcon className={cn(event.interested && 'fill-current')} />
                {t('interested', { count: event.interestedCount })}
              </AppButton>
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
                    <dd className="text-sm font-medium capitalize">{fact.value}</dd>
                  </div>
                </div>
              )
            })}
          </dl>

          <p className="whitespace-pre-line text-[15px] leading-relaxed">{event.description}</p>

          {event.bands.length > 0 ? (
            <div className="flex flex-col gap-2">
              <h2 className="app-h3">{t('lineup')}</h2>
              <div className="flex flex-wrap gap-2">
                {event.bands.map((band) => (
                  <Link
                    key={band.id}
                    to={`/bandas/${band.slug}`}
                    className="flex items-center gap-2 rounded-full border py-1 pr-3 pl-1 text-sm transition-colors hover:bg-accent"
                  >
                    <AppAvatar name={band.name} src={band.avatarUrl} size="sm" verified={band.isVerified} />
                    {band.name}
                  </Link>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      </section>

      <AppCard title={t('comments.title', { count: comments.totalCount })}>
        <div className="flex flex-col gap-4">
          <CommentComposer onSubmit={(text) => addEventComment(event.id, text).then(refresh)} />
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
    </div>
  )
}
