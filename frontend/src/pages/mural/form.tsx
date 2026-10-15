import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { z } from 'zod'

import { Permissions } from '@/auth/permissions'
import { useCan } from '@/auth/use-can'
import { AppButton } from '@/components/app-button'
import { AppCityField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppFormLayout, AppFormPanel } from '@/components/app-form-layout'
import { AppPageHeader } from '@/components/app-page-header'
import { AppSkeleton } from '@/components/app-skeleton'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { LineupEditor } from '@/pages/eventos/components/lineup-editor'
import { ArchiveEmbed } from '@/pages/feed/components/archive-embed'
import { MediaKind } from '@/pages/feed/media-kind'
import { MediaStatus } from '@/pages/feed/media-status'
import type { LineupItem } from '@/pages/eventos/components/lineup-editor'
import { cityValueSchema, emptyCityValue, toCityInput, toCityValue } from '@/pages/perfil/city'
import { featuredCities, searchCities } from '@/pages/perfil/services/cidades-service'

import { ArchiveEntryStatus, mySubmissionsPath } from './archive-status'
import { MediaManager } from './components/media-manager'
import { fromSaved } from './managed-media'
import type { ManagedMedia } from './managed-media'
import { createArchiveEntry, getArchiveEntry, submitArchiveEntry, updateArchiveEntry, updateArchiveSubmission } from './services/mural-service'
import type { ArchiveEntryInput, ArchiveSummaryDto } from './services/mural-service'

const none = 'none'
const minYear = 1950
const currentYear = new Date().getFullYear()
const monthFormat = new Intl.DateTimeFormat('pt-BR', { month: 'long', timeZone: 'UTC' })
const months = Array.from({ length: 12 }, (_, index) => ({ value: index + 1, label: monthFormat.format(new Date(Date.UTC(2000, index, 1))) }))

const schema = z
  .object({
    title: z.string().trim().min(3, 'validation.required').max(150),
    description: z.string().trim().max(10000),
    year: z.string().trim().regex(/^\d{4}$/, 'validation.archiveYear'),
    month: z.string(),
    day: z.string().trim().refine((value) => !value || /^\d{1,2}$/.test(value), 'validation.number'),
    venue: z.string().trim().max(120),
    city: cityValueSchema,
  })
  .refine((values) => Number(values.year) >= minYear && Number(values.year) <= currentYear, { path: ['year'], message: 'validation.archiveYear' })
  .refine((values) => !values.day || values.month !== none, { path: ['day'], message: 'validation.archiveDayNeedsMonth' })
  .refine(
    (values) => !values.day || values.month === none || Number(values.day) <= new Date(Date.UTC(Number(values.year), Number(values.month), 0)).getUTCDate(),
    { path: ['day'], message: 'validation.archiveDay' },
  )

type FormValues = z.infer<typeof schema>

export function MuralFormPage({ mode = 'curation' }: { mode?: 'curation' | 'submission' }) {
  const { t } = useTranslation('mural')
  const can = useCan()
  const { id } = useParams()
  const navigate = useNavigate()
  const entryId = id ? Number(id) : null
  const [loading, setLoading] = useState(entryId !== null)
  const [saving, setSaving] = useState(false)
  const [bands, setBands] = useState<LineupItem[]>([])
  const [media, setMedia] = useState<ManagedMedia[]>([])
  const [coverKey, setCoverKey] = useState<string | null>(null)
  const [mediaError, setMediaError] = useState<string | null>(null)
  const [editAsAuthor, setEditAsAuthor] = useState(false)
  const submitting = mode === 'submission'
  const uploadTarget = can(Permissions.archiveCreate) ? 'curation' : 'submission'

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', description: '', year: '', month: none, day: '', venue: '', city: emptyCityValue },
  })

  useEffect(() => {
    if (entryId === null) return
    let cancelled = false
    getArchiveEntry(entryId)
      .then((entry) => {
        if (cancelled) return
        if (!entry.canEdit) {
          navigate(`/mural/${entry.id}`, { replace: true })
          return
        }
        form.reset({
          title: entry.title,
          description: entry.description ?? '',
          year: String(entry.year),
          month: entry.month ? String(entry.month) : none,
          day: entry.day ? String(entry.day) : '',
          venue: entry.venue ?? '',
          city: toCityValue(entry),
        })
        setBands(entry.bands.map((band) => ({ key: crypto.randomUUID(), band: band.band, name: band.name })))
        const saved = entry.media.map((item) => fromSaved(item.media, item.caption))
        setMedia(saved)
        setCoverKey(entry.cover ? `saved-${entry.cover.id}` : null)
        setEditAsAuthor(entry.isMine && entry.status === ArchiveEntryStatus.pending && !can(Permissions.archiveEdit))
      })
      .catch(() => navigate('/mural', { replace: true }))
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [entryId, form, navigate, can])

  const month = useWatch({ control: form.control, name: 'month' })
  const [previewTitle, previewYear, previewMonth, previewDay] = useWatch({ control: form.control, name: ['title', 'year', 'month', 'day'] })
  const uploading = media.some((item) => item.state === 'uploading')

  const submit = async (values: FormValues) => {
    const ready = media.filter((item) => item.state === 'ready' && item.mediaId !== null)
    if (ready.length === 0) {
      setMediaError(t('form.media.required'))
      return
    }
    setMediaError(null)
    const cover = ready.find((item) => item.key === coverKey) ?? ready[0]
    const input: ArchiveEntryInput = {
      title: values.title,
      description: values.description || null,
      year: Number(values.year),
      month: values.month === none ? null : Number(values.month),
      day: values.day ? Number(values.day) : null,
      venue: values.venue || null,
      ...toCityInput(values.city),
      bands: bands.map((item) => (item.band ? { bandId: item.band.id, name: null } : { bandId: null, name: item.name })),
      media: ready.map((item) => ({ mediaId: item.mediaId!, caption: item.caption.trim() || null })),
      coverMediaId: cover.mediaId,
    }
    setSaving(true)
    try {
      const saved =
        entryId !== null
          ? await (editAsAuthor ? updateArchiveSubmission : updateArchiveEntry)(entryId, input)
          : await (submitting ? submitArchiveEntry : createArchiveEntry)(input)
      setMedia((previous) => previous.map((item) => ({ ...item, saved: true })))
      navigate(entryId === null && submitting ? mySubmissionsPath : `/mural/${saved.id}`, { replace: true })
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <AppSkeleton preset="lines" count={8} className="app-page-enter mx-auto max-w-3xl" />
  }

  const readyMedia = media.filter((item) => item.state === 'ready')
  const coverItem = media.find((item) => item.key === coverKey) ?? media[0] ?? null
  const preview: ArchiveSummaryDto = {
    id: entryId ?? 0,
    title: previewTitle?.trim() || t('form.previewTitle'),
    year: /^\d{4}$/.test(previewYear ?? '') ? Number(previewYear) : currentYear,
    month: previewMonth && previewMonth !== none ? Number(previewMonth) : null,
    day: previewMonth && previewMonth !== none && /^\d{1,2}$/.test(previewDay ?? '') ? Number(previewDay) : null,
    cover: coverItem?.previewUrl
      ? {
          id: 0,
          kind: MediaKind.photo,
          status: MediaStatus.ready,
          url: coverItem.previewUrl,
          posterUrl: coverItem.previewUrl,
          width: null,
          height: null,
          durationSeconds: null,
          tags: [],
        }
      : null,
    photoCount: readyMedia.filter((item) => item.kind === 'photo').length,
    videoCount: readyMedia.filter((item) => item.kind === 'video').length,
  }

  return (
    <div className="app-page-enter flex flex-col gap-6">
      <AppPageHeader
        title={t(entryId !== null ? 'form.editTitle' : submitting ? 'form.submitTitle' : 'form.createTitle')}
        description={t(submitting ? 'form.submitDescription' : 'form.description')}
      />

      <AppForm form={form} onSubmit={submit}>
        <AppFormLayout
          aside={
            <>
              <div className="flex flex-col gap-2">
                <span className="app-meta">{t('form.preview')}</span>
                <ArchiveEmbed entry={preview} linked={false} />
              </div>

              {mediaError ? (
                <p role="alert" className="text-sm text-destructive">
                  {mediaError}
                </p>
              ) : null}

              <div className="flex flex-col gap-2">
                <AppButton type="submit" loading={saving} disabled={uploading} size="lg" className="w-full font-heading tracking-wider uppercase">
                  {uploading ? t('form.waitUpload') : t(entryId !== null ? 'form.submitEdit' : submitting ? 'form.submitSend' : 'form.submitCreate')}
                </AppButton>
                <AppButton intent="ghost" type="button" onClick={() => navigate(-1)}>
                  {t('common:actions.cancel')}
                </AppButton>
              </div>
            </>
          }
        >
          <AppFormPanel title={t('form.sections.what')} wide>
            <AppField name="title" label={t('form.fields.title')} placeholder={t('form.titlePlaceholder')} />
          </AppFormPanel>

          <AppFormPanel title={t('form.fields.date')} description={t('form.dateHint')}>
            <div className="grid grid-cols-[1fr_1.4fr_1fr] gap-3">
              <AppField name="year" label={t('form.fields.year')} inputMode="numeric" placeholder="1987" />
              <div className="flex flex-col gap-1.5">
                <Label>{t('form.fields.month')}</Label>
                <Select value={month} onValueChange={(value) => form.setValue('month', value, { shouldValidate: true })}>
                  <SelectTrigger className="h-11 md:h-10" aria-label={t('form.fields.month')}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={none}>{t('form.unknown')}</SelectItem>
                    {months.map((item) => (
                      <SelectItem key={item.value} value={String(item.value)}>
                        {item.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <AppField name="day" label={t('form.fields.day')} inputMode="numeric" placeholder={t('form.unknownShort')} />
            </div>
          </AppFormPanel>

          <AppFormPanel title={t('form.sections.where')}>
            <AppField name="venue" label={t('form.fields.venue')} placeholder={t('form.venuePlaceholder')} />
            <AppCityField
              name="city"
              label={t('form.fields.city')}
              placeholder={t('form.cityPlaceholder')}
              clearLabel={t('form.unknown')}
              search={searchCities} suggest={featuredCities}
              allowAbroad
            />
          </AppFormPanel>

          <AppFormPanel title={t('form.sections.story')} wide>
            <AppTextareaField name="description" label={t('form.fields.description')} rows={8} placeholder={t('form.descriptionPlaceholder')} />
          </AppFormPanel>

          <AppFormPanel title={t('form.sections.bands')} wide>
            <LineupEditor value={bands} onChange={setBands} legend={t('form.fields.bands')} hint={t('form.bandsHint')} />
          </AppFormPanel>

          <AppFormPanel title={t('form.sections.media')} wide>
            <MediaManager value={media} onChange={setMedia} coverKey={coverKey} onCoverChange={setCoverKey} target={uploadTarget} />
          </AppFormPanel>
        </AppFormLayout>
      </AppForm>
    </div>
  )
}
