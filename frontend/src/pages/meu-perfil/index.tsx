import { zodResolver } from '@hookform/resolvers/zod'
import { EyeIcon, MapPinIcon, GuitarIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import type { ProfileDto } from '@/api/types'
import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppAvatarUploader } from '@/components/app-avatar-uploader'
import { AppBannerEditor } from '@/components/app-banner-editor'
import { AppButton } from '@/components/app-button'
import { AppChipsField, AppCityField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppFormAsideBox, AppFormLayout, AppFormPanel } from '@/components/app-form-layout'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { AppPageHeader } from '@/components/app-page-header'
import { removeAvatar, removeBanner, setAvatar, setBanner, setBannerPosition, updateProfile } from '@/pages/minha-conta/services/conta-service'
import { formatCity, toCityInput, toCityValue } from '@/pages/perfil/city'
import { featuredCities, searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { bioMaxLength, maxGenres, profileFormSchema } from './profile-validators'
import type { ProfileFormValues } from './profile-validators'

function toFormValues(user: ProfileDto): ProfileFormValues {
  return {
    name: user.name,
    bio: user.bio ?? '',
    city: toCityValue(user),
    mainInstrument: user.mainInstrument ?? '',
    genres: user.genres,
    instagram: user.socialLinks.instagram ?? '',
    youtube: user.socialLinks.youtube ?? '',
    spotify: user.socialLinks.spotify ?? '',
  }
}

const orNull = (value: string) => (value === '' ? null : value)

export function MeuPerfilPage() {
  const { t } = useTranslation('conta')
  const { user, replaceUser } = useAuth()
  const genreLabel = useMusicGenreLabel()
  const [saving, setSaving] = useState(false)

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: user ? toFormValues(user) : undefined,
  })

  const [name, bio, city, instrument, genres] = useWatch({ control: form.control, name: ['name', 'bio', 'city', 'mainInstrument', 'genres'] })

  if (!user) return null

  const save = async (values: ProfileFormValues) => {
    setSaving(true)
    try {
      const updated = await updateProfile({
        name: values.name,
        bio: orNull(values.bio),
        ...toCityInput(values.city),
        mainInstrument: orNull(values.mainInstrument),
        genres: values.genres as ProfileDto['genres'],
        socialLinks: {
          instagram: orNull(values.instagram),
          youtube: orNull(values.youtube),
          spotify: orNull(values.spotify),
        },
      })
      replaceUser(updated)
      form.reset(toFormValues(updated))
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const replaceAndReturn = async (request: Promise<ProfileDto>) => {
    const updated = await request
    replaceUser(updated)
    return updated
  }

  const previewCity = formatCity({ city: city?.city ?? null, cityName: city?.cityName ?? null })

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader
        title={t('profilePage.title')}
        description={t('profilePage.description')}
        actions={
          <AppButton asChild intent="secondary" size="sm">
            <Link to={`/perfil/${user.nickname}`}>
              <EyeIcon />
              {t('profilePage.view')}
            </Link>
          </AppButton>
        }
      />

      <AppForm form={form} onSubmit={save}>
        <AppFormLayout
          aside={
            <>
              <div className="flex flex-col gap-2">
                <span className="app-meta">{t('profilePage.preview')}</span>
                <div className="overflow-hidden rounded-xl border bg-card">
                  <div className="relative aspect-[3/1] bg-muted">
                    {user.bannerUrl ? (
                      <img src={user.bannerUrl} alt="" className="size-full object-cover" style={{ objectPosition: `center ${user.bannerPosition}%` }} />
                    ) : null}
                  </div>
                  <div className="flex flex-col gap-2 px-4 pb-4">
                    <span className="-mt-9 inline-flex self-start rounded-full bg-card p-1">
                      <AppAvatar name={name || user.name} src={user.avatarUrl} size="xl" />
                    </span>
                    <div className="flex flex-col">
                      <span className="app-h3 truncate">{name || user.name}</span>
                      <span className="app-meta">@{user.nickname}</span>
                    </div>
                    {previewCity || instrument ? (
                      <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
                        {previewCity ? (
                          <span className="inline-flex items-center gap-1.5">
                            <MapPinIcon className="size-4" />
                            {previewCity}
                          </span>
                        ) : null}
                        {instrument ? (
                          <span className="inline-flex items-center gap-1.5">
                            <GuitarIcon className="size-4" />
                            {instrument}
                          </span>
                        ) : null}
                      </div>
                    ) : null}
                    {bio ? <p className="line-clamp-3 text-sm whitespace-pre-line">{bio}</p> : null}
                    {genres?.length ? <AppGenreBadges genres={genres as MusicGenre[]} max={4} tone="brand" /> : null}
                  </div>
                </div>
              </div>

              <AppFormAsideBox title={t('profile.nickname')} description={t('profile.nicknameHint', { nickname: user.nickname })} />

              <AppButton type="submit" loading={saving} size="lg" className="w-full font-heading tracking-wider uppercase">
                {t('profile.save')}
              </AppButton>
            </>
          }
        >
          <AppFormPanel title={t('appearance.title')} description={t('appearance.description')} wide>
            <div className="grid gap-6 md:grid-cols-[auto_minmax(0,1fr)]">
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
          </AppFormPanel>

          <AppFormPanel title={t('profilePage.about')} wide>
            <AppField name="name" label={t('profile.name')} autoComplete="name" />
            <AppTextareaField
              name="bio"
              label={t('profile.bio')}
              rows={4}
              maxLength={bioMaxLength}
              placeholder={t('profile.bioPlaceholder')}
              description={t('profile.bioHint', { max: bioMaxLength })}
            />
          </AppFormPanel>

          <AppFormPanel title={t('profilePage.where')}>
            <AppCityField
              name="city"
              label={t('profile.city')}
              placeholder={t('profile.cityPlaceholder')}
              clearLabel={t('profile.cityNone')}
              hint={t('profile.cityTooltip')}
              search={searchCities} suggest={featuredCities}
              allowAbroad
            />
            <AppField name="mainInstrument" label={t('profile.instrument')} placeholder={t('profile.instrumentPlaceholder')} />
          </AppFormPanel>

          <AppFormPanel title={t('profilePage.links')}>
            <AppField name="instagram" type="url" label={t('profile.instagram')} placeholder="https://instagram.com/..." />
            <AppField name="youtube" type="url" label={t('profile.youtube')} placeholder="https://youtube.com/@..." />
            <AppField name="spotify" type="url" label={t('profile.spotify')} placeholder="https://open.spotify.com/..." />
          </AppFormPanel>

          <AppFormPanel title={t('profilePage.genres')} wide>
            <AppChipsField
              name="genres"
              label={t('profile.genres')}
              max={maxGenres}
              description={t('profile.genresHint', { max: maxGenres })}
              options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))}
            />
          </AppFormPanel>
        </AppFormLayout>
      </AppForm>
    </div>
  )
}
