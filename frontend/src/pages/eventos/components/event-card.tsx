import { MapPinIcon, StarIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppButton } from '@/components/app-button'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { cn } from '@/lib/utils'
import { dayParts, formatPrice, formatTime } from '@/lib/format'
import { useAbcCityLabel } from '@/pages/perfil/cities'

import type { EventDto } from '../services/eventos-service'

type EventCardProps = {
  event: EventDto
  onToggleInterest: (id: number) => void
}

export function EventCard({ event, onToggleInterest }: EventCardProps) {
  const { t } = useTranslation('eventos')
  const cityLabel = useAbcCityLabel()
  const { day, month } = dayParts(event.startsAt)
  const price = formatPrice(event.price)

  return (
    <article className="flex flex-col overflow-hidden rounded-xl border bg-card">
      <Link to={`/eventos/${event.id}`} className="relative block aspect-video bg-muted focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
        {event.bannerUrl ? (
          <img src={event.bannerUrl} alt="" loading="lazy" className="size-full object-cover" style={{ objectPosition: `center ${event.bannerPosition}%` }} />
        ) : null}
        <span className="absolute top-3 left-3 flex flex-col items-center rounded-lg bg-background/95 px-2.5 py-1.5 text-center leading-none">
          <span className="font-heading text-2xl font-bold tabular-nums">{day}</span>
          <span className="text-[11px] font-semibold tracking-wider uppercase text-muted-foreground">{month}</span>
        </span>
      </Link>

      <div className="flex flex-1 flex-col gap-3 p-4">
        <div className="flex flex-col gap-1">
          <Link to={`/eventos/${event.id}`} className="app-h3 line-clamp-2 hover:text-primary">
            {event.title}
          </Link>
          <p className="app-meta inline-flex items-center gap-1.5">
            <MapPinIcon className="size-3.5" />
            {event.venue} · {cityLabel(event.city)} · {formatTime(event.startsAt)}
          </p>
        </div>

        <AppGenreBadges genres={event.genres} max={3} />

        <div className="mt-auto flex items-center justify-between gap-3 border-t pt-3">
          <span className="text-sm font-medium">{price ?? t('free')}</span>
          <AppButton
            intent={event.interested ? 'primary' : 'neutral'}
            size="sm"
            onClick={() => onToggleInterest(event.id)}
            aria-pressed={event.interested}
          >
            <StarIcon className={cn(event.interested && 'fill-current')} />
            {t('interested', { count: event.interestedCount })}
          </AppButton>
        </div>
      </div>
    </article>
  )
}
