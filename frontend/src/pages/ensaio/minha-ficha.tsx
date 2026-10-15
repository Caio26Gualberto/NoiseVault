import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowUpIcon, CarIcon, HouseIcon, PlusIcon, SpeakerIcon, XIcon } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { useAuth } from '@/auth/use-auth'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppChipsField, AppField, AppForm, AppSelectField, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { Checkbox } from '@/components/ui/checkbox'
import { ensaioPath } from '@/ensaio/frame'
import { cn } from '@/lib/utils'
import { formatCity } from '@/pages/perfil/city'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import { FlyerCard } from './components/flyer-card'
import { FlyerPhotoUploader } from './components/flyer-photo-uploader'
import { LocationControl } from './components/location-control'
import {
  Instrument,
  MusicianCommitment,
  MusicianGoal,
  SkillLevel,
  allDayPeriods,
  allInstruments,
  allWeekDays,
  commitmentKey,
  dayPeriodKey,
  defaultReachKm,
  goalKey,
  levelKey,
  reachOptions,
  useInstrumentLabel,
  useReachLabel,
  weekDayKey,
} from './ensaio-status'
import type { DayPeriod, WeekDay } from './ensaio-status'
import { getMyMusicianProfile, removeMusicianPhoto, saveMyMusicianProfile, uploadMusicianPhoto } from './services/ensaio-service'
import type { MusicianInputDto, MusicianProfileDto } from './services/ensaio-service'

const maxInstruments = 5
const maxLinks = 4
const maxGenres = 10
const otherValue = String(Instrument.other)
const anywhereValue = 'anywhere'
const sampleHosts = ['youtube.com', 'youtu.be', 'open.spotify.com', 'spotify.com', 'bandcamp.com', 'soundcloud.com']

function isSampleLink(value: string) {
  try {
    const url = new URL(value)
    const host = url.hostname.replace(/^(www|m)\./, '')
    return url.protocol === 'https:' && sampleHosts.some((allowed) => host === allowed || host.endsWith(`.${allowed}`))
  } catch {
    return false
  }
}

const schema = z.object({
  looking: z.boolean(),
  instruments: z
    .array(
      z
        .object({ instrument: z.string().min(1, 'validation.required'), otherName: z.string().trim().max(60, 'validation.instrumentLength'), level: z.string() })
        .refine((item) => item.instrument !== otherValue || item.otherName.length > 0, { path: ['otherName'], message: 'ensaio:validation.otherName' }),
    )
    .min(1, 'ensaio:validation.instrumentsMin')
    .max(maxInstruments),
  goal: z.string(),
  commitment: z.string(),
  reachKm: z.string(),
  availability: z.array(z.string()),
  hasGear: z.boolean(),
  hasTransport: z.boolean(),
  hasRehearsalSpace: z.boolean(),
  genres: z.array(z.number()).max(maxGenres, 'validation.genresMax'),
  links: z.array(z.object({ url: z.string().trim().refine((value) => value === '' || isSampleLink(value), 'ensaio:validation.sampleLink') })).max(maxLinks),
  influences: z.string().trim().max(300, 'ensaio:validation.influences'),
})

type FormValues = z.infer<typeof schema>
type StructureName = 'hasGear' | 'hasTransport' | 'hasRehearsalSpace'

const structureIcons: Record<StructureName, LucideIcon> = {
  hasGear: SpeakerIcon,
  hasTransport: CarIcon,
  hasRehearsalSpace: HouseIcon,
}

const slotKey = (day: WeekDay, period: DayPeriod) => `${day}-${period}`

function toValues(profile: MusicianProfileDto): FormValues {
  return {
    looking: profile.exists ? profile.isLookingForBand : true,
    instruments: profile.instruments.length
      ? profile.instruments.map((item) => ({ instrument: String(item.instrument), otherName: item.otherName ?? '', level: String(item.level) }))
      : [{ instrument: '', otherName: '', level: String(SkillLevel.intermediate) }],
    goal: String(profile.goal),
    commitment: String(profile.commitment),
    reachKm: profile.reachKm === null ? anywhereValue : String(profile.reachKm),
    availability: profile.availability.map((slot) => slotKey(slot.day, slot.period)),
    hasGear: profile.hasGear,
    hasTransport: profile.hasTransport,
    hasRehearsalSpace: profile.hasRehearsalSpace,
    genres: profile.genres,
    links: profile.sampleLinks.map((url) => ({ url })),
    influences: profile.influences ?? '',
  }
}

function toInput(values: FormValues): MusicianInputDto {
  return {
    isLookingForBand: values.looking,
    goal: Number(values.goal) as MusicianGoal,
    commitment: Number(values.commitment) as MusicianCommitment,
    reachKm: values.reachKm === anywhereValue ? null : Number(values.reachKm),
    hasGear: values.hasGear,
    hasTransport: values.hasTransport,
    hasRehearsalSpace: values.hasRehearsalSpace,
    influences: values.influences || null,
    instruments: values.instruments.map((item) => {
      const instrument = Number(item.instrument) as Instrument
      return { instrument, otherName: instrument === Instrument.other ? item.otherName : null, level: Number(item.level) as SkillLevel }
    }),
    genres: values.genres as MusicGenre[],
    availability: values.availability.map((key) => {
      const [day, period] = key.split('-').map(Number)
      return { day: day as WeekDay, period: period as DayPeriod }
    }),
    sampleLinks: values.links.map((link) => link.url).filter(Boolean),
  }
}

function Panel({ title, description, className, children }: { title: string; description?: string; className?: string; children: ReactNode }) {
  return (
    <section className={cn('flex min-w-0 flex-col gap-3 rounded-xl border bg-card p-4 md:p-5', className)}>
      <div className="flex flex-col gap-0.5">
        <h2 className="font-heading text-base font-bold tracking-wider uppercase">{title}</h2>
        {description ? <p className="app-meta">{description}</p> : null}
      </div>
      {children}
    </section>
  )
}

export function EnsaioMinhaFichaPage() {
  const { t } = useTranslation('ensaio')
  const { user } = useAuth()
  const instrumentLabel = useInstrumentLabel()
  const reachLabel = useReachLabel()
  const genreLabel = useMusicGenreLabel()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [exists, setExists] = useState(false)
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [pendingPhoto, setPendingPhoto] = useState<string | null>(null)

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: toValues(emptyProfile) })
  const instruments = useFieldArray({ control: form.control, name: 'instruments' })
  const links = useFieldArray({ control: form.control, name: 'links' })
  const instrumentValues = useWatch({ control: form.control, name: 'instruments' })
  const availability = useWatch({ control: form.control, name: 'availability' })
  const looking = useWatch({ control: form.control, name: 'looking' })
  const genres = useWatch({ control: form.control, name: 'genres' })
  const structure = useWatch({ control: form.control, name: ['hasGear', 'hasTransport', 'hasRehearsalSpace'] })
  const instrumentsError = form.formState.errors.instruments?.root?.message ?? form.formState.errors.instruments?.message
  const onPreviewChange = useCallback((url: string | null) => setPendingPhoto(url), [])

  useEffect(() => {
    let cancelled = false
    getMyMusicianProfile()
      .then((profile) => {
        if (cancelled) return
        form.reset(toValues(profile))
        setExists(profile.exists)
        setPhotoUrl(profile.photoUrl)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [form])

  const submit = async (values: FormValues) => {
    setSaving(true)
    try {
      const saved = await saveMyMusicianProfile(toInput(values))
      form.reset(toValues(saved))
      setExists(true)
      setPhotoUrl(saved.photoUrl)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const toggleSlot = (key: string) => {
    const next = availability.includes(key) ? availability.filter((item) => item !== key) : [...availability, key]
    form.setValue('availability', next, { shouldDirty: true })
  }

  if (loading || !user) return <AppSkeleton preset="lines" count={10} className="mx-auto max-w-6xl" />

  const optionsOf = <T extends number>(values: readonly T[], label: (value: T) => string) => values.map((value) => ({ value: String(value), label: label(value) }))
  const main = instrumentValues[0]
  const previewTitle = main?.instrument
    ? instrumentLabel(Number(main.instrument) as Instrument, main.instrument === otherValue ? main.otherName : null)
    : t('myProfile.previewInstrument')
  const previewMeta = [formatCity(user), genres[0] !== undefined ? genreLabel(genres[0] as MusicGenre) : null].filter(Boolean).join(' · ') || null
  const structureNames: StructureName[] = ['hasGear', 'hasTransport', 'hasRehearsalSpace']

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader
        title={t('myProfile.title')}
        description={t('myProfile.description')}
        actions={
          exists ? (
            <AppButton asChild intent="secondary" size="sm">
              <Link to={ensaioPath(`/musicos/${user.nickname}`)}>{t('myProfile.view')}</Link>
            </AppButton>
          ) : null
        }
      />

      <AppForm form={form} onSubmit={submit} className="grid gap-6 lg:grid-cols-[340px_minmax(0,1fr)] lg:items-start">
        <aside className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-24">
          <div className="flex flex-col gap-2">
            <span className="app-meta">{t('myProfile.preview')}</span>
            <FlyerCard
              index={0}
              title={previewTitle}
              name={user.name}
              meta={previewMeta}
              photoUrl={pendingPhoto ?? photoUrl ?? user.avatarUrl}
            />
          </div>

          <div className="rounded-xl border bg-card p-4">
            <FlyerPhotoUploader
              photoUrl={photoUrl}
              fallbackUrl={user.avatarUrl}
              disabled={!exists}
              showPreview={false}
              onPreviewChange={onPreviewChange}
              onUpload={async (file) => setPhotoUrl((await uploadMusicianPhoto(file)).photoUrl)}
              onRemove={async () => setPhotoUrl((await removeMusicianPhoto()).photoUrl)}
            />
          </div>

          <label className={cn('flex items-start gap-3 rounded-xl border p-4', looking ? 'border-primary/40 bg-primary/5' : 'bg-card')}>
            <Checkbox className="mt-0.5" checked={looking} onCheckedChange={(checked) => form.setValue('looking', checked === true, { shouldDirty: true })} />
            <span className="flex flex-col gap-1">
              <span className="font-semibold">{t('myProfile.looking')}</span>
              <span className="app-meta">{t('myProfile.lookingHint')}</span>
            </span>
          </label>

          <div className="flex flex-col gap-2 rounded-xl border bg-card p-4">
            <span className="text-sm font-medium">{t('location.title')}</span>
            <span className="app-meta">{t('location.description')}</span>
            <LocationControl />
          </div>

          <AppButton type="submit" loading={saving} size="lg" className="w-full font-heading tracking-wider uppercase">
            {t('myProfile.save')}
          </AppButton>
        </aside>

        <div className="grid min-w-0 gap-4 xl:grid-cols-2">
          <Panel title={t('myProfile.instruments')} description={t('myProfile.instrumentsHint')} className="xl:col-span-2">
            <div className="flex flex-col gap-3">
              {instruments.fields.map((field, index) => (
                <div key={field.id} className="flex flex-col gap-3 rounded-lg border p-3 md:flex-row md:items-end">
                  <span className="hidden size-9 shrink-0 items-center justify-center self-center rounded-full bg-muted font-heading font-bold md:flex">{index + 1}</span>
                  <div className="grid flex-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    <AppSelectField name={`instruments.${index}.instrument`} label={t('myProfile.instrument')} options={optionsOf(allInstruments, (value) => instrumentLabel(value))} />
                    <AppSelectField name={`instruments.${index}.level`} label={t('myProfile.level')} options={optionsOf(Object.values(SkillLevel), (value) => t(`levels.${levelKey[value]}`))} />
                    {instrumentValues[index]?.instrument === otherValue ? <AppField name={`instruments.${index}.otherName`} label={t('myProfile.otherName')} /> : null}
                  </div>
                  <div className="flex gap-1">
                    {index > 0 ? (
                      <AppIconButton type="button" label={t('myProfile.moveUp')} onClick={() => instruments.move(index, index - 1)}>
                        <ArrowUpIcon />
                      </AppIconButton>
                    ) : null}
                    {instruments.fields.length > 1 ? (
                      <AppIconButton type="button" label={t('myProfile.removeInstrument')} onClick={() => instruments.remove(index)}>
                        <XIcon />
                      </AppIconButton>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
            {instrumentsError ? (
              <p role="alert" className="text-[13px] text-destructive">
                {t(instrumentsError, { ns: 'common', defaultValue: instrumentsError })}
              </p>
            ) : null}
            {instruments.fields.length < maxInstruments ? (
              <AppButton
                type="button"
                intent="secondary"
                size="sm"
                className="self-start"
                onClick={() => instruments.append({ instrument: '', otherName: '', level: String(SkillLevel.intermediate) })}
              >
                <PlusIcon />
                {t('myProfile.addInstrument')}
              </AppButton>
            ) : null}
          </Panel>

          <Panel title={t('myProfile.wants')}>
            <AppSelectField name="goal" label={t('myProfile.goal')} options={optionsOf(Object.values(MusicianGoal), (value) => t(`goals.${goalKey[value]}`))} />
            <AppSelectField
              name="commitment"
              label={t('myProfile.commitment')}
              options={optionsOf(Object.values(MusicianCommitment), (value) => t(`commitments.${commitmentKey[value]}`))}
            />
            <AppSelectField
              name="reachKm"
              label={t('myProfile.reach')}
              description={t('myProfile.reachHint')}
              options={reachOptions.map((km) => ({ value: km === null ? anywhereValue : String(km), label: reachLabel(km) }))}
            />
          </Panel>

          <Panel title={t('myProfile.structure')} description={t('myProfile.structureHint')}>
            <div className="grid gap-2">
              {structureNames.map((name, index) => {
                const Icon = structureIcons[name]
                const active = structure[index]
                return (
                  <button
                    key={name}
                    type="button"
                    aria-pressed={active}
                    onClick={() => form.setValue(name, !active, { shouldDirty: true })}
                    className={cn(
                      'flex h-12 items-center gap-3 rounded-lg border px-3 text-left text-sm transition-colors focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
                      active ? 'border-primary bg-primary/10 font-semibold' : 'hover:bg-accent',
                    )}
                  >
                    <Icon className={cn('size-5', active ? 'text-primary' : 'text-muted-foreground')} aria-hidden="true" />
                    {t(`profile.${name}`)}
                  </button>
                )
              })}
            </div>
          </Panel>

          <Panel title={t('myProfile.availability')} description={t('myProfile.availabilityHint')} className="xl:col-span-2">
            <div role="group" aria-label={t('myProfile.availability')} className="grid grid-cols-[3.25rem_repeat(7,minmax(0,1fr))] gap-1 sm:grid-cols-[4.5rem_repeat(7,minmax(0,1fr))] sm:gap-1.5">
              <span />
              {allWeekDays.map((day) => (
                <span key={day} className="pb-1 text-center font-heading text-sm font-medium tracking-wide uppercase">
                  {t(`weekDays.${weekDayKey[day]}`)}
                </span>
              ))}
              {allDayPeriods.map((period) => (
                <div key={period} className="contents">
                  <span className="flex items-center text-sm font-medium">{t(`periods.${dayPeriodKey[period]}`)}</span>
                  {allWeekDays.map((day) => {
                    const key = slotKey(day, period)
                    const active = availability.includes(key)
                    return (
                      <button
                        key={key}
                        type="button"
                        aria-pressed={active}
                        aria-label={`${t(`weekDays.${weekDayKey[day]}`)} ${t(`periods.${dayPeriodKey[period]}`)}`}
                        onClick={() => toggleSlot(key)}
                        className={cn(
                          'h-10 rounded-md border transition-colors focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none',
                          active ? 'border-primary bg-primary text-primary-foreground' : 'hover:bg-accent',
                        )}
                      />
                    )
                  })}
                </div>
              ))}
            </div>
          </Panel>

          <Panel title={t('myProfile.genres')} className="xl:col-span-2">
            <AppChipsField name="genres" label={t('myProfile.genresLabel')} max={maxGenres} options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))} />
          </Panel>

          <Panel title={t('myProfile.links')} description={t('myProfile.linksHint')}>
            {links.fields.map((field, index) => (
              <div key={field.id} className="flex items-end gap-2">
                <div className="min-w-0 flex-1">
                  <AppField name={`links.${index}.url`} label={`${t('myProfile.link')} ${index + 1}`} type="url" placeholder="https://" />
                </div>
                <AppIconButton type="button" label={t('myProfile.removeLink')} onClick={() => links.remove(index)}>
                  <XIcon />
                </AppIconButton>
              </div>
            ))}
            {links.fields.length < maxLinks ? (
              <AppButton type="button" intent="secondary" size="sm" className="self-start" onClick={() => links.append({ url: '' })}>
                <PlusIcon />
                {t('myProfile.addLink')}
              </AppButton>
            ) : null}
          </Panel>

          <Panel title={t('myProfile.influences')}>
            <AppTextareaField name="influences" label={t('myProfile.influencesLabel')} placeholder={t('myProfile.influencesPlaceholder')} rows={5} maxLength={300} />
          </Panel>
        </div>
      </AppForm>
    </div>
  )
}

const emptyProfile: MusicianProfileDto = {
  exists: false,
  isMine: true,
  person: { id: 0, name: '', nickname: '', avatarUrl: null },
  photoUrl: null,
  city: null,
  cityName: null,
  isLookingForBand: true,
  lookingSince: null,
  distanceKm: null,
  goal: MusicianGoal.either,
  commitment: MusicianCommitment.playLive,
  reachKm: defaultReachKm,
  hasGear: false,
  hasTransport: false,
  hasRehearsalSpace: false,
  influences: null,
  instruments: [],
  genres: [],
  availability: [],
  sampleLinks: [],
}
