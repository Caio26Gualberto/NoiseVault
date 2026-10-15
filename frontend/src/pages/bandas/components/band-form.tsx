import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppChipsField, AppCityField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { maxGenres } from '@/pages/meu-perfil/profile-validators'
import { featuredCities, searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'

import { bandDescriptionMaxLength, bandFormSchema } from '../band-validators'
import type { BandFormValues } from '../band-validators'

type BandFormProps = {
  defaultValues: BandFormValues
  withInstrument?: boolean
  submitLabel: string
  saving: boolean
  onSubmit: (values: BandFormValues) => Promise<void>
}

export function BandForm({ defaultValues, withInstrument = false, submitLabel, saving, onSubmit }: BandFormProps) {
  const { t } = useTranslation('bandas')
  const genreLabel = useMusicGenreLabel()

  const form = useForm<BandFormValues>({
    resolver: zodResolver(bandFormSchema),
    defaultValues,
  })

  const submit = async (values: BandFormValues) => {
    await onSubmit(values)
    form.reset(values)
  }

  return (
    <AppForm form={form} onSubmit={submit}>
      <AppField name="name" label={t('form.name')} />
      <AppTextareaField
        name="description"
        label={t('form.description')}
        rows={4}
        maxLength={bandDescriptionMaxLength}
        placeholder={t('form.descriptionPlaceholder')}
        description={t('form.descriptionHint', { max: bandDescriptionMaxLength })}
      />
      <div className="grid gap-4 md:grid-cols-2">
        <AppCityField
          name="city"
          label={t('form.city')}
          placeholder={t('form.cityPlaceholder')}
          clearLabel={t('form.cityNone')}
          search={searchCities} suggest={featuredCities}
          allowAbroad
        />
        <AppField name="foundedYear" inputMode="numeric" label={t('form.foundedYear')} placeholder={t('form.foundedYearPlaceholder')} />
      </div>
      {withInstrument ? (
        <AppField name="instrument" label={t('form.instrument')} placeholder={t('form.instrumentPlaceholder')} />
      ) : null}
      <AppChipsField
        name="genres"
        label={t('form.genres')}
        max={maxGenres}
        description={t('form.genresHint', { max: maxGenres })}
        options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))}
      />
      <div className="grid gap-4 md:grid-cols-3">
        <AppField name="instagram" type="url" label={t('form.instagram')} placeholder="https://instagram.com/..." />
        <AppField name="youtube" type="url" label={t('form.youtube')} placeholder="https://youtube.com/@..." />
        <AppField name="spotify" type="url" label={t('form.spotify')} placeholder="https://open.spotify.com/..." />
      </div>
      <AppButton type="submit" loading={saving} className="self-start">
        {submitLabel}
      </AppButton>
    </AppForm>
  )
}
