import { ArrowDownIcon, ArrowUpIcon, DiscIcon, EyeIcon, EyeOffIcon, ImageIcon, PencilIcon, PlusIcon, RefreshCwIcon, StarIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { AppBadge } from '@/components/app-badge'
import { AppButton, AppIconButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { useAppConfirm } from '@/components/app-confirm'
import { AppSkeleton } from '@/components/app-skeleton'
import { avatarMaxBytes, checkImageFile } from '@/lib/image-file'
import { formatRelative } from '@/lib/format'

import { ReleaseSource, releaseKindKey } from '../release-kind'
import { deleteRelease, getBandDiscography, importDiscography, reorderDiscography, setFeaturedTrack, setReleaseCover, setReleaseHidden } from '../services/bandas-service'
import type { BandDiscographyDto, BandProfileDto, BandReleaseDto } from '../services/bandas-service'
import { ReleaseFormDialog } from './release-form-dialog'
import { SpotifyLogo } from './spotify-embed'

type DiscographyManagerProps = {
  band: BandProfileDto
}

export function DiscographyManager({ band }: DiscographyManagerProps) {
  const { t } = useTranslation('bandas')
  const confirm = useAppConfirm()
  const [discography, setDiscography] = useState<BandDiscographyDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [order, setOrder] = useState<number[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [importing, setImporting] = useState(false)
  const [expanded, setExpanded] = useState<number | null>(null)
  const [coverError, setCoverError] = useState<string | null>(null)
  const [form, setForm] = useState<{ open: boolean; release: BandReleaseDto | null }>({ open: false, release: null })
  const coverInput = useRef<HTMLInputElement>(null)
  const coverTarget = useRef<number | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    getBandDiscography(band.slug)
      .then((data) => {
        if (!cancelled) setDiscography(data)
      })
      .catch(() => {
        if (!cancelled) setDiscography(null)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [band.slug])

  const apply = (data: BandDiscographyDto) => {
    setDiscography(data)
    setOrder(null)
  }

  const run = async (action: () => Promise<BandDiscographyDto>) => {
    setBusy(true)
    try {
      apply(await action())
    } catch {
      return
    } finally {
      setBusy(false)
    }
  }

  const doImport = async () => {
    setImporting(true)
    try {
      apply(await importDiscography(band.slug))
    } catch {
      return
    } finally {
      setImporting(false)
    }
  }

  const releases = discography?.releases ?? []
  const ids = order ?? releases.map((release) => release.id)
  const ordered = ids.map((id) => releases.find((release) => release.id === id)).filter((release): release is BandReleaseDto => release !== undefined)
  const dirty = order !== null && order.some((id, index) => releases[index]?.id !== id)

  const move = (index: number, step: number) => {
    const next = [...ids]
    const [item] = next.splice(index, 1)
    next.splice(index + step, 0, item)
    setOrder(next)
  }

  const remove = (release: BandReleaseDto) =>
    confirm({
      title: t('discography.manage.deleteTitle', { title: release.title }),
      message: t('discography.manage.deleteMessage'),
      confirmLabel: t('discography.manage.deleteConfirm'),
      danger: true,
      onConfirm: () => run(() => deleteRelease(band.slug, release.id)),
    })

  const pickCover = (releaseId: number) => {
    coverTarget.current = releaseId
    coverInput.current?.click()
  }

  const uploadCover = async (file: File | undefined) => {
    const releaseId = coverTarget.current
    if (!file || releaseId === null) return
    const check = checkImageFile(file, avatarMaxBytes)
    if (check !== 'ok') {
      setCoverError(t(`common:imageUpload.image.${check}`, { size: 5 }))
      return
    }
    setCoverError(null)
    await run(() => setReleaseCover(band.slug, releaseId, file))
  }

  const hasArtistLink = /open\.spotify\.com\/(?:intl-[a-z-]+\/)?artist\//i.test(band.socialLinks.spotify ?? '')

  return (
    <AppCard title={t('discography.manage.title')} description={t('discography.manage.description')}>
      {loading || !discography ? (
        <AppSkeleton preset="list" count={2} />
      ) : (
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2 rounded-xl border border-dashed p-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-3 text-sm">
              <SpotifyLogo className="mt-0.5 size-5 shrink-0 text-success" />
              <div className="flex flex-col gap-0.5">
                <span className="font-medium">{t('discography.manage.spotifyTitle')}</span>
                <span className="text-muted-foreground">
                  {!hasArtistLink
                    ? t('discography.manage.spotifyNoLink')
                    : discography.syncedAt
                      ? t('discography.manage.spotifySynced', { when: formatRelative(discography.syncedAt) })
                      : t('discography.manage.spotifyNeverSynced')}
                </span>
              </div>
            </div>
            <AppButton type="button" intent="secondary" loading={importing} disabled={!discography.canImport || busy} onClick={() => void doImport()} className="shrink-0">
              <RefreshCwIcon />
              {t(discography.syncedAt ? 'discography.manage.refresh' : 'discography.manage.import')}
            </AppButton>
          </div>

          {ordered.length === 0 ? <p className="app-meta">{t('discography.manage.empty')}</p> : null}
          {coverError ? <p className="text-sm text-destructive">{coverError}</p> : null}

          <ol className="flex flex-col gap-2">
            {ordered.map((release, index) => {
              const manual = release.source === ReleaseSource.manual
              const open = expanded === release.id
              return (
                <li key={release.id} className={`rounded-xl border ${release.isHidden ? 'opacity-60' : ''}`}>
                  <div className="flex items-center gap-3 p-2">
                    <span className="size-12 shrink-0 overflow-hidden rounded-md bg-muted">
                      {release.coverUrl ? (
                        <img src={release.coverUrl} alt="" className="size-full object-cover" />
                      ) : (
                        <span className="flex size-full items-center justify-center text-muted-foreground">
                          <DiscIcon className="size-5" />
                        </span>
                      )}
                    </span>
                    <button type="button" onClick={() => setExpanded(open ? null : release.id)} className="flex min-w-0 flex-1 flex-col text-left focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none">
                      <span className="truncate text-sm font-medium">{release.title}</span>
                      <span className="app-meta flex flex-wrap items-center gap-1.5">
                        {t(`discography.kinds.${releaseKindKey[release.kind]}`)} · {release.year} · {t('discography.trackCount', { count: release.tracks.length })}
                        <AppBadge tone={manual ? 'neutral' : 'success'}>{t(manual ? 'discography.manage.manual' : 'discography.manage.fromSpotify')}</AppBadge>
                        {release.isHidden ? <AppBadge tone="warning">{t('discography.manage.hidden')}</AppBadge> : null}
                      </span>
                    </button>
                    <span className="flex shrink-0 items-center gap-0.5">
                      <AppIconButton type="button" label={t('discography.form.up')} size="sm" disabled={index === 0 || busy} onClick={() => move(index, -1)}>
                        <ArrowUpIcon />
                      </AppIconButton>
                      <AppIconButton type="button" label={t('discography.form.down')} size="sm" disabled={index === ordered.length - 1 || busy} onClick={() => move(index, 1)}>
                        <ArrowDownIcon />
                      </AppIconButton>
                      <AppIconButton
                        type="button"
                        label={t(release.isHidden ? 'discography.manage.show' : 'discography.manage.hide')}
                        size="sm"
                        disabled={busy}
                        onClick={() => void run(() => setReleaseHidden(band.slug, release.id, !release.isHidden))}
                      >
                        {release.isHidden ? <EyeOffIcon /> : <EyeIcon />}
                      </AppIconButton>
                      {manual ? (
                        <>
                          <AppIconButton type="button" label={t('discography.manage.cover')} size="sm" disabled={busy} onClick={() => pickCover(release.id)}>
                            <ImageIcon />
                          </AppIconButton>
                          <AppIconButton type="button" label={t('discography.manage.edit')} size="sm" disabled={busy} onClick={() => setForm({ open: true, release })}>
                            <PencilIcon />
                          </AppIconButton>
                          <AppIconButton type="button" label={t('discography.manage.delete')} size="sm" disabled={busy} onClick={() => remove(release)}>
                            <Trash2Icon />
                          </AppIconButton>
                        </>
                      ) : null}
                    </span>
                  </div>
                  {open ? (
                    <ol className="flex flex-col divide-y border-t">
                      {release.tracks.map((track) => (
                        <li key={track.id} className="flex items-center gap-3 px-3 py-1.5 text-sm">
                          <span className="w-6 shrink-0 text-right text-muted-foreground tabular-nums">{track.number}</span>
                          <span className="min-w-0 flex-1 truncate">{track.title}</span>
                          <AppIconButton
                            type="button"
                            label={t(track.isFeatured ? 'discography.manage.unfeature' : 'discography.manage.feature')}
                            size="sm"
                            disabled={busy}
                            onClick={() => void run(() => setFeaturedTrack(band.slug, track.isFeatured ? null : track.id))}
                          >
                            <StarIcon className={track.isFeatured ? 'fill-highlight text-highlight' : ''} />
                          </AppIconButton>
                        </li>
                      ))}
                      {release.tracks.length === 0 ? <li className="app-meta px-3 py-2">{t('discography.manage.noTracks')}</li> : null}
                    </ol>
                  ) : null}
                </li>
              )
            })}
          </ol>

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
            <AppButton type="button" intent="neutral" onClick={() => setForm({ open: true, release: null })} disabled={busy}>
              <PlusIcon />
              {t('discography.manage.add')}
            </AppButton>
            {dirty ? (
              <div className="flex gap-2">
                <AppButton type="button" intent="ghost" onClick={() => setOrder(null)} disabled={busy}>
                  {t('discography.manage.discardOrder')}
                </AppButton>
                <AppButton type="button" loading={busy} onClick={() => void run(() => reorderDiscography(band.slug, ids))}>
                  {t('discography.manage.saveOrder')}
                </AppButton>
              </div>
            ) : null}
          </div>

          <input
            ref={coverInput}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={(event) => {
              void uploadCover(event.target.files?.[0])
              event.target.value = ''
            }}
          />

          {form.open ? (
            <ReleaseFormDialog
              key={form.release?.id ?? 'new'}
              slug={band.slug}
              release={form.release}
              open={form.open}
              onOpenChange={(open) => setForm((current) => ({ ...current, open }))}
              onSaved={apply}
            />
          ) : null}
        </div>
      )}
    </AppCard>
  )
}
