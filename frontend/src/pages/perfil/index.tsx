import { CalendarIcon, GuitarIcon, MapPinIcon, PencilIcon, UserRoundXIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'

import { ApiCodes } from '@/api/api-codes'
import { ApiError } from '@/api/types'
import { feedback } from '@/api/feedback-sink'
import { accountPath, homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppButton } from '@/components/app-button'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { formatDate } from '@/lib/format'

import { useAbcCityLabel } from './cities'
import { getUserProfile } from './services/perfil-service'
import type { UserProfileDto } from './services/perfil-service'

type LoadState = { status: 'loading' } | { status: 'found'; profile: UserProfileDto } | { status: 'notFound' } | { status: 'failed' }

export function PerfilPage() {
  const { t } = useTranslation('perfil')
  const { nickname = '' } = useParams()
  const { user } = useAuth()
  const cityLabel = useAbcCityLabel()
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
        subtitle={profile.bio ? <p className="whitespace-pre-line">{profile.bio}</p> : null}
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
            ) : null}
          </>
        }
      >
        <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
          {profile.city !== null ? (
            <span className="inline-flex items-center gap-1.5">
              <MapPinIcon className="size-4" />
              {cityLabel(profile.city)}
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

      <AppEmptyState title={t('empty.title')} description={t('empty.description')} />
    </div>
  )
}
