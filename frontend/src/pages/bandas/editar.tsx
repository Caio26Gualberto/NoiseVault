import { ArrowLeftIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation, useNavigate, useParams } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { AppAvatarUploader } from '@/components/app-avatar-uploader'
import { AppBannerEditor } from '@/components/app-banner-editor'
import { AppButton } from '@/components/app-button'
import { AppFormAsideBox, AppFormPanel } from '@/components/app-form-layout'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'

import { bandEditPath, bandKindContext, bandPagePath, canonicalBandLocation, isSolo } from './band-kind'
import { BandMemberRole } from './band-member-role'
import { toBandForm, toBandInput } from './band-validators'
import type { BandFormValues } from './band-validators'
import { BandForm } from './components/band-form'
import { InvitationsManager } from './components/invitations-manager'
import { DiscographyManager } from './components/discography-manager'
import { MembersManager } from './components/members-manager'
import { PartnershipsManager } from './components/partnerships-manager'
import { useBand } from './hooks/use-band'
import {
  removeBandAvatar,
  removeBandBanner,
  setBandAvatar,
  setBandBanner,
  setBandBannerPosition,
  updateBand,
} from './services/bandas-service'
import type { BandProfileDto } from './services/bandas-service'

export function BandaEditarPage() {
  const { t } = useTranslation('bandas')
  const { slug = '' } = useParams()
  const { pathname } = useLocation()
  const { user } = useAuth()
  const navigate = useNavigate()
  const { state, replace } = useBand(slug)
  const [saving, setSaving] = useState(false)

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-2xl" />
  }

  if (state.status !== 'found' || state.band.myRole !== BandMemberRole.owner) {
    return <Navigate to={state.status === 'found' ? bandPagePath(state.band) : pathname.replace(/\/editar$/, '')} replace />
  }

  const { band } = state
  const canonical = canonicalBandLocation(pathname, band.kind)

  if (canonical) return <Navigate to={canonical} replace />

  const context = bandKindContext(band.kind)
  const solo = isSolo(band.kind)

  const apply = async (request: Promise<BandProfileDto>) => {
    const updated = await request
    replace(updated)
    return updated
  }

  const saveData = async (values: BandFormValues) => {
    setSaving(true)
    try {
      const updated = await apply(updateBand(band.slug, toBandInput(values)))
      if (updated.kind !== band.kind) navigate(bandEditPath(updated), { replace: true })
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader
        title={t('edit.title', { name: band.name })}
        description={t('edit.description')}
        actions={
          <AppButton intent="neutral" size="sm" asChild>
            <Link to={bandPagePath(band)}>
              <ArrowLeftIcon />
              {t('edit.back', { context })}
            </Link>
          </AppButton>
        }
      />

      <BandForm
        key={`${band.id}-${band.kind ?? 0}`}
        defaultValues={toBandForm(band)}
        withKind
        submitLabel={t('form.save')}
        saving={saving}
        onSubmit={saveData}
        preview={{ slug: band.slug, avatarUrl: band.avatarUrl, bannerUrl: band.bannerUrl, bannerPosition: band.bannerPosition, isVerified: band.isVerified }}
        asideNote={<AppFormAsideBox title={t('edit.data.title', { context })} description={t('edit.data.description', { context, path: bandPagePath(band) })} />}
        appearance={
          <AppFormPanel title={t('edit.appearance.title')} description={t('edit.appearance.description', { context })} wide>
            <div className="grid gap-6 md:grid-cols-[auto_minmax(0,1fr)]">
              <AppAvatarUploader
                name={band.name}
                avatarUrl={band.avatarUrl}
                onUpload={(file) => apply(setBandAvatar(band.slug, file))}
                onRemove={() => apply(removeBandAvatar(band.slug))}
              />
              <AppBannerEditor
                bannerUrl={band.bannerUrl}
                bannerPosition={band.bannerPosition}
                onUpload={(file, position) => apply(setBandBanner(band.slug, file, position))}
                onPosition={(position) => apply(setBandBannerPosition(band.slug, position))}
                onRemove={() => apply(removeBandBanner(band.slug))}
              />
            </div>
          </AppFormPanel>
        }
      />

      {solo ? null : (
        <>
          <MembersManager band={band} onChange={replace} onLeft={() => navigate(user ? `/perfil/${user.nickname}` : bandPagePath(band))} />
          <InvitationsManager band={band} />
        </>
      )}
      <PartnershipsManager band={band} />
      <DiscographyManager band={band} />
    </div>
  )
}
