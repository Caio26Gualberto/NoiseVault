import { zodResolver } from '@hookform/resolvers/zod'
import { CalendarIcon, MapPinIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { AppChipsField, AppCityField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { plainMarkdown } from '@/lib/markdown'
import { AppFormLayout, AppFormPanel } from '@/components/app-form-layout'
import { AppGenreBadges } from '@/components/app-genre-badges'
import { maxGenres } from '@/pages/meu-perfil/profile-validators'
import { formatCity } from '@/pages/perfil/city'
import { featuredCities, searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { bandDescriptionMaxLength, bandFormSchema } from '../band-validators'
import type { BandFormValues } from '../band-validators'

type BandFormPreview = {
  slug: string | null
  avatarUrl: string | null
  bannerUrl: string | null
  bannerPosition: number
  isVerified: boolean
}

const emptyPreview: BandFormPreview = { slug: null, avatarUrl: null, bannerUrl: null, bannerPosition: 50, isVerified: false }

type BandFormProps = {
  defaultValues: BandFormValues
  withInstrument?: boolean
  submitLabel: string
  saving: boolean
  onSubmit: (values: BandFormValues) => Promise<void>
  onCancel?: () => void
  preview?: BandFormPreview
  asideNote?: ReactNode
  appearance?: ReactNode
}

export function BandForm({
  defaultValues,
  withInstrument = false,
  submitLabel,
  saving,
  onSubmit,
  onCancel,
  preview = emptyPreview,
  asideNote,
  appearance,
}: BandFormProps) {
  const { t } = useTranslation('bandas')
  const genreLabel = useMusicGenreLabel()

  const form = useForm<BandFormValues>({
    resolver: zodResolver(bandFormSchema),
    defaultValues,
  })

  const [name, description, city, foundedYear, genres] = useWatch({
    control: form.control,
    name: ['name', 'description', 'city', 'foundedYear', 'genres'],
  })

  const submit = async (values: BandFormValues) => {
    await onSubmit(values)
    form.reset(values)
  }

  const previewCity = formatCity({ city: city?.city ?? null, cityName: city?.cityName ?? null })
  const previewName = name || t('form.previewName')

  return (
    <AppForm form={form} onSubmit={submit}>
      <AppFormLayout
        aside={
          <>
            <div className="flex flex-col gap-2">
              <span className="app-meta">{t('form.preview')}</span>
              <div className="overflow-hidden rounded-xl border bg-card">
                <div className="relative aspect-[3/1] bg-muted">
                  {preview.bannerUrl ? (
                    <img src={preview.bannerUrl} alt="" className="size-full object-cover" style={{ objectPosition: `center ${preview.bannerPosition}%` }} />
                  ) : null}
                </div>
                <div className="flex flex-col gap-2 px-4 pb-4">
                  <span className="-mt-9 inline-flex self-start rounded-full bg-card p-1">
                    <AppAvatar name={previewName} src={preview.avatarUrl} size="xl" verified={preview.isVerified} />
                  </span>
                  <div className="flex flex-col">
                    <span className="app-h3 truncate">{previewName}</span>
                    {preview.slug ? <span className="app-meta">/bandas/{preview.slug}</span> : null}
                  </div>
                  {previewCity || foundedYear ? (
                    <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground">
                      {previewCity ? (
                        <span className="inline-flex items-center gap-1.5">
                          <MapPinIcon className="size-4" />
                          {previewCity}
                        </span>
                      ) : null}
                      {foundedYear ? (
                        <span className="inline-flex items-center gap-1.5">
                          <CalendarIcon className="size-4" />
                          {t('since', { year: foundedYear })}
                        </span>
                      ) : null}
                    </div>
                  ) : null}
                  {description ? <p className="line-clamp-3 text-sm whitespace-pre-line">{plainMarkdown(description)}</p> : null}
                  {genres?.length ? <AppGenreBadges genres={genres as MusicGenre[]} max={4} tone="brand" /> : null}
                </div>
              </div>
            </div>

            {asideNote}

            <AppButton type="submit" loading={saving} size="lg" className="w-full font-heading tracking-wider uppercase">
              {submitLabel}
            </AppButton>
            {onCancel ? (
              <AppButton intent="ghost" type="button" onClick={onCancel}>
                {t('common:actions.cancel')}
              </AppButton>
            ) : null}
          </>
        }
      >
        {appearance}

        <AppFormPanel title={t('form.sections.about')} wide>
          <AppField name="name" label={t('form.name')} />
          <AppTextareaField
            name="description"
            label={t('form.description')}
            rows={4}
            maxLength={bandDescriptionMaxLength}
            placeholder={t('form.descriptionPlaceholder')}
            description={t('form.descriptionHint', { max: bandDescriptionMaxLength })}
            hint={t('common:field.markdown')}
          />
        </AppFormPanel>

        <AppFormPanel title={t('form.sections.origin')}>
          <AppCityField
            name="city"
            label={t('form.city')}
            placeholder={t('form.cityPlaceholder')}
            clearLabel={t('form.cityNone')}
            search={searchCities} suggest={featuredCities}
            allowAbroad
          />
          <AppField name="foundedYear" inputMode="numeric" label={t('form.foundedYear')} placeholder={t('form.foundedYearPlaceholder')} />
          {withInstrument ? (
            <AppField name="instrument" label={t('form.instrument')} placeholder={t('form.instrumentPlaceholder')} />
          ) : null}
        </AppFormPanel>

        <AppFormPanel title={t('form.sections.links')}>
          <AppField name="instagram" type="url" label={t('form.instagram')} placeholder="https://instagram.com/..." />
          <AppField name="youtube" type="url" label={t('form.youtube')} placeholder="https://youtube.com/@..." />
          <AppField name="spotify" type="url" label={t('form.spotify')} placeholder="https://open.spotify.com/..." />
        </AppFormPanel>

        <AppFormPanel title={t('form.sections.genres')} wide>
          <AppChipsField
            name="genres"
            label={t('form.genres')}
            max={maxGenres}
            description={t('form.genresHint', { max: maxGenres })}
            options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))}
          />
        </AppFormPanel>
      </AppFormLayout>
    </AppForm>
  )
}
