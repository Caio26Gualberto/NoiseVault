import { ScissorsIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { EnsaioLink } from '@/ensaio/ensaio-link'
import { OpeningStatus, openingStatusKey, useInstrumentLabel } from '@/pages/ensaio/ensaio-status'
import type { BandOpeningSummaryDto } from '@/pages/ensaio/services/ensaio-service'

export function OpeningEmbed({ opening }: { opening: BandOpeningSummaryDto }) {
  const { t } = useTranslation('ensaio')
  const instrumentLabel = useInstrumentLabel()
  const open = opening.status === OpeningStatus.open && !opening.isExpired
  const status = opening.status === OpeningStatus.open && opening.isExpired ? 'expired' : openingStatusKey[opening.status]

  return (
    <div className="ensaio">
      <EnsaioLink
        to={`/vagas/${opening.id}`}
        className="group relative flex flex-col bg-paper text-paper-foreground shadow-md transition-transform focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
      >
        <span aria-hidden="true" className="absolute -top-2 left-1/2 h-5 w-20 -translate-x-1/2 -rotate-2 bg-tape" />
        <span className="flex flex-col gap-1 px-5 pt-5 pb-4">
          <span className="flex items-center justify-between gap-3">
            <span className="font-typewriter text-sm text-paper-muted">{t('openings.wanted')}</span>
            {open ? null : <AppBadge tone="neutral">{t(`status.${status}`)}</AppBadge>}
          </span>
          <span className="font-heading text-4xl leading-none font-bold uppercase">{instrumentLabel(opening.instrument, opening.otherName)}</span>
          <span className="mt-1 line-clamp-3 text-sm whitespace-pre-line">{opening.description}</span>
        </span>
        {open ? (
          <span className="flex h-11 items-center justify-center gap-2 border-t-2 border-dashed border-paper-rule font-typewriter group-hover:bg-paper-rule/15">
            <ScissorsIcon className="size-4" aria-hidden="true" />
            {t('openings.interested')}
          </span>
        ) : null}
      </EnsaioLink>
    </div>
  )
}
