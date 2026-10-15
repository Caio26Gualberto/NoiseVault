import { ChevronRightIcon, MegaphoneIcon, PlusIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { EnsaioLink } from '@/ensaio/ensaio-link'

import { OpeningStatus, useInstrumentLabel } from '../ensaio-status'
import { getBandOpenings } from '../services/ensaio-service'
import type { BandOpeningDto } from '../services/ensaio-service'

export function BandOpenings({ slug, isOwner }: { slug: string; isOwner: boolean }) {
  const { t } = useTranslation('ensaio')
  const instrumentLabel = useInstrumentLabel()
  const [openings, setOpenings] = useState<BandOpeningDto[]>([])

  useEffect(() => {
    let cancelled = false
    getBandOpenings(slug, { page: 1, pageSize: 5 })
      .then((data) => {
        if (!cancelled) setOpenings(data.items.filter((item) => item.status === OpeningStatus.open && !item.isExpired))
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [slug])

  if (openings.length === 0 && !isOwner) return null

  return (
    <section className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <h2 className="app-h3">{t('openings.title')}</h2>
        {isOwner ? (
          <AppButton size="sm" intent="neutral" asChild>
            <EnsaioLink to="/vagas/nova">
              <PlusIcon />
              {t('openings.create')}
            </EnsaioLink>
          </AppButton>
        ) : null}
      </div>
      {openings.length === 0 ? (
        <p className="app-meta">{t('openings.emptyTitle')}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {openings.map((opening) => (
            <li key={opening.id}>
              <EnsaioLink
                to={`/vagas/${opening.id}`}
                className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <span className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
                  <MegaphoneIcon className="size-5" />
                </span>
                <span className="flex min-w-0 flex-1 flex-col">
                  <span className="font-heading font-semibold tracking-wide uppercase">{t('openings.lookingFor', { instrument: instrumentLabel(opening.instrument, opening.otherName) })}</span>
                  <span className="app-meta truncate">{opening.description}</span>
                </span>
                <ChevronRightIcon className="size-4 text-muted-foreground" />
              </EnsaioLink>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
