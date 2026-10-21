import { CalendarDaysIcon, MapPinIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppBadge } from '@/components/app-badge'
import { AppCoverImage } from '@/components/app-cover-image'
import { cn } from '@/lib/utils'
import { dayParts, formatTime, formatWeekday } from '@/lib/format'
import { EventStatus } from '@/pages/eventos/event-status'
import { formatCity } from '@/pages/perfil/city'
import type { EventSummaryDto } from '@/pages/eventos/services/eventos-service'

export function EventEmbed({ event, linked = true }: { event: EventSummaryDto; linked?: boolean }) {
  const { t } = useTranslation('feed')
  const { day, month } = dayParts(event.startsAt)
  const cancelled = event.status === EventStatus.cancelled
  const closed = cancelled || event.hasEnded

  const className = 'group flex flex-col overflow-hidden rounded-lg border transition-colors hover:border-primary/60 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none'
  const body = (
    <>
      <div className="relative aspect-[21/9] overflow-hidden bg-muted">
        {event.bannerUrl ? (
          <AppCoverImage src={event.bannerUrl} position={event.bannerPosition} loading="lazy" className={cn(closed && 'grayscale-[60%]')} />
        ) : (
          <span className="flex size-full items-center justify-center text-muted-foreground">
            <CalendarDaysIcon className="size-10" />
          </span>
        )}
        <span className="absolute top-3 left-3 flex flex-col items-center rounded-lg bg-background/95 px-2.5 py-1.5 text-center leading-none">
          <span className="font-heading text-2xl font-bold tabular-nums">{day}</span>
          <span className="text-[11px] font-semibold tracking-wider uppercase text-muted-foreground">{month}</span>
        </span>
        {cancelled ? (
          <AppBadge tone="danger" className="absolute top-3 right-3">{t('event.cancelled')}</AppBadge>
        ) : event.hasEnded ? (
          <AppBadge tone="neutral" className="absolute top-3 right-3">{t('event.ended')}</AppBadge>
        ) : null}
      </div>
      <div className="flex flex-col gap-1 p-3">
        <span className={cn('app-h3 line-clamp-2 group-hover:text-primary', cancelled && 'line-through decoration-2')}>{event.title}</span>
        <span className="app-meta first-letter:uppercase">
          {formatWeekday(event.startsAt)} · {formatTime(event.startsAt)}
        </span>
        <span className="app-meta inline-flex items-center gap-1.5">
          <MapPinIcon className="size-3.5 shrink-0" />
          <span className="truncate">
            {[event.venue, formatCity(event)].filter(Boolean).join(' · ')}
          </span>
        </span>
      </div>
    </>
  )

  return linked ? (
    <Link to={`/eventos/${event.id}`} className={className}>
      {body}
    </Link>
  ) : (
    <div className={className}>{body}</div>
  )
}
