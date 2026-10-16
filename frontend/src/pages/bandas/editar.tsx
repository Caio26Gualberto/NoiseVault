import { ArrowLeftIcon } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { AppAvatarUploader } from '@/components/app-avatar-uploader'
import { AppBannerEditor } from '@/components/app-banner-editor'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'

import { BandMemberRole } from './band-member-role'
import { toBandForm, toBandInput } from './band-validators'
import type { BandFormValues } from './band-validators'
import { BandForm } from './components/band-form'
import { InvitationsManager } from './components/invitations-manager'
import { DiscographyManager } from './components/discography-manager'
import { MembersManager } from './components/members-manager'
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
  const { user } = useAuth()
  const navigate = useNavigate()
  const { state, replace } = useBand(slug)
  const [saving, setSaving] = useState(false)

  if (state.status === 'loading') {
    return <AppSkeleton preset="profile" className="app-page-enter mx-auto max-w-2xl" />
  }

  if (state.status !== 'found' || state.band.myRole !== BandMemberRole.owner) {
    return <Navigate to={`/bandas/${slug}`} replace />
  }

  const { band } = state

  const apply = async (request: Promise<BandProfileDto>) => {
    const updated = await request
    replace(updated)
    return updated
  }

  const saveData = async (values: BandFormValues) => {
    setSaving(true)
    try {
      await apply(updateBand(band.slug, toBandInput(values)))
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader
        title={t('edit.title', { name: band.name })}
        description={t('edit.description')}
        actions={
          <AppButton intent="neutral" size="sm" asChild>
            <Link to={`/bandas/${band.slug}`}>
              <ArrowLeftIcon />
              {t('edit.back')}
            </Link>
          </AppButton>
        }
      />

      <AppCard title={t('edit.appearance.title')} description={t('edit.appearance.description')}>
        <div className="flex flex-col gap-6">
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
      </AppCard>

      <AppCard title={t('edit.data.title')} description={t('edit.data.description', { slug: band.slug })}>
        <BandForm key={band.id} defaultValues={toBandForm(band)} submitLabel={t('form.save')} saving={saving} onSubmit={saveData} />
      </AppCard>

      <MembersManager band={band} onChange={replace} onLeft={() => navigate(user ? `/perfil/${user.nickname}` : `/bandas/${band.slug}`)} />

      <InvitationsManager band={band} />
      <DiscographyManager band={band} />
    </div>
  )
}
