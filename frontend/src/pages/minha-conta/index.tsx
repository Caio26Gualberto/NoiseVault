import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { tokenStorage } from '@/api/token-storage'
import type { ProfileDto } from '@/api/types'
import { changePassword } from '@/auth/auth-service'
import { useAuth } from '@/auth/use-auth'
import { passwordSchema, withPasswordConfirmation } from '@/auth/validators'
import { AppAvatarUploader } from '@/components/app-avatar-uploader'
import { AppBannerEditor } from '@/components/app-banner-editor'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppChipsField, AppField, AppForm, AppPasswordField, AppSelectField, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { allAbcCities, useAbcCityLabel } from '@/pages/perfil/cities'
import type { AbcCity } from '@/pages/perfil/cities'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'

import { bioMaxLength, maxGenres, profileFormSchema } from './profile-validators'
import type { ProfileFormValues } from './profile-validators'
import { removeAvatar, removeBanner, setAvatar, setBanner, setBannerPosition, updateProfile } from './services/conta-service'

const passwordFormSchema = withPasswordConfirmation(
  z.object({
    currentPassword: z.string().min(1, 'validation.required'),
    password: passwordSchema,
    confirmPassword: z.string(),
  }),
)

type PasswordValues = z.infer<typeof passwordFormSchema>

function toFormValues(user: ProfileDto): ProfileFormValues {
  return {
    name: user.name,
    bio: user.bio ?? '',
    city: user.city === null ? '' : String(user.city),
    mainInstrument: user.mainInstrument ?? '',
    genres: user.genres,
    instagram: user.socialLinks.instagram ?? '',
    youtube: user.socialLinks.youtube ?? '',
    spotify: user.socialLinks.spotify ?? '',
  }
}

const orNull = (value: string) => (value === '' ? null : value)

export function MinhaContaPage() {
  const { t } = useTranslation('conta')
  const { t: tAuth } = useTranslation('auth')
  const { user, replaceUser } = useAuth()
  const cityLabel = useAbcCityLabel()
  const genreLabel = useMusicGenreLabel()
  const [savingProfile, setSavingProfile] = useState(false)
  const [savingPassword, setSavingPassword] = useState(false)

  const profileForm = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: user ? toFormValues(user) : undefined,
  })

  const passwordForm = useForm<PasswordValues>({
    resolver: zodResolver(passwordFormSchema),
    defaultValues: { currentPassword: '', password: '', confirmPassword: '' },
  })

  if (!user) return null

  const saveProfile = async (values: ProfileFormValues) => {
    setSavingProfile(true)
    try {
      const updated = await updateProfile({
        name: values.name,
        bio: orNull(values.bio),
        city: values.city === '' ? null : (Number(values.city) as AbcCity),
        mainInstrument: orNull(values.mainInstrument),
        genres: values.genres as ProfileDto['genres'],
        socialLinks: {
          instagram: orNull(values.instagram),
          youtube: orNull(values.youtube),
          spotify: orNull(values.spotify),
        },
      })
      replaceUser(updated)
      profileForm.reset(toFormValues(updated))
    } catch {
      return
    } finally {
      setSavingProfile(false)
    }
  }

  const savePassword = async ({ currentPassword, password }: PasswordValues) => {
    setSavingPassword(true)
    try {
      const tokens = await changePassword(currentPassword, password)
      tokenStorage.saveTokens(tokens.token, tokens.refreshToken)
      passwordForm.reset()
    } catch {
      return
    } finally {
      setSavingPassword(false)
    }
  }

  const replaceAndReturn = async (request: Promise<ProfileDto>) => {
    const updated = await request
    replaceUser(updated)
    return updated
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader title={t('title')} description={t('description')} />

      <AppCard title={t('appearance.title')} description={t('appearance.description')}>
        <div className="flex flex-col gap-6">
          <AppAvatarUploader
            name={user.name}
            avatarUrl={user.avatarUrl}
            onUpload={async (file) => replaceUser(await setAvatar(file))}
            onRemove={async () => replaceUser(await removeAvatar())}
          />
          <AppBannerEditor
            bannerUrl={user.bannerUrl}
            bannerPosition={user.bannerPosition}
            onUpload={(file, position) => replaceAndReturn(setBanner(file, position))}
            onPosition={(position) => replaceAndReturn(setBannerPosition(position))}
            onRemove={() => replaceAndReturn(removeBanner())}
          />
        </div>
      </AppCard>

      <AppCard title={t('profile.title')} description={t('profile.description')}>
        <AppForm form={profileForm} onSubmit={saveProfile}>
          <AppField name="name" label={t('profile.name')} autoComplete="name" />
          <div className="grid gap-4 md:grid-cols-2">
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">{t('profile.nickname')}</span>
              <span className="flex h-11 items-center rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">
                @{user.nickname}
              </span>
              <span className="text-[13px] text-muted-foreground">{t('profile.nicknameHint', { nickname: user.nickname })}</span>
            </div>
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-medium">{t('profile.email')}</span>
              <span className="flex h-11 items-center truncate rounded-lg border bg-muted/40 px-3 text-sm text-muted-foreground md:h-10">
                {user.email}
              </span>
            </div>
          </div>
          <AppTextareaField
            name="bio"
            label={t('profile.bio')}
            rows={4}
            maxLength={bioMaxLength}
            placeholder={t('profile.bioPlaceholder')}
            description={t('profile.bioHint', { max: bioMaxLength })}
          />
          <div className="grid gap-4 md:grid-cols-2">
            <AppSelectField
              name="city"
              label={t('profile.city')}
              placeholder={t('profile.cityPlaceholder')}
              emptyLabel={t('profile.cityNone')}
              options={allAbcCities.map((city) => ({ value: String(city), label: cityLabel(city) }))}
            />
            <AppField name="mainInstrument" label={t('profile.instrument')} placeholder={t('profile.instrumentPlaceholder')} />
          </div>
          <AppChipsField
            name="genres"
            label={t('profile.genres')}
            max={maxGenres}
            description={t('profile.genresHint', { max: maxGenres })}
            options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))}
          />
          <div className="grid gap-4 md:grid-cols-3">
            <AppField name="instagram" type="url" label={t('profile.instagram')} placeholder="https://instagram.com/..." />
            <AppField name="youtube" type="url" label={t('profile.youtube')} placeholder="https://youtube.com/@..." />
            <AppField name="spotify" type="url" label={t('profile.spotify')} placeholder="https://open.spotify.com/..." />
          </div>
          <AppButton type="submit" loading={savingProfile} className="self-start">
            {t('profile.save')}
          </AppButton>
        </AppForm>
      </AppCard>

      <AppCard title={t('password.title')} description={t('password.description')}>
        <AppForm form={passwordForm} onSubmit={savePassword}>
          <AppPasswordField name="currentPassword" label={tAuth('fields.currentPassword')} autoComplete="current-password" />
          <div className="grid gap-4 md:grid-cols-2">
            <AppPasswordField name="password" label={tAuth('fields.newPassword')} autoComplete="new-password" />
            <AppPasswordField name="confirmPassword" label={tAuth('fields.confirmPassword')} autoComplete="new-password" />
          </div>
          <AppButton type="submit" intent="secondary" loading={savingPassword} className="self-start">
            {t('password.save')}
          </AppButton>
        </AppForm>
      </AppCard>
    </div>
  )
}
