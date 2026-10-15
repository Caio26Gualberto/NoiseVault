import { MegaphoneIcon, PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { ensaioPath } from '@/ensaio/frame'
import { formatCity } from '@/pages/perfil/city'
import { useMusicGenreLabel } from '@/pages/perfil/genres'

import { EnsaioFilters } from './components/ensaio-filters'
import { FlyerCard } from './components/flyer-card'
import type { FlyerAction } from './components/flyer-card'
import { InterestDialog } from './components/interest-dialog'
import { useInstrumentLabel } from './ensaio-status'
import { useEnsaioList } from './hooks/use-ensaio-list'
import { useOwnedBands } from './hooks/use-owned-bands'
import { getOpenings } from './services/ensaio-service'
import type { BandOpeningDto } from './services/ensaio-service'

export function EnsaioVagasPage() {
  const { t } = useTranslation('ensaio')
  const list = useEnsaioList(getOpenings)
  const { bands } = useOwnedBands()
  const instrumentLabel = useInstrumentLabel()
  const genreLabel = useMusicGenreLabel()
  const [interested, setInterested] = useState<BandOpeningDto | null>(null)

  const meta = (opening: BandOpeningDto) =>
    [formatCity(opening), opening.band.genres[0] !== undefined ? genreLabel(opening.band.genres[0]) : null].filter(Boolean).join(' · ') || null

  const action = (opening: BandOpeningDto): FlyerAction => {
    if (opening.canManage) {
      return { label: t('openings.manageCount', { count: opening.interestCount ?? 0 }), to: ensaioPath(`/vagas/${opening.id}`) }
    }
    if (opening.myInterest) return { label: t('openings.interestSent'), disabled: true }
    return { label: t('openings.interested'), onClick: () => setInterested(opening) }
  }

  return (
    <div className="app-page-enter flex flex-col gap-8">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-1">
          <h1 className="font-heading text-[32px] leading-tight font-bold tracking-wide uppercase md:text-[44px]">{t('openings.title')}</h1>
          <p className="max-w-xl text-muted-foreground">{t('openings.description')}</p>
        </div>
        <div className="flex flex-col gap-3 lg:items-end">
          {bands.length > 0 ? (
            <AppButton asChild intent="secondary" className="self-start lg:self-end">
              <Link to={ensaioPath('/vagas/nova')}>
                <PlusIcon />
                {t('openings.create')}
              </Link>
            </AppButton>
          ) : null}
          <EnsaioFilters value={list.filters} onChange={list.changeFilters} />
        </div>
      </div>

      {list.loading ? (
        <AppSkeleton preset="card" count={6} className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3" />
      ) : list.items.length === 0 ? (
        <AppEmptyState icon={MegaphoneIcon} title={t('openings.emptyTitle')} description={t('openings.emptyDescription')} />
      ) : (
        <div className="grid gap-8 sm:grid-cols-2 sm:gap-10 lg:grid-cols-3">
          {list.items.map((opening, index) => (
            <FlyerCard
              key={opening.id}
              index={index}
              to={ensaioPath(`/vagas/${opening.id}`)}
              title={instrumentLabel(opening.instrument, opening.otherName)}
              name={opening.band.name}
              meta={meta(opening)}
              photoUrl={opening.band.avatarUrl}
              badge={<span className="font-typewriter text-sm text-paper-muted">{t('openings.wanted')}</span>}
              action={action(opening)}
            />
          ))}
        </div>
      )}

      <AppPagination page={list.page} totalPages={list.totalPages} totalCount={list.totalCount} onChange={list.setPage} loading={list.loading} />

      <InterestDialog
        opening={interested}
        onOpenChange={(open) => {
          if (!open) setInterested(null)
        }}
        onSent={() => list.refresh()}
      />
    </div>
  )
}
