import { CalendarDaysIcon, PlusIcon, SearchIcon, StarIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { cn } from '@/lib/utils'
import { AppCitySelect } from '@/components/app-city-select'
import { searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { EventCard } from './components/event-card'
import { useEventosPage } from './hooks/use-eventos-page'
import type { EventScope } from './services/eventos-service'

const all = 'all'

export function EventosPage() {
  const { t } = useTranslation('eventos')
  const page = useEventosPage()
  const genreLabel = useMusicGenreLabel()

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

      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Tabs value={page.scope} onValueChange={(value) => page.changeScope(value as EventScope)}>
          <TabsList>
            <TabsTrigger value="upcoming">{t('scope.upcoming')}</TabsTrigger>
            <TabsTrigger value="past">{t('scope.past')}</TabsTrigger>
          </TabsList>
        </Tabs>
        <button
          type="button"
          onClick={() => page.changeInterested(!page.interested)}
          aria-pressed={page.interested}
          className={cn(
            'inline-flex h-8 items-center gap-1.5 rounded-full border px-3 text-sm transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
            page.interested && 'border-primary bg-primary/15 text-primary hover:bg-primary/20',
          )}
        >
          <StarIcon className={cn('size-4', page.interested && 'fill-current')} />
          {t('filters.going')}
        </button>
      </div>

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
        <AppCitySelect
          value={{ city: page.city, cityName: '' }}
          onChange={(value) => page.changeCity(value.city)}
          search={searchCities}
          placeholder={t('filters.allCities')}
          clearLabel={t('filters.allCities')}
          ariaLabel={t('filters.city')}
          className="h-10 md:h-10 md:w-56"
        />
      </div>

      {page.loading ? (
        <AppSkeleton preset="card" count={3} className="grid gap-4 md:grid-cols-2 xl:grid-cols-3" />
      ) : page.items.length === 0 ? (
        <AppEmptyState
          icon={CalendarDaysIcon}
          title={page.interested ? t('empty.goingTitle') : page.scope === 'past' ? t('empty.pastTitle') : t('empty.title')}
          description={page.interested ? t('empty.goingDescription') : t('empty.description')}
        />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {page.items.map((event) => (
            <EventCard key={event.id} event={event} onToggleInterest={(target) => void page.toggle(target)} />
          ))}
        </div>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
