import { zodResolver } from '@hookform/resolvers/zod'
import { MegaphoneIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppEmptyState } from '@/components/app-empty-state'
import { AppField, AppForm, AppSelectField, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { ensaioPath } from '@/ensaio/frame'

import { Instrument, MusicianGoal, OpeningStatus, allInstruments, goalKey, useInstrumentLabel } from './ensaio-status'
import type { MusicianGoal as Goal } from './ensaio-status'
import { useOwnedBands } from './hooks/use-owned-bands'
import { createOpening, getOpening, updateOpening } from './services/ensaio-service'
import type { BandOpeningInputDto } from './services/ensaio-service'

const otherValue = String(Instrument.other)

const schema = z
  .object({
    band: z.string(),
    instrument: z.string().min(1, 'validation.required'),
    otherName: z.string().trim().max(60, 'validation.instrumentLength'),
    goal: z.string(),
    description: z.string().trim().min(1, 'ensaio:validation.openingText').max(500, 'ensaio:validation.openingText'),
  })
  .refine((values) => values.instrument !== otherValue || values.otherName.length > 0, { path: ['otherName'], message: 'ensaio:validation.otherName' })

type FormValues = z.infer<typeof schema>

function toInput(values: FormValues): BandOpeningInputDto {
  const instrument = Number(values.instrument) as Instrument
  return {
    instrument,
    otherName: instrument === Instrument.other ? values.otherName : null,
    description: values.description,
    goal: values.goal === '' ? null : (Number(values.goal) as Goal),
  }
}

export function EnsaioVagaFormPage() {
  const { t } = useTranslation('ensaio')
  const { id } = useParams()
  const openingId = id ? Number(id) : null
  const navigate = useNavigate()
  const instrumentLabel = useInstrumentLabel()
  const { bands, loaded } = useOwnedBands()
  const [loading, setLoading] = useState(openingId !== null)
  const [saving, setSaving] = useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { band: '', instrument: '', otherName: '', goal: '', description: '' },
  })

  const instrument = useWatch({ control: form.control, name: 'instrument' })

  useEffect(() => {
    if (openingId === null && bands.length === 1) form.setValue('band', bands[0].slug)
  }, [openingId, bands, form])

  useEffect(() => {
    if (openingId === null) return
    let cancelled = false
    getOpening(openingId)
      .then((opening) => {
        if (cancelled) return
        if (!opening.canManage || opening.status !== OpeningStatus.open) {
          navigate(ensaioPath(`/vagas/${opening.id}`), { replace: true })
          return
        }
        form.reset({
          band: opening.band.slug,
          instrument: String(opening.instrument),
          otherName: opening.otherName ?? '',
          goal: opening.goal === null ? '' : String(opening.goal),
          description: opening.description,
        })
      })
      .catch(() => navigate(ensaioPath('/vagas'), { replace: true }))
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [openingId, form, navigate])

  const submit = async (values: FormValues) => {
    if (openingId === null && !values.band) {
      form.setError('band', { message: 'ensaio:validation.band' })
      return
    }
    setSaving(true)
    try {
      const saved = openingId === null ? await createOpening(values.band, toInput(values)) : await updateOpening(openingId, toInput(values))
      navigate(ensaioPath(`/vagas/${saved.id}`), { replace: true })
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  if (loading || (openingId === null && !loaded)) return <AppSkeleton preset="lines" count={6} className="mx-auto max-w-2xl" />

  if (openingId === null && bands.length === 0) {
    return <AppEmptyState icon={MegaphoneIcon} title={t('openingForm.noBands')} />
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col gap-6">
      <AppPageHeader title={t(openingId === null ? 'openingForm.createTitle' : 'openingForm.editTitle')} description={t('openingForm.description')} />
      <AppCard>
        <AppForm form={form} onSubmit={submit}>
          {openingId === null ? (
            <AppSelectField name="band" label={t('openingForm.band')} options={bands.map((band) => ({ value: band.slug, label: band.name }))} />
          ) : null}
          <div className="grid gap-4 md:grid-cols-2">
            <AppSelectField
              name="instrument"
              label={t('openingForm.instrument')}
              options={allInstruments.map((value) => ({ value: String(value), label: instrumentLabel(value) }))}
            />
            {instrument === otherValue ? <AppField name="otherName" label={t('openingForm.otherName')} /> : null}
          </div>
          <AppSelectField
            name="goal"
            label={t('openingForm.goal')}
            emptyLabel={t('openingForm.noGoal')}
            options={Object.values(MusicianGoal).map((value) => ({ value: String(value), label: t(`goals.${goalKey[value]}`) }))}
          />
          <AppTextareaField name="description" label={t('openingForm.text')} placeholder={t('openingForm.textPlaceholder')} rows={5} maxLength={500} />
          <div className="flex gap-2">
            <AppButton type="submit" loading={saving}>
              {t(openingId === null ? 'openingForm.submitCreate' : 'openingForm.submitEdit')}
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
