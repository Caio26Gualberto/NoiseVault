import { CalendarIcon, GuitarIcon, MapPinIcon, MegaphoneIcon, PencilIcon, PlusIcon, UserRoundXIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { ApiCodes } from '@/api/api-codes'
import { ApiError } from '@/api/types'
import { feedback } from '@/api/feedback-sink'
import { homePath, profileEditPath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppShareButton } from '@/components/app-share-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { EnsaioLink } from '@/ensaio/ensaio-link'
import { formatDate } from '@/lib/format'
import { OwnerPosts } from '@/pages/feed/components/owner-posts'

import { formatCity } from './city'
import { getUserProfile } from './services/perfil-service'
import type { UserProfileDto } from './services/perfil-service'

type LoadState = { status: 'loading' } | { status: 'found'; profile: UserProfileDto } | { status: 'notFound' } | { status: 'failed' }

export function PerfilPage() {
  const { t } = useTranslation('perfil')
  const { nickname = '' } = useParams()
  const { user } = useAuth()
  const [state, setState] = useState<LoadState>({ status: 'loading' })

  useEffect(() => {
    let cancelled = false
    setState({ status: 'loading' })
    getUserProfile(nickname)
      .then((profile) => {
        if (!cancelled) setState({ status: 'found', profile })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        if (error instanceof ApiError && error.code === ApiCodes.profileNotFound) {
          setState({ status: 'notFound' })
          return
        }
        if (error instanceof ApiError) feedback.error(error.code, error.backendMessage)
        setState({ status: 'failed' })
      })
    return () => {
      cancelled = true
    }
  }, [nickname])

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  if (state.status !== 'found') {
    return (
      <div className="app-page-enter mx-auto max-w-4xl">
        <AppEmptyState
          icon={UserRoundXIcon}
          title={state.status === 'notFound' ? t('notFound.title') : t('failed.title')}
          description={state.status === 'notFound' ? t('notFound.description', { nickname }) : t('failed.description')}
          action={
            <AppButton intent="neutral" asChild>
              <Link to={homePath}>{t('notFound.action')}</Link>
            </AppButton>
          }
        />
      </div>
    )
  }

  const { profile } = state
  const isMe = user?.nickname === profile.nickname

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      <AppProfileHero
        name={profile.name}
        handle={`@${profile.nickname}`}
        avatarUrl={profile.avatarUrl}
        bannerUrl={profile.bannerUrl}
        bannerPosition={profile.bannerPosition}
        stats={[{ label: t('postsLabel', { count: profile.postCount }), value: profile.postCount }]}
        subtitle={profile.bio ? <p className="whitespace-pre-line">{profile.bio}</p> : null}
        actions={
          <>
            <AppSocialLinks links={profile.socialLinks} />
            <AppShareButton path={`/perfil/${profile.nickname}`} title={profile.name} label size="md" />
            {isMe ? (
              <AppButton intent="neutral" asChild>
                <Link to={profileEditPath}>
                  <PencilIcon />
                  {t('edit')}
                </Link>
              </AppButton>
            ) : null}
          </>
        }
      >
        <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
          {profile.isLookingForBand ? (
            <EnsaioLink
              to={`/musicos/${profile.nickname}`}
              className="inline-flex items-center gap-1.5 rounded-full bg-success/15 px-2.5 py-1 font-medium text-success hover:bg-success/25"
            >
              <MegaphoneIcon className="size-4" />
              {t('lookingForBand')}
            </EnsaioLink>
          ) : null}
          {formatCity(profile) ? (
            <span className="inline-flex items-center gap-1.5">
              <MapPinIcon className="size-4" />
              {formatCity(profile)}
            </span>
          ) : null}
          {profile.mainInstrument ? (
            <span className="inline-flex items-center gap-1.5">
              <GuitarIcon className="size-4" />
              {profile.mainInstrument}
            </span>
          ) : null}
          <span className="inline-flex items-center gap-1.5">
            <CalendarIcon className="size-4" />
            {t('memberSince', { date: formatDate(profile.memberSince) })}
          </span>
        </div>
        <AppGenreBadges genres={profile.genres} max={10} tone="brand" />
      </AppProfileHero>

      {profile.bands.length > 0 || isMe ? (
        <section className="flex flex-col gap-3">
          <div className="flex items-center justify-between gap-3">
            <h2 className="app-h3">{t('bands.title')}</h2>
            {isMe ? (
              <AppButton size="sm" intent="neutral" asChild>
                <Link to="/bandas/nova">
                  <PlusIcon />
                  {t('bands.create')}
                </Link>
              </AppButton>
            ) : null}
          </div>
          {profile.bands.length === 0 ? (
            <p className="app-meta">{t('bands.empty')}</p>
          ) : (
            <ul className="grid gap-3 sm:grid-cols-2">
              {profile.bands.map((band) => (
                <li key={band.id}>
                  <Link
                    to={`/bandas/${band.slug}`}
                    className="flex items-center gap-3 rounded-xl border bg-card p-3 transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                  >
                    <AppAvatar name={band.name} src={band.avatarUrl} size="lg" verified={band.isVerified} />
                    <span className="flex min-w-0 flex-1 flex-col gap-1">
                      <span className="truncate font-medium">{band.name}</span>
                      <AppGenreBadges genres={band.genres} max={2} />
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>
      ) : null}

      <div className="mx-auto w-full max-w-2xl">
        <OwnerPosts
          key={profile.nickname}
          owner={{ nickname: profile.nickname }}
          title={t('posts.title')}
          emptyTitle={isMe ? t('posts.emptyMine') : t('posts.empty', { name: profile.name })}
        />
      </div>
    </div>
  )
}
