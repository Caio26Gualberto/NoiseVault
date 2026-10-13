import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'

import { feedback } from '@/api/feedback-sink'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { AbcCity, allAbcCities, useAbcCityLabel } from '@/pages/perfil/cities'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { createEvent, getEvent, updateEvent } from './services/eventos-service'
import type { EventInputDto } from './services/eventos-service'

const schema = z.object({
  title: z.string().trim().min(3, 'validation.required').max(120),
  description: z.string().trim().min(10, 'validation.required'),
  startsAt: z.string().min(1, 'validation.required'),
  endsAt: z.string(),
  venue: z.string().trim().min(2, 'validation.required'),
  city: z.number(),
  genres: z.array(z.number()).min(1, 'validation.required'),
  ticketsUrl: z.string().trim(),
  price: z.string().trim(),
  capacity: z.string().trim(),
})

type FormValues = z.infer<typeof schema>

function toLocalInput(iso: string | null): string {
  if (!iso) return ''
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function toInput(values: FormValues): EventInputDto {
  return {
    title: values.title,
    description: values.description,
    startsAt: new Date(values.startsAt).toISOString(),
    endsAt: values.endsAt ? new Date(values.endsAt).toISOString() : null,
    venue: values.venue,
    city: values.city as AbcCity,
    genres: values.genres as MusicGenre[],
    ticketsUrl: values.ticketsUrl || null,
    price: values.price ? Number(values.price.replace(',', '.')) : null,
    capacity: values.capacity ? Number(values.capacity) : null,
  }
}

export function EventoFormPage() {
  const { t } = useTranslation('eventos')
  const { id } = useParams()
  const navigate = useNavigate()
  const eventId = id ? Number(id) : null
  const [loading, setLoading] = useState(eventId !== null)
  const [saving, setSaving] = useState(false)
  const genreLabel = useMusicGenreLabel()
  const cityLabel = useAbcCityLabel()

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      startsAt: '',
      endsAt: '',
      venue: '',
      city: AbcCity.santoAndre,
      genres: [],
      ticketsUrl: '',
      price: '',
      capacity: '',
    },
  })

  useEffect(() => {
    if (eventId === null) return
    let cancelled = false
    getEvent(eventId)
      .then((event) => {
        if (cancelled || !event) return
        form.reset({
          title: event.title,
          description: event.description,
          startsAt: toLocalInput(event.startsAt),
          endsAt: toLocalInput(event.endsAt),
          venue: event.venue,
          city: event.city,
          genres: event.genres,
          ticketsUrl: event.ticketsUrl ?? '',
          price: event.price?.toString() ?? '',
          capacity: event.capacity?.toString() ?? '',
        })
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [eventId, form])

  const submit = async (values: FormValues) => {
    setSaving(true)
    try {
      const input = toInput(values)
      const saved = eventId === null ? await createEvent(input) : await updateEvent(eventId, input)
      if (!saved) return
      feedback.success('', t(eventId === null ? 'form.created' : 'form.updated'))
      navigate(`/eventos/${saved.id}`, { replace: true })
    } finally {
      setSaving(false)
    }
  }

  const genres = useWatch({ control: form.control, name: 'genres' })
  const city = useWatch({ control: form.control, name: 'city' })

  if (loading) {
    return <AppSkeleton preset="lines" count={8} className="app-page-enter mx-auto max-w-2xl" />
  }
  const genreError = form.formState.errors.genres?.message

  const toggleGenre = (genre: MusicGenre, checked: boolean) => {
    const next = checked ? [...genres, genre] : genres.filter((g) => g !== genre)
    form.setValue('genres', next, { shouldValidate: true })
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader title={t(eventId === null ? 'form.createTitle' : 'form.editTitle')} description={t('form.description')} />

      <AppCard>
        <AppForm form={form} onSubmit={submit}>
          <AppField name="title" label={t('form.fields.title')} />
          <AppTextareaField name="description" label={t('form.fields.description')} rows={5} />
          <div className="grid gap-4 md:grid-cols-2">
            <AppField name="startsAt" label={t('form.fields.startsAt')} type="datetime-local" />
            <AppField name="endsAt" label={t('form.fields.endsAt')} type="datetime-local" />
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <AppField name="venue" label={t('form.fields.venue')} />
            <div className="flex flex-col gap-1.5">
              <Label>{t('form.fields.city')}</Label>
              <Select value={String(city)} onValueChange={(value) => form.setValue('city', Number(value))}>
                <SelectTrigger className="h-11 md:h-10">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {allAbcCities.map((option) => (
                    <SelectItem key={option} value={String(option)}>
                      {cityLabel(option)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <fieldset className="flex flex-col gap-2">
            <legend className="text-sm font-medium">{t('form.fields.genres')}</legend>
            <div className="grid grid-cols-2 gap-x-4 gap-y-2 sm:grid-cols-3">
              {allMusicGenres.map((genre) => (
                <label key={genre} className="flex items-center gap-2 text-sm">
                  <Checkbox checked={genres.includes(genre)} onCheckedChange={(checked) => toggleGenre(genre, checked === true)} />
                  {genreLabel(genre)}
                </label>
              ))}
            </div>
            {genreError ? (
              <p role="alert" className="text-[13px] text-destructive">
                {t(genreError, { ns: 'common', defaultValue: genreError })}
              </p>
            ) : null}
          </fieldset>

          <div className="grid gap-4 md:grid-cols-3">
            <AppField name="price" label={t('form.fields.price')} inputMode="decimal" placeholder="0,00" />
            <AppField name="capacity" label={t('form.fields.capacity')} inputMode="numeric" />
            <AppField name="ticketsUrl" label={t('form.fields.ticketsUrl')} type="url" placeholder="https://" />
          </div>

          <div className="flex gap-2">
            <AppButton type="submit" loading={saving}>
              {t(eventId === null ? 'form.submitCreate' : 'form.submitEdit')}
            </AppButton>
            <AppButton intent="ghost" type="button" onClick={() => navigate(-1)}>
              {t('common:actions.cancel')}
            </AppButton>
          </div>
        </AppForm>
      </AppCard>
    </div>
  )
}
