import { CalendarIcon, GuitarIcon, MapPinIcon, PencilIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { accountPath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatDate } from '@/lib/format'

import { useAbcCityLabel } from './cities'
import { getUserProfile } from './services/perfil-service'
import type { UserProfileDto } from './services/perfil-service'

export function PerfilPage() {
  const { t } = useTranslation('perfil')
  const { nickname = '' } = useParams()
  const { user } = useAuth()
  const cityLabel = useAbcCityLabel()
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getUserProfile(nickname)
      .then((result) => {
        if (!cancelled) setProfile(result)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [nickname])

  if (loading || !profile) {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  const isMe = user?.nickname === profile.nickname

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      <AppProfileHero
        name={profile.name}
        handle={`@${profile.nickname}`}
        avatarUrl={profile.avatarUrl}
        bannerUrl={profile.bannerUrl}
        bannerPosition={profile.bannerPosition}
        verified={profile.isVerified}
        subtitle={profile.bio}
        stats={[
          { label: t('stats.posts'), value: profile.stats.posts },
          { label: t('stats.photos'), value: profile.stats.photos },
          { label: t('stats.videos'), value: profile.stats.videos },
        ]}
        actions={
          <>
            <AppSocialLinks links={profile.socialLinks} />
            {isMe ? (
              <AppButton intent="neutral" asChild>
                <Link to={accountPath}>
                  <PencilIcon />
                  {t('edit')}
                </Link>
              </AppButton>
            ) : (
              <AppButton intent="secondary" disabled>
                {t('follow')}
              </AppButton>
            )}
          </>
        }
      >
        <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <MapPinIcon className="size-4" />
            {cityLabel(profile.city)}
          </span>
          {profile.instrument ? (
            <span className="inline-flex items-center gap-1.5">
              <GuitarIcon className="size-4" />
              {profile.instrument}
            </span>
          ) : null}
          <span className="inline-flex items-center gap-1.5">
            <CalendarIcon className="size-4" />
            {t('memberSince', { date: formatDate(profile.memberSince) })}
          </span>
        </div>
        <AppGenreBadges genres={profile.genres} max={5} tone="brand" />
      </AppProfileHero>

      {profile.bands.length > 0 ? (
        <section className="flex flex-col gap-3">
          <h2 className="app-h3">{t('bands')}</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            {profile.bands.map((band) => (
              <Link
                key={band.id}
                to={`/bandas/${band.slug}`}
                className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <AppAvatar name={band.name} src={band.avatarUrl} size="lg" verified={band.isVerified} />
                <div className="flex min-w-0 flex-col gap-1">
                  <span className="truncate font-medium">{band.name}</span>
                  <AppGenreBadges genres={band.genres} max={2} />
                </div>
              </Link>
            ))}
          </div>
        </section>
      ) : null}

      <Tabs defaultValue="posts">
        <TabsList>
          <TabsTrigger value="posts">{t('tabs.posts')}</TabsTrigger>
          <TabsTrigger value="photos">{t('tabs.photos')}</TabsTrigger>
          <TabsTrigger value="videos">{t('tabs.videos')}</TabsTrigger>
        </TabsList>
        {(['posts', 'photos', 'videos'] as const).map((tab) => (
          <TabsContent key={tab} value={tab} className="pt-4">
            <AppEmptyState title={t(`empty.${tab}`)} description={t('empty.description')} />
          </TabsContent>
        ))}
      </Tabs>
    </div>
  )
}
