import { BadgeCheckIcon, CalendarIcon, LogOutIcon, MapPinIcon, PencilIcon, UsersRoundIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'

import { homePath } from '@/auth/routes'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppBadge } from '@/components/app-badge'
import { AppButton } from '@/components/app-button'
import { AppShareButton } from '@/components/app-share-button'
import { useAppConfirm } from '@/components/app-confirm'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppProfileHero } from '@/components/app-profile-hero'
import { AppSkeleton } from '@/components/app-skeleton'
import { AppSocialLinks } from '@/components/app-social-links'
import { formatDate } from '@/lib/format'
import { BandOpenings } from '@/pages/ensaio/components/band-openings'
import { BandEvents } from '@/pages/eventos/components/band-events'
import { BandArchive } from '@/pages/mural/components/band-archive'
import { OwnerPosts } from '@/pages/feed/components/owner-posts'
import { formatCity } from '@/pages/perfil/city'

import { BandMemberRole } from './band-member-role'
import { useBand } from './hooks/use-band'
import { BandVerificationStatus } from './band-verification-status'
import { VerificationRequestDialog } from './components/verification-request-dialog'
import { leaveBand } from './services/bandas-service'

export function BandasPage() {
  const { t } = useTranslation('bandas')
  const { slug = '' } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const confirm = useAppConfirm()
  const [verifying, setVerifying] = useState(false)
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
            <AppShareButton path={`/bandas/${band.slug}`} title={band.name} label size="md" />
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
          {formatCity(band) ? (
            <span className="inline-flex items-center gap-1.5">
              <MapPinIcon className="size-4" />
              {formatCity(band)}
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

      {isOwner && !band.isVerified ? (
        <section className="flex flex-col gap-3 rounded-xl border border-dashed p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-3">
            <BadgeCheckIcon className="mt-0.5 size-5 shrink-0 text-highlight" />
            <div className="flex flex-col gap-1 text-sm">
              {band.verification?.status === BandVerificationStatus.pending ? (
                <>
                  <span className="font-medium">{t('verification.pendingTitle')}</span>
                  <span className="text-muted-foreground">{t('verification.pendingDescription')}</span>
                </>
              ) : band.verification?.status === BandVerificationStatus.rejected ? (
                <>
                  <span className="font-medium">{t('verification.rejectedTitle')}</span>
                  <span className="text-muted-foreground">{band.verification.rejectionReason}</span>
                </>
              ) : (
                <>
                  <span className="font-medium">{t('verification.askTitle')}</span>
                  <span className="text-muted-foreground">{t('verification.askDescription')}</span>
                </>
              )}
            </div>
          </div>
          {band.verification?.status !== BandVerificationStatus.pending ? (
            <AppButton intent="secondary" onClick={() => setVerifying(true)} className="shrink-0">
              {t(band.verification?.status === BandVerificationStatus.rejected ? 'verification.askAgain' : 'verification.ask')}
            </AppButton>
          ) : null}
          <VerificationRequestDialog slug={band.slug} open={verifying} onOpenChange={setVerifying} onRequested={reload} />
        </section>
      ) : null}

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

      <BandOpenings slug={band.slug} isOwner={isOwner} />

      <BandEvents slug={band.slug} />

      <BandArchive slug={band.slug} />

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
