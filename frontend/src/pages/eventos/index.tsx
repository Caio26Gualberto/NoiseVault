import { CalendarDaysIcon, PlusIcon, SearchIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { allAbcCities, useAbcCityLabel } from '@/pages/perfil/cities'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { AbcCity } from '@/pages/perfil/cities'
import type { MusicGenre } from '@/pages/perfil/genres'

import { EventCard } from './components/event-card'
import { useEventosPage } from './hooks/use-eventos-page'

const all = 'all'

export function EventosPage() {
  const { t } = useTranslation('eventos')
  const page = useEventosPage()
  const genreLabel = useMusicGenreLabel()
  const cityLabel = useAbcCityLabel()

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton asChild>
            <Link to="/eventos/novo">
              <PlusIcon />
              {t('create')}
            </Link>
          </AppButton>
        }
      />

      <div className="mb-6 flex flex-col gap-3 md:flex-row">
        <div className="relative flex-1">
          <SearchIcon className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
          <input
            type="search"
            value={page.search}
            onChange={(event) => page.changeSearch(event.target.value)}
            placeholder={t('searchPlaceholder')}
            aria-label={t('searchPlaceholder')}
            className="h-10 w-full rounded-lg border bg-transparent pl-9 pr-3 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
          />
        </div>
        <Select
          value={page.genre === null ? all : String(page.genre)}
          onValueChange={(value) => page.changeGenre(value === all ? null : (Number(value) as MusicGenre))}
        >
          <SelectTrigger className="h-10 md:w-48" aria-label={t('filters.genre')}>
            <SelectValue placeholder={t('filters.genre')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={all}>{t('filters.allGenres')}</SelectItem>
            {allMusicGenres.map((genre) => (
              <SelectItem key={genre} value={String(genre)}>
                {genreLabel(genre)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={page.city === null ? all : String(page.city)}
          onValueChange={(value) => page.changeCity(value === all ? null : (Number(value) as AbcCity))}
        >
          <SelectTrigger className="h-10 md:w-56" aria-label={t('filters.city')}>
            <SelectValue placeholder={t('filters.city')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={all}>{t('filters.allCities')}</SelectItem>
            {allAbcCities.map((city) => (
              <SelectItem key={city} value={String(city)}>
                {cityLabel(city)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {page.loading ? (
        <AppSkeleton preset="card" count={3} className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" />
      ) : page.items.length === 0 ? (
        <AppEmptyState icon={CalendarDaysIcon} title={t('empty.title')} description={t('empty.description')} />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {page.items.map((event) => (
            <EventCard key={event.id} event={event} onToggleInterest={(id) => void page.toggle(id)} />
          ))}
        </div>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
