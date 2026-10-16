import { ArrowDownIcon, ArrowUpIcon, PlusIcon, XIcon } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton, AppIconButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'

import { ReleaseKind, allReleaseKinds, releaseKindKey } from '../release-kind'
import { createRelease, maxTracksPerRelease, releaseMinYear, releaseTitleMaxLength, updateRelease } from '../services/bandas-service'
import type { BandDiscographyDto, BandReleaseDto, BandReleaseInput } from '../services/bandas-service'

type TrackDraft = { key: string; title: string; externalUrl: string }

type Draft = { title: string; kind: ReleaseKind; year: string; externalUrl: string; tracks: TrackDraft[] }

const currentYear = new Date().getFullYear()

type ReleaseFormDialogProps = {
  slug: string
  release: BandReleaseDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved: (discography: BandDiscographyDto) => void
}

const inputClass = 'h-11 w-full rounded-lg border bg-background px-3 text-[15px] outline-none focus-visible:ring-2 focus-visible:ring-ring/50 md:h-10'

function draftFrom(release: BandReleaseDto | null): Draft {
  return {
    title: release?.title ?? '',
    kind: release?.kind ?? ReleaseKind.album,
    year: release ? String(release.year) : '',
    externalUrl: release?.externalUrl ?? '',
    tracks: (release?.tracks ?? []).map((track) => ({ key: String(crypto.randomUUID()), title: track.title, externalUrl: track.externalUrl ?? '' })),
  }
}

export function ReleaseFormDialog({ slug, release, open, onOpenChange, onSaved }: ReleaseFormDialogProps) {
  const { t } = useTranslation('bandas')
  const [draft, setDraft] = useState(() => draftFrom(release))
  const [saving, setSaving] = useState(false)

  const year = Number(draft.year)
  const title = draft.title.trim()
  const valid =
    title.length > 0 &&
    title.length <= releaseTitleMaxLength &&
    Number.isInteger(year) &&
    year >= releaseMinYear &&
    year <= currentYear &&
    draft.tracks.every((track) => track.title.trim().length > 0)

  const change = (next: boolean) => {
    if (next) setDraft(draftFrom(release))
    onOpenChange(next)
  }

  const setTrack = (key: string, patch: Partial<TrackDraft>) =>
    setDraft((current) => ({ ...current, tracks: current.tracks.map((track) => (track.key === key ? { ...track, ...patch } : track)) }))

  const moveTrack = (index: number, step: number) =>
    setDraft((current) => {
      const next = [...current.tracks]
      const [item] = next.splice(index, 1)
      next.splice(index + step, 0, item)
      return { ...current, tracks: next }
    })

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!valid) return
    const input: BandReleaseInput = {
      title,
      kind: draft.kind,
      year,
      externalUrl: draft.externalUrl.trim() || null,
      tracks: draft.tracks.map((track) => ({ title: track.title.trim(), externalUrl: track.externalUrl.trim() || null, durationSeconds: null })),
    }
    setSaving(true)
    try {
      onSaved(release ? await updateRelease(slug, release.id, input) : await createRelease(slug, input))
      onOpenChange(false)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppDialog open={open} onOpenChange={change} title={t(release ? 'discography.form.editTitle' : 'discography.form.createTitle')} description={t('discography.form.description')} size="lg">
      <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">{t('discography.form.title')}</span>
          <input value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} maxLength={releaseTitleMaxLength} className={inputClass} />
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium">{t('discography.form.kind')}</span>
            <div className="flex flex-wrap gap-2">
              {allReleaseKinds.map((kind) => (
                <AppButton key={kind} type="button" size="sm" intent={draft.kind === kind ? 'primary' : 'neutral'} aria-pressed={draft.kind === kind} onClick={() => setDraft({ ...draft, kind })}>
                  {t(`discography.kinds.${releaseKindKey[kind]}`)}
                </AppButton>
              ))}
            </div>
          </div>
          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium">{t('discography.form.year')}</span>
            <input
              inputMode="numeric"
              value={draft.year}
              onChange={(event) => setDraft({ ...draft, year: event.target.value.replace(/\D/g, '').slice(0, 4) })}
              placeholder={String(currentYear)}
              className={inputClass}
            />
          </label>
        </div>

        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">{t('discography.form.link')}</span>
          <input type="url" value={draft.externalUrl} onChange={(event) => setDraft({ ...draft, externalUrl: event.target.value })} placeholder={t('discography.form.linkPlaceholder')} className={inputClass} />
          <span className="app-meta">{t('discography.form.linkHint')}</span>
        </label>

        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">{t('discography.form.tracks')}</span>
            <AppButton
              type="button"
              size="sm"
              intent="neutral"
              disabled={draft.tracks.length >= maxTracksPerRelease}
              onClick={() => setDraft({ ...draft, tracks: [...draft.tracks, { key: String(crypto.randomUUID()), title: '', externalUrl: '' }] })}
            >
              <PlusIcon />
              {t('discography.form.addTrack')}
            </AppButton>
          </div>
          {draft.tracks.length === 0 ? <p className="app-meta">{t('discography.form.noTracks')}</p> : null}
          <ol className="flex flex-col gap-2">
            {draft.tracks.map((track, index) => (
              <li key={track.key} className="flex flex-col gap-2 rounded-lg border p-2 sm:flex-row sm:items-center">
                <span className="w-6 shrink-0 text-center text-sm text-muted-foreground tabular-nums">{index + 1}</span>
                <input
                  value={track.title}
                  onChange={(event) => setTrack(track.key, { title: event.target.value })}
                  placeholder={t('discography.form.trackTitle')}
                  maxLength={releaseTitleMaxLength}
                  aria-label={t('discography.form.trackTitle')}
                  className={`${inputClass} sm:flex-1`}
                />
                <input
                  type="url"
                  value={track.externalUrl}
                  onChange={(event) => setTrack(track.key, { externalUrl: event.target.value })}
                  placeholder={t('discography.form.trackLink')}
                  aria-label={t('discography.form.trackLink')}
                  className={`${inputClass} sm:flex-1`}
                />
                <span className="flex shrink-0 items-center gap-1">
                  <AppIconButton type="button" label={t('discography.form.up')} size="sm" disabled={index === 0} onClick={() => moveTrack(index, -1)}>
                    <ArrowUpIcon />
                  </AppIconButton>
                  <AppIconButton type="button" label={t('discography.form.down')} size="sm" disabled={index === draft.tracks.length - 1} onClick={() => moveTrack(index, 1)}>
                    <ArrowDownIcon />
                  </AppIconButton>
                  <AppIconButton type="button" label={t('discography.form.removeTrack')} size="sm" onClick={() => setDraft({ ...draft, tracks: draft.tracks.filter((item) => item.key !== track.key) })}>
                    <XIcon />
                  </AppIconButton>
                </span>
              </li>
            ))}
          </ol>
        </div>

        <div className="flex justify-end gap-2">
          <AppButton type="button" intent="ghost" onClick={() => change(false)}>
            {t('discography.form.cancel')}
          </AppButton>
          <AppButton type="submit" loading={saving} disabled={!valid}>
            {t(release ? 'discography.form.save' : 'discography.form.create')}
          </AppButton>
        </div>
      </form>
    </AppDialog>
  )
}
