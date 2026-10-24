import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'

import { bandKindKey, bandPagePath } from '../band-kind'
import { getBandPartners } from '../services/bandas-service'
import type { BandPartnershipDto } from '../services/bandas-service'
import { partnersPageSize } from './partnerships-manager'

export function BandPartners({ slug }: { slug: string }) {
  const { t } = useTranslation('bandas')
  const [partners, setPartners] = useState<BandPartnershipDto[]>([])

  useEffect(() => {
    let cancelled = false
    getBandPartners(slug, { page: 1, pageSize: partnersPageSize })
      .then((page) => {
        if (!cancelled) setPartners(page.items)
      })
      .catch(() => {
        if (!cancelled) setPartners([])
      })
    return () => {
      cancelled = true
    }
  }, [slug])

  if (partners.length === 0) return null

  return (
    <section className="flex flex-col gap-3">
      <h2 className="app-h3">{t('partners.title')}</h2>
      <ul className="grid gap-3 sm:grid-cols-2">
        {partners.map(({ id, partner }) => (
          <li key={id}>
            <Link
              to={bandPagePath(partner)}
              className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
            >
              <AppAvatar name={partner.name} src={partner.avatarUrl} size="lg" verified={partner.isVerified} />
              <span className="flex min-w-0 flex-1 flex-col">
                <span className="truncate font-medium">{partner.name}</span>
                <span className="app-meta truncate">{t(`kinds.${bandKindKey[partner.kind ?? 0]}.title`)}</span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  )
}
