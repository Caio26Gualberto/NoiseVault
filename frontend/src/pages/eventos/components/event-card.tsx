import { MapPinIcon, StarIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { cn } from '@/lib/utils'
import { dayParts, formatPrice, formatTime } from '@/lib/format'

import { EventStatus } from '../event-status'
import { useEventPlace } from '../event-place'
import type { EventDto } from '../services/eventos-service'

type EventCardProps = {
  event: EventDto
  onToggleInterest: (event: EventDto) => void
}

export function EventCard({ event, onToggleInterest }: EventCardProps) {
  const { t } = useTranslation('eventos')
  const place = useEventPlace()
  const { day, month } = dayParts(event.startsAt)
  const price = formatPrice(event.price)
  const cancelled = event.status === EventStatus.cancelled
  const closed = cancelled || event.hasEnded

  return (
    <article className={cn('flex flex-col overflow-hidden rounded-xl border bg-card', closed && 'opacity-80')}>
      <Link to={`/eventos/${event.id}`} className="relative block aspect-video bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
        {event.bannerUrl ? (
          <img src={event.bannerUrl} alt="" loading="lazy" className={cn('size-full object-cover', closed && 'grayscale-[60%]')} style={{ objectPosition: `center ${event.bannerPosition}%` }} />
        ) : null}
        <span className="absolute top-3 left-3 flex flex-col items-center rounded-lg bg-background/95 px-2.5 py-1.5 text-center leading-none">
          <span className="font-heading text-2xl font-bold tabular-nums">{day}</span>
          <span className="text-[11px] font-semibold tracking-wider uppercase text-muted-foreground">{month}</span>
        </span>
        {cancelled ? (
          <AppBadge tone="danger" className="absolute top-3 right-3">{t('status.cancelled')}</AppBadge>
        ) : event.hasEnded ? (
          <AppBadge tone="neutral" className="absolute top-3 right-3">{t('status.ended')}</AppBadge>
        ) : null}
      </Link>

      <div className="flex flex-1 flex-col gap-3 p-4">
        <div className="flex flex-col gap-1">
          <Link to={`/eventos/${event.id}`} className={cn('app-h3 line-clamp-2 hover:text-primary', cancelled && 'line-through decoration-2')}>
            {event.title}
          </Link>
          <p className="app-meta inline-flex items-center gap-1.5">
            <MapPinIcon className="size-3.5 shrink-0" />
            <span className="truncate">
              {event.venue} · {place(event)} · {formatTime(event.startsAt)}
            </span>
          </p>
          {event.lineup.length > 0 ? (
            <p className="app-meta line-clamp-1">{event.lineup.map((item) => item.name).join(' · ')}</p>
          ) : null}
        </div>

        <AppGenreBadges genres={event.genres} max={3} />

        <div className="mt-auto flex items-center justify-between gap-3 border-t pt-3">
          <span className="text-sm font-medium">{price ? t('priceFrom', { price }) : t('free')}</span>
          <AppButton
            intent={event.interested ? 'primary' : 'neutral'}
            size="sm"
            onClick={() => onToggleInterest(event)}
            aria-pressed={event.interested}
            disabled={closed && !event.interested}
          >
            <StarIcon className={cn(event.interested && 'fill-current')} />
            {t('interested', { count: event.interestedCount })}
          </AppButton>
        </div>
      </div>
    </article>
  )
}
