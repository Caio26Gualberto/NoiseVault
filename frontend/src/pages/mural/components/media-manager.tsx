import { ArrowDownIcon, ArrowUpIcon, CircleAlertIcon, ImagePlusIcon, LoaderCircleIcon, StarIcon, VideoIcon, XIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton, AppIconButton } from '@/components/app-button'
import { acceptedImageTypes, checkImageFile } from '@/lib/image-file'
import { cn } from '@/lib/utils'
import { archivePhotoMaxBytes, archiveVideoLimits, checkVideoFile } from '@/lib/video-file'
import { archiveVideoStartPath, deleteMedia, uploadVideo } from '@/pages/feed/services/media-service'

import { archiveMaxMedia } from '../managed-media'
import type { ManagedMedia } from '../managed-media'
import { uploadArchivePhoto } from '../services/mural-service'

const captionMaxLength = 500

type MediaManagerProps = {
  value: ManagedMedia[]
  onChange: (update: (previous: ManagedMedia[]) => ManagedMedia[]) => void
  coverKey: string | null
  onCoverChange: (key: string) => void
}

export function MediaManager({ value, onChange, coverKey, onCoverChange }: MediaManagerProps) {
  const { t } = useTranslation('mural')
  const photoInput = useRef<HTMLInputElement>(null)
  const videoInput = useRef<HTMLInputElement>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const valueRef = useRef(value)

  useEffect(() => {
    valueRef.current = value
  }, [value])

  useEffect(
    () => () => {
      for (const item of valueRef.current) {
        item.controller?.abort()
        if (item.localUrl && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
      }
    },
    [],
  )

  const patch = (key: string, changes: Partial<ManagedMedia>) =>
    onChange((previous) => previous.map((item) => (item.key === key ? { ...item, ...changes } : item)))

  const room = archiveMaxMedia - value.length

  const addPhotos = (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    setNotice(files.length > room ? t('form.media.limit', { max: archiveMaxMedia }) : null)

    for (const file of files.slice(0, Math.max(room, 0))) {
      const check = checkImageFile(file, archivePhotoMaxBytes)
      if (check !== 'ok') {
        setNotice(t(`form.media.photo.${check}`, { size: 40 }))
        continue
      }
      const key = crypto.randomUUID()
      onChange((previous) => [
        ...previous,
        { key, kind: 'photo', previewUrl: URL.createObjectURL(file), localUrl: true, mediaId: null, caption: '', state: 'uploading', progress: null, saved: false, controller: null },
      ])
      uploadArchivePhoto(file)
        .then((media) => patch(key, { state: 'ready', mediaId: media.id }))
        .catch(() => patch(key, { state: 'error' }))
    }
  }

  const addVideos = async (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    setNotice(files.length > room ? t('form.media.limit', { max: archiveMaxMedia }) : null)

    for (const file of files.slice(0, Math.max(room, 0))) {
      const check = await checkVideoFile(file, archiveVideoLimits)
      if (check !== 'ok') {
        setNotice(t(`form.media.video.${check}`, { size: 4, hours: 2 }))
        continue
      }
      const key = crypto.randomUUID()
      const controller = new AbortController()
      onChange((previous) => [
        ...previous,
        { key, kind: 'video', previewUrl: null, localUrl: false, mediaId: null, caption: '', state: 'uploading', progress: 0, saved: false, controller },
      ])
      uploadVideo(file, (loaded, total) => patch(key, { progress: Math.round((loaded / total) * 100) }), controller.signal, archiveVideoStartPath)
        .then((media) => patch(key, { state: 'ready', mediaId: media.id, progress: 100, controller: null }))
        .catch(() => {
          if (!controller.signal.aborted) patch(key, { state: 'error', controller: null })
        })
    }
  }

  const remove = (item: ManagedMedia) => {
    item.controller?.abort()
    if (!item.saved && item.mediaId !== null) void deleteMedia(item.mediaId).catch(() => undefined)
    if (item.localUrl && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
    onChange((previous) => previous.filter((other) => other.key !== item.key))
  }

  const move = (index: number, step: number) =>
    onChange((previous) => {
      const next = [...previous]
      const [item] = next.splice(index, 1)
      next.splice(index + step, 0, item)
      return next
    })

  const effectiveCover = coverKey ?? value.find((item) => item.state === 'ready')?.key ?? null

  return (
    <fieldset className="flex flex-col gap-3">
      <legend className="text-sm font-medium">{t('form.media.title')}</legend>
      <p className="app-meta">{t('form.media.hint')}</p>

      {value.length > 0 ? (
        <ol className="flex flex-col gap-2">
          {value.map((item, index) => (
            <li key={item.key} className={cn('flex gap-3 rounded-lg border bg-background p-2', effectiveCover === item.key && 'border-primary/60')}>
              <div className="relative size-20 shrink-0 overflow-hidden rounded-md bg-muted sm:size-24">
                {item.previewUrl ? <img src={item.previewUrl} alt="" className="size-full object-cover" /> : null}
                {item.kind === 'video' && item.state === 'ready' ? (
                  <span className="absolute bottom-1 left-1 rounded bg-scrim/70 p-1 text-on-scrim">
                    <VideoIcon className="size-3.5" />
                  </span>
                ) : null}
                {item.state !== 'ready' ? (
                  <span className={cn('absolute inset-0 flex flex-col items-center justify-center gap-1 text-xs font-medium text-on-scrim tabular-nums', item.state === 'error' ? 'bg-scrim/75' : 'bg-scrim/55')}>
                    {item.state === 'error' ? <CircleAlertIcon className="size-5" /> : <LoaderCircleIcon className="size-5 animate-spin" />}
                    {item.state === 'error' ? t('form.media.failed') : item.progress !== null ? `${item.progress}%` : null}
                  </span>
                ) : null}
              </div>
              <div className="flex min-w-0 flex-1 flex-col gap-2">
                <input
                  type="text"
                  value={item.caption}
                  maxLength={captionMaxLength}
                  onChange={(event) => patch(item.key, { caption: event.target.value })}
                  placeholder={t('form.media.captionPlaceholder')}
                  aria-label={t('form.media.caption')}
                  className="h-9 w-full rounded-md border bg-transparent px-2.5 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                />
                <div className="flex flex-wrap items-center gap-1">
                  <AppButton
                    type="button"
                    intent={effectiveCover === item.key ? 'primary' : 'ghost'}
                    size="sm"
                    disabled={item.state !== 'ready'}
                    onClick={() => onCoverChange(item.key)}
                    aria-pressed={effectiveCover === item.key}
                  >
                    <StarIcon className={cn(effectiveCover === item.key && 'fill-current')} />
                    {effectiveCover === item.key ? t('form.media.isCover') : t('form.media.makeCover')}
                  </AppButton>
                  <span className="ml-auto flex items-center">
                    <AppIconButton type="button" label={t('form.media.up')} size="sm" disabled={index === 0} onClick={() => move(index, -1)}>
                      <ArrowUpIcon />
                    </AppIconButton>
                    <AppIconButton type="button" label={t('form.media.down')} size="sm" disabled={index === value.length - 1} onClick={() => move(index, 1)}>
                      <ArrowDownIcon />
                    </AppIconButton>
                    <AppIconButton type="button" label={t('form.media.remove')} size="sm" onClick={() => remove(item)}>
                      <XIcon />
                    </AppIconButton>
                  </span>
                </div>
              </div>
            </li>
          ))}
        </ol>
      ) : null}

      {notice ? <p className="text-sm text-destructive">{notice}</p> : null}

      <div className="flex flex-wrap gap-2">
        <input ref={photoInput} type="file" accept={acceptedImageTypes.join(',')} multiple className="hidden" onChange={addPhotos} />
        <input ref={videoInput} type="file" accept="video/*" multiple className="hidden" onChange={(event) => void addVideos(event)} />
        <AppButton type="button" intent="neutral" onClick={() => photoInput.current?.click()} disabled={room <= 0}>
          <ImagePlusIcon />
          {t('form.media.addPhotos')}
        </AppButton>
        <AppButton type="button" intent="neutral" onClick={() => videoInput.current?.click()} disabled={room <= 0}>
          <VideoIcon />
          {t('form.media.addVideos')}
        </AppButton>
      </div>
    </fieldset>
  )
}
