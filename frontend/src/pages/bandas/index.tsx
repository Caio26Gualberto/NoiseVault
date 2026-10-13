import { CalendarIcon, MapPinIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { formatDate } from '@/lib/format'
import { useAbcCityLabel } from '@/pages/perfil/cities'

import { BandMemberRole } from './band-member-role'
import { getBandProfile } from './services/bandas-service'
import type { BandProfileDto } from './services/bandas-service'

export function BandasPage() {
  const { t } = useTranslation('bandas')
  const { slug = '' } = useParams()
  const cityLabel = useAbcCityLabel()
  const [band, setBand] = useState<BandProfileDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getBandProfile(slug)
      .then((result) => {
        if (!cancelled) setBand(result)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [slug])

  if (loading || !band) {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      <AppProfileHero
        name={band.name}
        handle={`/bandas/${band.slug}`}
        avatarUrl={band.avatarUrl}
        bannerUrl={band.bannerUrl}
        bannerPosition={band.bannerPosition}
        verified={band.isVerified}
        subtitle={band.description}
        stats={[
          { label: t('stats.followers'), value: band.stats.followers },
          { label: t('stats.posts'), value: band.stats.posts },
          { label: t('stats.events'), value: band.stats.events },
        ]}
        actions={
          <>
            <AppSocialLinks links={band.socialLinks} />
            <AppButton disabled>{t('follow')}</AppButton>
          </>
        }
      >
        <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <MapPinIcon className="size-4" />
            {cityLabel(band.city)}
          </span>
          {band.foundedAt ? (
            <span className="inline-flex items-center gap-1.5">
              <CalendarIcon className="size-4" />
              {t('since', { date: formatDate(band.foundedAt) })}
            </span>
          ) : null}
          {band.isVerified ? <AppBadge tone="highlight">{t('verified')}</AppBadge> : null}
        </div>
        <AppGenreBadges genres={band.genres} max={5} tone="brand" />
      </AppProfileHero>

      <section className="flex flex-col gap-3">
        <h2 className="app-h3">{t('members')}</h2>
        <ul className="grid gap-3 sm:grid-cols-2">
          {band.members.map((member) => (
            <li key={member.user.id}>
              <Link
                to={`/perfil/${member.user.nickname}`}
                className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <AppAvatar name={member.user.name} src={member.user.avatarUrl} size="lg" />
                <div className="flex min-w-0 flex-1 flex-col">
                  <span className="truncate font-medium">{member.user.name}</span>
                  <span className="app-meta truncate">
                    {member.instrument} · {t('memberSince', { date: formatDate(member.joinedAt) })}
                  </span>
                </div>
                {member.role === BandMemberRole.owner ? <AppBadge tone="brand">{t('owner')}</AppBadge> : null}
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
