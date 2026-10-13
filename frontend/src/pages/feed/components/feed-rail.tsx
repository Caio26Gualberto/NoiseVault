import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppSkeleton } from '@/components/app-skeleton'
import { dayParts, formatTime } from '@/lib/format'
import { getSuggestedBands } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import { getUpcomingEvents } from '@/pages/eventos/services/eventos-service'
import type { EventDto } from '@/pages/eventos/services/eventos-service'

export function FeedRail() {
  const { t } = useTranslation()
  const [events, setEvents] = useState<EventDto[] | null>(null)
  const [bands, setBands] = useState<BandSummaryDto[] | null>(null)

  useEffect(() => {
    let cancelled = false
    void getUpcomingEvents(3).then((result) => {
      if (!cancelled) setEvents(result)
    })
    void getSuggestedBands().then((result) => {
      if (!cancelled) setBands(result)
    })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="sticky top-20 flex flex-col gap-6">
      <section className="flex flex-col gap-3 rounded-xl border bg-card p-4">
        <div className="flex items-center justify-between">
          <h2 className="app-h3">{t('layout:rail.upcomingEvents')}</h2>
          <Link to="/eventos" className="text-[13px] text-primary hover:underline">
            {t('actions.seeAll')}
          </Link>
        </div>
        {events ? (
          <ul className="flex flex-col gap-3">
            {events.map((event) => {
              const { day, month } = dayParts(event.startsAt)
              return (
                <li key={event.id}>
                  <Link to={`/eventos/${event.id}`} className="flex items-center gap-3 rounded-lg transition-colors hover:bg-accent">
                    <span className="flex w-12 shrink-0 flex-col items-center rounded-lg border py-1 leading-none">
                      <span className="font-heading text-lg font-bold tabular-nums">{day}</span>
                      <span className="text-[10px] font-semibold tracking-wider uppercase text-muted-foreground">{month}</span>
                    </span>
                    <span className="flex min-w-0 flex-col">
                      <span className="truncate text-sm font-medium">{event.title}</span>
                      <span className="app-meta truncate">
                        {event.venue} · {formatTime(event.startsAt)}
                      </span>
                    </span>
                  </Link>
                </li>
              )
            })}
          </ul>
        ) : (
          <AppSkeleton preset="list" count={3} />
        )}
      </section>

      <section className="flex flex-col gap-3 rounded-xl border bg-card p-4">
        <h2 className="app-h3">{t('layout:rail.suggestedBands')}</h2>
        {bands ? (
          <ul className="flex flex-col gap-3">
            {bands.map((band) => (
              <li key={band.id}>
                <Link to={`/bandas/${band.slug}`} className="flex items-center gap-3 rounded-lg transition-colors hover:bg-accent">
                  <AppAvatar name={band.name} src={band.avatarUrl} size="md" verified={band.isVerified} />
                  <span className="flex min-w-0 flex-col gap-1">
                    <span className="truncate text-sm font-medium">{band.name}</span>
                    <AppGenreBadges genres={band.genres} max={2} />
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        ) : (
          <AppSkeleton preset="list" count={4} />
        )}
      </section>
    </div>
  )
}
