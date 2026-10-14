import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'

import { useAuth } from '@/auth/use-auth'
import { AppBannerEditor } from '@/components/app-banner-editor'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppCityField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { getMyBands } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'
import { cityValueSchema, emptyCityValue, toCityInput, toCityValue } from '@/pages/perfil/city'
import { searchCities } from '@/pages/perfil/services/cidades-service'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { LineupEditor } from './components/lineup-editor'
import type { LineupItem } from './components/lineup-editor'
import { EventStatus } from './event-status'
import {
  createEvent,
  getEvent,
  removeEventBanner,
  updateEvent,
  updateEventBannerPosition,
  uploadEventBanner,
} from './services/eventos-service'
import type { EventInputDto } from './services/eventos-service'

const maxGenres = 5
const asSelf = 'self'

const schema = z
  .object({
    title: z.string().trim().min(3, 'validation.required').max(120),
    description: z.string().trim().max(5000),
    startsAt: z.string().min(1, 'validation.required'),
    endsAt: z.string(),
    venue: z.string().trim().min(2, 'validation.required').max(120),
    city: cityValueSchema,
    genres: z.array(z.number()).max(maxGenres),
    ticketsUrl: z.string().trim().max(500).refine((value) => !value || /^https?:\/\//i.test(value), 'validation.url'),
    price: z.string().trim().refine((value) => !value || !Number.isNaN(Number(value.replace(',', '.'))), 'validation.number'),
    capacity: z.string().trim().refine((value) => !value || /^\d+$/.test(value), 'validation.number'),
  })
  .refine((values) => values.city.city !== null || values.city.cityName.length >= 2, { path: ['city'], message: 'validation.cityRequired' })
  .refine((values) => !values.endsAt || new Date(values.endsAt) > new Date(values.startsAt), { path: ['endsAt'], message: 'validation.endsAfterStart' })

type FormValues = z.infer<typeof schema>

type PendingBanner = { file: File; position: number; previewUrl: string }

function toLocalInput(iso: string | null): string {
  if (!iso) return ''
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function toInput(values: FormValues, lineup: LineupItem[]): EventInputDto {
  return {
    title: values.title,
    description: values.description || null,
    startsAt: new Date(values.startsAt).toISOString(),
    endsAt: values.endsAt ? new Date(values.endsAt).toISOString() : null,
    venue: values.venue,
    ...toCityInput(values.city),
    genres: values.genres as MusicGenre[],
    ticketsUrl: values.ticketsUrl || null,
    price: values.price ? Number(values.price.replace(',', '.')) : null,
    capacity: values.capacity ? Number(values.capacity) : null,
    lineup: lineup.map((item) => (item.band ? { bandId: item.band.id, name: null } : { bandId: null, name: item.name })),
  }
}

export function EventoFormPage() {
  const { t } = useTranslation('eventos')
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const eventId = id ? Number(id) : null
  const [loading, setLoading] = useState(eventId !== null)
  const [saving, setSaving] = useState(false)
  const [lineup, setLineup] = useState<LineupItem[]>([])
  const [bands, setBands] = useState<BandSummaryDto[]>([])
  const [publishAs, setPublishAs] = useState(asSelf)
  const [banner, setBanner] = useState<{ url: string | null; position: number }>({ url: null, position: 50 })
  const [pending, setPending] = useState<PendingBanner | null>(null)
  const genreLabel = useMusicGenreLabel()

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      startsAt: '',
      endsAt: '',
      venue: '',
      city: emptyCityValue,
      genres: [],
      ticketsUrl: '',
      price: '',
      capacity: '',
    },
  })

  useEffect(() => {
    if (eventId !== null) return
    let cancelled = false
    getMyBands()
      .then((data) => {
        if (!cancelled) setBands(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [eventId])

  useEffect(() => {
    if (eventId === null) return
    let cancelled = false
    getEvent(eventId)
      .then((event) => {
        if (cancelled) return
        if (!event.canManage || event.status === EventStatus.cancelled) {
          navigate(`/eventos/${event.id}`, { replace: true })
          return
        }
        form.reset({
          title: event.title,
          description: event.description ?? '',
          startsAt: toLocalInput(event.startsAt),
          endsAt: toLocalInput(event.endsAt),
          venue: event.venue,
          city: toCityValue(event),
          genres: event.genres,
          ticketsUrl: event.ticketsUrl ?? '',
          price: event.price?.toString().replace('.', ',') ?? '',
          capacity: event.capacity?.toString() ?? '',
        })
        setLineup(event.lineup.map((item) => ({ key: crypto.randomUUID(), band: item.band, name: item.name })))
        setBanner({ url: event.bannerUrl, position: event.bannerPosition })
      })
      .catch(() => navigate('/eventos', { replace: true }))
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [eventId, form, navigate])

  useEffect(() => () => {
    if (pending) URL.revokeObjectURL(pending.previewUrl)
  }, [pending])

  const genres = useWatch({ control: form.control, name: 'genres' })

  const submit = async (values: FormValues) => {
    setSaving(true)
    try {
      const input = toInput(values, lineup)
      if (eventId === null) {
        const created = await createEvent({ ...input, bandSlug: publishAs === asSelf ? null : publishAs })
        if (pending) await uploadEventBanner(created.id, pending.file, pending.position).catch(() => undefined)
        navigate(`/eventos/${created.id}`, { replace: true })
      } else {
        await updateEvent(eventId, input)
        navigate(`/eventos/${eventId}`, { replace: true })
      }
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const bannerHandlers =
    eventId === null
      ? {
          onUpload: async (file: File, position: number) => {
            setPending({ file, position, previewUrl: URL.createObjectURL(file) })
            return { bannerPosition: position }
          },
          onPosition: async (position: number) => {
            setPending((current) => (current ? { ...current, position } : current))
            return { bannerPosition: position }
          },
          onRemove: async () => {
            setPending(null)
            return { bannerPosition: 50 }
          },
        }
      : {
          onUpload: async (file: File, position: number) => {
            const saved = await uploadEventBanner(eventId, file, position)
            setBanner({ url: saved.bannerUrl, position: saved.bannerPosition })
            return saved
          },
          onPosition: async (position: number) => {
            const saved = await updateEventBannerPosition(eventId, position)
            setBanner({ url: saved.bannerUrl, position: saved.bannerPosition })
            return saved
          },
          onRemove: async () => {
            const saved = await removeEventBanner(eventId)
            setBanner({ url: saved.bannerUrl, position: saved.bannerPosition })
            return saved
          },
        }

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

      <AppCard title={t('form.bannerTitle')} description={t('form.bannerHint')}>
        <AppBannerEditor
          bannerUrl={eventId === null ? (pending?.previewUrl ?? null) : banner.url}
          bannerPosition={eventId === null ? (pending?.position ?? 50) : banner.position}
          {...bannerHandlers}
        />
      </AppCard>

      <AppCard>
        <AppForm form={form} onSubmit={submit}>
          {eventId === null && bands.length > 0 && user ? (
            <div className="flex flex-col gap-1.5">
              <Label>{t('form.fields.publishAs')}</Label>
              <Select value={publishAs} onValueChange={setPublishAs}>
                <SelectTrigger className="h-11 md:h-10" aria-label={t('form.fields.publishAs')}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={asSelf}>{t('form.asSelf', { name: user.name })}</SelectItem>
                  {bands.map((band) => (
                    <SelectItem key={band.slug} value={band.slug}>
                      {t('form.asBand', { name: band.name })}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          ) : null}

          <AppField name="title" label={t('form.fields.title')} />
          <AppTextareaField name="description" label={t('form.fields.description')} rows={5} />
          <div className="grid gap-4 md:grid-cols-2">
            <AppField name="startsAt" label={t('form.fields.startsAt')} type="datetime-local" />
            <AppField name="endsAt" label={t('form.fields.endsAt')} type="datetime-local" />
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <AppField name="venue" label={t('form.fields.venue')} placeholder={t('form.venuePlaceholder')} />
            <AppCityField
              name="city"
              label={t('form.fields.city')}
              placeholder={t('form.cityPlaceholder')}
              search={searchCities}
              allowAbroad
            />
          </div>

          <LineupEditor value={lineup} onChange={setLineup} />

          <fieldset className="flex flex-col gap-2">
            <legend className="text-sm font-medium">{t('form.fields.genres')}</legend>
            <p className="app-meta">{t('form.genresHint', { max: maxGenres })}</p>
            <div className="grid grid-cols-2 gap-x-4 gap-y-2 sm:grid-cols-3">
              {allMusicGenres.map((genre) => (
                <label key={genre} className="flex items-center gap-2 text-sm">
                  <Checkbox
                    checked={genres.includes(genre)}
                    disabled={!genres.includes(genre) && genres.length >= maxGenres}
                    onCheckedChange={(checked) => toggleGenre(genre, checked === true)}
                  />
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
            <AppField name="price" label={t('form.fields.price')} inputMode="decimal" placeholder={t('form.pricePlaceholder')} />
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
