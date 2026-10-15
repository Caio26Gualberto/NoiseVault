import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowUpIcon, PlusIcon, XIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useFieldArray, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { useAuth } from '@/auth/use-auth'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppChipsField, AppField, AppForm, AppSelectField, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { Checkbox } from '@/components/ui/checkbox'
import { ensaioPath } from '@/ensaio/frame'
import { allMusicGenres, useMusicGenreLabel } from '@/pages/perfil/genres'
import type { MusicGenre } from '@/pages/perfil/genres'

import {
  Instrument,
  MusicianCommitment,
  MusicianGoal,
  Reach,
  SkillLevel,
  allDayPeriods,
  allInstruments,
  allWeekDays,
  commitmentKey,
  dayPeriodKey,
  goalKey,
  levelKey,
  reachKey,
  useInstrumentLabel,
  weekDayKey,
} from './ensaio-status'
import type { DayPeriod, WeekDay } from './ensaio-status'
import { getMyMusicianProfile, saveMyMusicianProfile } from './services/ensaio-service'
import type { MusicianInputDto, MusicianProfileDto } from './services/ensaio-service'

const maxInstruments = 5
const maxLinks = 4
const maxGenres = 10
const otherValue = String(Instrument.other)
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
  reach: z.string(),
  availability: z.array(z.string()),
  hasGear: z.boolean(),
  hasTransport: z.boolean(),
  hasRehearsalSpace: z.boolean(),
  genres: z.array(z.number()).max(maxGenres, 'validation.genresMax'),
  links: z.array(z.object({ url: z.string().trim().refine((value) => value === '' || isSampleLink(value), 'ensaio:validation.sampleLink') })).max(maxLinks),
  influences: z.string().trim().max(300, 'ensaio:validation.influences'),
})

type FormValues = z.infer<typeof schema>

const slotKey = (day: WeekDay, period: DayPeriod) => `${day}-${period}`

function toValues(profile: MusicianProfileDto): FormValues {
  return {
    looking: profile.exists ? profile.isLookingForBand : true,
    instruments: profile.instruments.length
      ? profile.instruments.map((item) => ({ instrument: String(item.instrument), otherName: item.otherName ?? '', level: String(item.level) }))
      : [{ instrument: '', otherName: '', level: String(SkillLevel.intermediate) }],
    goal: String(profile.goal),
    commitment: String(profile.commitment),
    reach: String(profile.reach),
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
    reach: Number(values.reach) as Reach,
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

export function EnsaioMinhaFichaPage() {
  const { t } = useTranslation('ensaio')
  const { user } = useAuth()
  const instrumentLabel = useInstrumentLabel()
  const genreLabel = useMusicGenreLabel()
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [exists, setExists] = useState(false)

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: toValues(emptyProfile) })
  const instruments = useFieldArray({ control: form.control, name: 'instruments' })
  const links = useFieldArray({ control: form.control, name: 'links' })
  const instrumentValues = useWatch({ control: form.control, name: 'instruments' })
  const availability = useWatch({ control: form.control, name: 'availability' })
  const looking = useWatch({ control: form.control, name: 'looking' })
  const instrumentsError = form.formState.errors.instruments?.root?.message ?? form.formState.errors.instruments?.message

  useEffect(() => {
    let cancelled = false
    getMyMusicianProfile()
      .then((profile) => {
        if (cancelled) return
        form.reset(toValues(profile))
        setExists(profile.exists)
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
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const toggleSlot = (key: string, checked: boolean) => {
    const next = checked ? [...availability, key] : availability.filter((item) => item !== key)
    form.setValue('availability', next, { shouldDirty: true })
  }

  if (loading) return <AppSkeleton preset="lines" count={10} className="mx-auto max-w-2xl" />

  const optionsOf = <T extends number>(values: readonly T[], label: (value: T) => string) => values.map((value) => ({ value: String(value), label: label(value) }))

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader
        title={t('myProfile.title')}
        description={t('myProfile.description')}
        actions={
          exists && user ? (
            <AppButton asChild intent="secondary" size="sm">
              <Link to={ensaioPath(`/musicos/${user.nickname}`)}>{t('myProfile.view')}</Link>
            </AppButton>
          ) : null
        }
      />

      <AppCard>
        <AppForm form={form} onSubmit={submit}>
          <label className="flex items-start gap-3 rounded-lg border border-primary/40 bg-primary/5 p-4">
            <Checkbox
              className="mt-0.5"
              checked={looking}
              onCheckedChange={(checked) => form.setValue('looking', checked === true, { shouldDirty: true })}
            />
            <span className="flex flex-col gap-1">
              <span className="font-semibold">{t('myProfile.looking')}</span>
              <span className="app-meta">{t('myProfile.lookingHint')}</span>
            </span>
          </label>

          <fieldset className="flex min-w-0 flex-col gap-3">
            <legend className="text-sm font-medium">{t('myProfile.instruments')}</legend>
            <p className="app-meta">{t('myProfile.instrumentsHint')}</p>
            {instruments.fields.map((field, index) => (
              <div key={field.id} className="flex flex-col gap-3 rounded-lg border p-3 sm:flex-row sm:items-end">
                <div className="grid flex-1 gap-3 sm:grid-cols-2">
                  <AppSelectField
                    name={`instruments.${index}.instrument`}
                    label={t('myProfile.instrument')}
                    options={optionsOf(allInstruments, (value) => instrumentLabel(value))}
                  />
                  <AppSelectField name={`instruments.${index}.level`} label={t('myProfile.level')} options={optionsOf(Object.values(SkillLevel), (value) => t(`levels.${levelKey[value]}`))} />
                  {instrumentValues[index]?.instrument === otherValue ? <AppField name={`instruments.${index}.otherName`} label={t('myProfile.otherName')} /> : null}
                </div>
                <div className="flex gap-1">
                  {index > 0 ? (
                    <AppIconButton label={t('myProfile.moveUp')} onClick={() => instruments.move(index, index - 1)}>
                      <ArrowUpIcon />
                    </AppIconButton>
                  ) : null}
                  {instruments.fields.length > 1 ? (
                    <AppIconButton label={t('myProfile.removeInstrument')} onClick={() => instruments.remove(index)}>
                      <XIcon />
                    </AppIconButton>
                  ) : null}
                </div>
              </div>
            ))}
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
          </fieldset>

          <div className="grid gap-4 md:grid-cols-3">
            <AppSelectField name="goal" label={t('myProfile.goal')} options={optionsOf(Object.values(MusicianGoal), (value) => t(`goals.${goalKey[value]}`))} />
            <AppSelectField
              name="commitment"
              label={t('myProfile.commitment')}
              options={optionsOf(Object.values(MusicianCommitment), (value) => t(`commitments.${commitmentKey[value]}`))}
            />
            <AppSelectField name="reach" label={t('myProfile.reach')} options={optionsOf(Object.values(Reach), (value) => t(`reach.${reachKey[value]}`))} />
          </div>

          <fieldset className="flex min-w-0 flex-col gap-2">
            <legend className="text-sm font-medium">{t('myProfile.availability')}</legend>
            <div className="relative overflow-x-auto">
              <table className="w-full min-w-[420px] border-collapse text-center text-sm">
                <thead>
                  <tr>
                    <th scope="col" className="w-20" />
                    {allWeekDays.map((day) => (
                      <th key={day} scope="col" className="pb-2 font-medium">
                        {t(`weekDays.${weekDayKey[day]}`)}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {allDayPeriods.map((period) => (
                    <tr key={period} className="border-t">
                      <th scope="row" className="py-2 text-left font-medium">
                        {t(`periods.${dayPeriodKey[period]}`)}
                      </th>
                      {allWeekDays.map((day) => {
                        const key = slotKey(day, period)
                        return (
                          <td key={day} className="py-2">
                            <Checkbox
                              aria-label={`${t(`weekDays.${weekDayKey[day]}`)} ${t(`periods.${dayPeriodKey[period]}`)}`}
                              checked={availability.includes(key)}
                              onCheckedChange={(checked) => toggleSlot(key, checked === true)}
                            />
                          </td>
                        )
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </fieldset>

          <fieldset className="flex min-w-0 flex-col gap-2">
            <legend className="text-sm font-medium">{t('myProfile.structure')}</legend>
            {(['hasGear', 'hasTransport', 'hasRehearsalSpace'] as const).map((name) => (
              <StructureCheck key={name} name={name} label={t(`profile.${name}`)} form={form} />
            ))}
          </fieldset>

          <AppChipsField name="genres" label={t('myProfile.genres')} max={maxGenres} options={allMusicGenres.map((genre) => ({ value: genre, label: genreLabel(genre) }))} />

          <fieldset className="flex min-w-0 flex-col gap-3">
            <legend className="text-sm font-medium">{t('myProfile.links')}</legend>
            <p className="app-meta">{t('myProfile.linksHint')}</p>
            {links.fields.map((field, index) => (
              <div key={field.id} className="flex items-end gap-2">
                <div className="flex-1">
                  <AppField name={`links.${index}.url`} label={`${t('myProfile.links')} ${index + 1}`} type="url" placeholder="https://" />
                </div>
                <AppIconButton label={t('myProfile.removeLink')} onClick={() => links.remove(index)}>
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
          </fieldset>

          <AppTextareaField name="influences" label={t('myProfile.influences')} placeholder={t('myProfile.influencesPlaceholder')} rows={3} maxLength={300} />

          <AppButton type="submit" loading={saving} className="self-start">
            {t('myProfile.save')}
          </AppButton>
        </AppForm>
      </AppCard>
    </div>
  )
}

type StructureName = 'hasGear' | 'hasTransport' | 'hasRehearsalSpace'

function StructureCheck({ name, label, form }: { name: StructureName; label: string; form: ReturnType<typeof useForm<FormValues>> }) {
  const checked = useWatch({ control: form.control, name })

  return (
    <label className="flex items-center gap-2 text-sm">
      <Checkbox checked={checked} onCheckedChange={(value) => form.setValue(name, value === true, { shouldDirty: true })} />
      {label}
    </label>
  )
}

const emptyProfile: MusicianProfileDto = {
  exists: false,
  isMine: true,
  person: { id: 0, name: '', nickname: '', avatarUrl: null },
  city: null,
  cityName: null,
  isLookingForBand: true,
  lookingSince: null,
  goal: MusicianGoal.either,
  commitment: MusicianCommitment.playLive,
  reach: Reach.abc,
  hasGear: false,
  hasTransport: false,
  hasRehearsalSpace: false,
  influences: null,
  instruments: [],
  genres: [],
  availability: [],
  sampleLinks: [],
}
