import { CalendarIcon, LogOutIcon, MapPinIcon, PencilIcon, UsersRoundIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { formatDate } from '@/lib/format'
import { BandEvents } from '@/pages/eventos/components/band-events'
import { OwnerPosts } from '@/pages/feed/components/owner-posts'
import { useAbcCityLabel } from '@/pages/perfil/cities'

import { BandMemberRole } from './band-member-role'
import { useBand } from './hooks/use-band'
import { leaveBand } from './services/bandas-service'

export function BandasPage() {
  const { t } = useTranslation('bandas')
  const { slug = '' } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const cityLabel = useAbcCityLabel()
  const { state, reload } = useBand(slug)

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-4xl" />
  }

  if (state.status !== 'found') {
    return (
      <div className="app-page-enter mx-auto max-w-4xl">
        <AppEmptyState
          icon={UsersRoundIcon}
          title={state.status === 'notFound' ? t('notFound.title') : t('failed.title')}
          description={state.status === 'notFound' ? t('notFound.description', { slug }) : t('failed.description')}
          action={
            <AppButton intent="neutral" asChild>
              <Link to={homePath}>{t('notFound.action')}</Link>
            </AppButton>
          }
        />
      </div>
    )
  }

  const { band } = state
  const isOwner = band.myRole === BandMemberRole.owner
  const isMember = band.myRole === BandMemberRole.member

  const leave = () =>
    confirm({
      title: t('leave.title', { name: band.name }),
      message: t('leave.message'),
      confirmLabel: t('leave.confirm'),
      danger: true,
      onConfirm: async () => {
        await leaveBand(band.slug)
        if (user) navigate(`/perfil/${user.nickname}`)
        else reload()
      },
    })

  return (
    <div className="app-page-enter mx-auto flex max-w-4xl flex-col gap-6">
      <AppProfileHero
        name={band.name}
        handle={`/bandas/${band.slug}`}
        avatarUrl={band.avatarUrl}
        bannerUrl={band.bannerUrl}
        bannerPosition={band.bannerPosition}
        verified={band.isVerified}
        subtitle={band.description ? <p className="whitespace-pre-line">{band.description}</p> : null}
        stats={[
          { label: t('membersLabel', { count: band.members.length }), value: band.members.length },
          { label: t('postsLabel', { count: band.postCount }), value: band.postCount },
          { label: t('showsLabel', { count: band.upcomingEventCount }), value: band.upcomingEventCount },
        ]}
        actions={
          <>
            <AppSocialLinks links={band.socialLinks} />
            {isOwner ? (
              <AppButton intent="neutral" asChild>
                <Link to={`/bandas/${band.slug}/editar`}>
                  <PencilIcon />
                  {t('editAction')}
                </Link>
              </AppButton>
            ) : null}
            {isMember ? (
              <AppButton intent="ghost" onClick={leave}>
                <LogOutIcon />
                {t('leave.action')}
              </AppButton>
            ) : null}
          </>
        }
      >
        <div className="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm text-muted-foreground">
          {band.city !== null ? (
            <span className="inline-flex items-center gap-1.5">
              <MapPinIcon className="size-4" />
              {cityLabel(band.city)}
            </span>
          ) : null}
          {band.foundedYear !== null ? (
            <span className="inline-flex items-center gap-1.5">
              <CalendarIcon className="size-4" />
              {t('since', { year: band.foundedYear })}
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
                    {member.instrument ? `${member.instrument} · ` : ''}
                    {t('memberSince', { date: formatDate(member.joinedAt) })}
                  </span>
                </div>
                {member.role === BandMemberRole.owner ? <AppBadge tone="brand">{t('owner')}</AppBadge> : null}
              </Link>
            </li>
          ))}
        </ul>
      </section>

      <BandEvents slug={band.slug} />

      <div className="mx-auto w-full max-w-2xl">
        <OwnerPosts
          key={band.slug}
          owner={{ bandSlug: band.slug }}
          title={t('posts.title')}
          emptyTitle={t('posts.empty')}
          emptyDescription={isOwner || isMember ? t('posts.emptyMember') : undefined}
        />
      </div>
    </div>
  )
}
