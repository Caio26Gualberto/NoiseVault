import { CassetteTapeIcon, PlusIcon, SearchIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { Permissions } from '@/auth/permissions'
import { useCan } from '@/auth/use-can'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPageHeader } from '@/components/app-page-header'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { AppCitySelect } from '@/components/app-city-select'
import { searchCities } from '@/pages/perfil/services/cidades-service'

import { ArchiveCard } from './components/archive-card'
import { Timeline } from './components/timeline'
import { useMuralPage } from './hooks/use-mural-page'
import type { ArchiveKind, ArchiveSort } from './services/mural-service'

const all = 'all'

export function MuralPage() {
  const { t } = useTranslation('mural')
  const can = useCan()
  const page = useMuralPage()
  const filtered = page.selection.year !== null || page.selection.decade !== null || page.search || page.city !== null || page.kind !== null

  return (
    <div className="app-page-enter flex flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          can(Permissions.archiveCreate) ? (
            <AppButton asChild>
              <Link to="/mural/novo">
                <PlusIcon />
                {t('create')}
              </Link>
            </AppButton>
          ) : null
        }
      />

      <div className="mb-4">
        <Timeline years={page.years} value={page.selection} onChange={page.changeSelection} />
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
            className="h-10 w-full rounded-lg border bg-transparent pr-3 pl-9 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
          />
        </div>
        <Select value={page.kind ?? all} onValueChange={(value) => page.changeKind(value === all ? null : (value as ArchiveKind))}>
          <SelectTrigger className="h-10 md:w-40" aria-label={t('filters.kind')}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={all}>{t('filters.allKinds')}</SelectItem>
            <SelectItem value="photo">{t('filters.photos')}</SelectItem>
            <SelectItem value="video">{t('filters.videos')}</SelectItem>
          </SelectContent>
        </Select>
        <AppCitySelect
          value={{ city: page.city, cityName: '' }}
          onChange={(value) => page.changeCity(value.city)}
          search={searchCities}
          placeholder={t('filters.allCities')}
          clearLabel={t('filters.allCities')}
          ariaLabel={t('filters.city')}
          className="h-10 md:h-10 md:w-52"
        />
        <Select value={page.sort} onValueChange={(value) => page.changeSort(value as ArchiveSort)}>
          <SelectTrigger className="h-10 md:w-44" aria-label={t('filters.sort')}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="newest">{t('filters.newest')}</SelectItem>
            <SelectItem value="oldest">{t('filters.oldest')}</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {page.loading && page.items.length === 0 ? (
        <AppSkeleton preset="card" count={6} className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" />
      ) : page.items.length === 0 ? (
        <AppEmptyState
          icon={CassetteTapeIcon}
          title={filtered ? t('empty.filteredTitle') : t('empty.title')}
          description={filtered ? t('empty.filteredDescription') : t('empty.description')}
        />
      ) : (
        <div
          key={`${page.selection.year}-${page.selection.decade}-${page.page}`}
          className={cn('app-page-enter grid gap-4 transition-opacity sm:grid-cols-2 xl:grid-cols-3', page.loading && 'opacity-60')}
        >
          {page.items.map((entry) => (
            <ArchiveCard key={entry.id} entry={entry} />
          ))}
        </div>
      )}

      <AppPagination page={page.page} totalPages={page.totalPages} totalCount={page.totalCount} onChange={page.setPage} loading={page.loading} />
    </div>
  )
}
