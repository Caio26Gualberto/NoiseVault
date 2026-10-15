import { UsersIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppPagination } from '@/components/app-pagination'
import { AppSkeleton } from '@/components/app-skeleton'
import { ensaioPath } from '@/ensaio/frame'
import { formatCity } from '@/pages/perfil/city'
import { useMusicGenreLabel } from '@/pages/perfil/genres'

import { EnsaioFilters } from './components/ensaio-filters'
import { FlyerCard } from './components/flyer-card'
import { InviteDialog } from './components/invite-dialog'
import { useDistanceLabel, useInstrumentLabel } from './ensaio-status'
import { useEnsaioList } from './hooks/use-ensaio-list'
import { useOwnedBands } from './hooks/use-owned-bands'
import { getMusicians, getMyMusicianProfile } from './services/ensaio-service'
import type { MusicianCardDto } from './services/ensaio-service'

export function EnsaioVitrinePage() {
  const { t } = useTranslation('ensaio')
  const list = useEnsaioList(getMusicians)
  const { bands } = useOwnedBands()
  const { user } = useAuth()
  const instrumentLabel = useInstrumentLabel()
  const genreLabel = useMusicGenreLabel()
  const distanceLabel = useDistanceLabel()
  const [inviting, setInviting] = useState<MusicianCardDto | null>(null)
  const [listed, setListed] = useState<boolean | null>(null)

  useEffect(() => {
    let cancelled = false
    getMyMusicianProfile()
      .then((mine) => {
        if (!cancelled) setListed(mine.exists && mine.isLookingForBand)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  const meta = (card: MusicianCardDto) => [formatCity(card), card.genres[0] !== undefined ? genreLabel(card.genres[0]) : null, distanceLabel(card.distanceKm)].filter(Boolean).join(' · ') || null

  return (
    <div className="app-page-enter flex flex-col gap-8">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-1">
          <h1 className="font-heading text-[32px] leading-tight font-bold tracking-wide uppercase md:text-[44px]">{t('musicians.title')}</h1>
          <p className="max-w-xl text-muted-foreground">{t('musicians.description')}</p>
        </div>
        <EnsaioFilters value={list.filters} onChange={list.changeFilters} withSamples />
      </div>

      {listed !== false ? null : (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-dashed border-chip-border px-4 py-3">
          <span>{t('musicians.notListed')}</span>
          <Link to={ensaioPath('/minha-ficha')} className="font-semibold text-primary hover:underline">
            {t('musicians.notListedAction')}
          </Link>
        </div>
      )}

      {list.loading ? (
        <AppSkeleton preset="card" count={6} className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3" />
      ) : list.items.length === 0 ? (
        <AppEmptyState icon={UsersIcon} title={t('musicians.emptyTitle')} description={t('musicians.emptyDescription')} />
      ) : (
        <div className="grid gap-8 sm:grid-cols-2 sm:gap-10 lg:grid-cols-3">
          {list.items.map((card, index) => (
            <FlyerCard
              key={card.person.id}
              index={index}
              to={ensaioPath(`/musicos/${card.person.nickname}`)}
              title={card.instrument ? instrumentLabel(card.instrument.instrument, card.instrument.otherName) : ''}
              name={card.person.name}
              meta={meta(card)}
              photoUrl={card.photoUrl ?? card.person.avatarUrl}
              action={bands.length > 0 && card.person.id !== user?.id ? { label: t('musicians.callToBand'), onClick: () => setInviting(card) } : null}
            />
          ))}
        </div>
      )}

      <AppPagination page={list.page} totalPages={list.totalPages} totalCount={list.totalCount} onChange={list.setPage} loading={list.loading} />

      <InviteDialog
        open={inviting !== null}
        onOpenChange={(open) => {
          if (!open) setInviting(null)
        }}
        bands={bands}
        person={inviting?.person ?? null}
        instrument={inviting?.instrument ? instrumentLabel(inviting.instrument.instrument, inviting.instrument.otherName) : ''}
      />
    </div>
  )
}
