import { CalendarDaysIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { emptyPage } from '@/api/types'
import type { PagedResult } from '@/api/types'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'

import { addInterest, getBandEvents, removeInterest } from '../services/eventos-service'
import type { EventDto, EventScope } from '../services/eventos-service'
import { EventCard } from './event-card'

const pageSize = 4

export function BandEvents({ slug }: { slug: string }) {
  const { t } = useTranslation('eventos')
  const [scope, setScope] = useState<EventScope>('upcoming')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<EventDto>>(() => emptyPage(pageSize))
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getBandEvents(slug, { page, pageSize, scope, genre: null, city: null, search: '', interested: false })
      .then((data) => {
        if (!cancelled) setResult(data)
      })
      .catch(() => {
        if (!cancelled) setResult(emptyPage(pageSize))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [slug, scope, page, version])

  const toggle = async (event: EventDto) => {
    await (event.interested ? removeInterest(event.id) : addInterest(event.id))
    setVersion((value) => value + 1)
  }

  return (
    <section className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="app-h3">{t('band.title')}</h2>
        <Tabs
          value={scope}
          onValueChange={(value) => {
            setScope(value as EventScope)
            setPage(1)
          }}
        >
          <TabsList>
            <TabsTrigger value="upcoming">{t('scope.upcoming')}</TabsTrigger>
            <TabsTrigger value="past">{t('scope.past')}</TabsTrigger>
          </TabsList>
        </Tabs>
      </div>
      {loading ? (
        <AppSkeleton preset="card" count={2} className="grid gap-4 md:grid-cols-2" />
      ) : result.items.length === 0 ? (
        <AppEmptyState icon={CalendarDaysIcon} title={scope === 'past' ? t('band.emptyPast') : t('band.empty')} />
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {result.items.map((event) => (
            <EventCard key={event.id} event={event} onToggleInterest={(target) => void toggle(target)} />
          ))}
        </div>
      )}
      <AppPagination page={page} totalPages={result.totalPages} totalCount={result.totalCount} onChange={setPage} loading={loading} />
    </section>
  )
}
