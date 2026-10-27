import { CircleAlertIcon, ImagePlusIcon, LoaderCircleIcon, VideoIcon, XIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { acceptedImageTypes, checkImageFile } from '@/lib/image-file'
import { cn } from '@/lib/utils'
import { checkVideoFile, postPhotoMaxBytes, supportVideoLimits } from '@/lib/video-file'
import { deleteMedia } from '@/pages/feed/services/media-service'

import { uploadSupportPhoto, uploadSupportVideo } from '../services/suporte-service'

export const supportMaxMedia = 6
export const supportMaxVideos = 1

export type ReportAttachment = {
  key: string
  kind: 'photo' | 'video'
  previewUrl: string | null
  mediaId: number | null
  state: 'uploading' | 'ready' | 'error'
  progress: number | null
  controller: AbortController | null
}

type ReportAttachmentsProps = {
  value: ReportAttachment[]
  onChange: (update: (previous: ReportAttachment[]) => ReportAttachment[]) => void
}

export function ReportAttachments({ value, onChange }: ReportAttachmentsProps) {
  const { t } = useTranslation('suporte')
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
        if (item.kind === 'photo' && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
      }
    },
    [],
  )

  const patch = (key: string, changes: Partial<ReportAttachment>) =>
    onChange((previous) => previous.map((item) => (item.key === key ? { ...item, ...changes } : item)))

  const room = supportMaxMedia - value.length
  const hasVideo = value.some((item) => item.kind === 'video')

  const addPhotos = (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    setNotice(files.length > room ? t('attachments.limit', { max: supportMaxMedia }) : null)

    for (const file of files.slice(0, Math.max(room, 0))) {
      const check = checkImageFile(file, postPhotoMaxBytes)
      if (check !== 'ok') {
        setNotice(t(`attachments.photo.${check}`))
        continue
      }
      const key = crypto.randomUUID()
      onChange((previous) => [
        ...previous,
        { key, kind: 'photo', previewUrl: URL.createObjectURL(file), mediaId: null, state: 'uploading', progress: null, controller: null },
      ])
      uploadSupportPhoto(file)
        .then((media) => patch(key, { state: 'ready', mediaId: media.id }))
        .catch(() => patch(key, { state: 'error' }))
    }
  }

  const addVideo = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    const check = await checkVideoFile(file, supportVideoLimits)
    if (check !== 'ok') {
      setNotice(t(`attachments.video.${check}`))
      return
    }
    setNotice(null)

    const key = crypto.randomUUID()
    const controller = new AbortController()
    onChange((previous) => [
      ...previous,
      { key, kind: 'video', previewUrl: null, mediaId: null, state: 'uploading', progress: 0, controller },
    ])
    uploadSupportVideo(file, (loaded, total) => patch(key, { progress: Math.round((loaded / total) * 100) }), controller.signal)
      .then((media) => patch(key, { state: 'ready', mediaId: media.id, progress: 100, controller: null }))
      .catch(() => {
        if (!controller.signal.aborted) patch(key, { state: 'error', controller: null })
      })
  }

  const remove = (item: ReportAttachment) => {
    item.controller?.abort()
    if (item.mediaId !== null) void deleteMedia(item.mediaId).catch(() => undefined)
    if (item.kind === 'photo' && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
    onChange((previous) => previous.filter((other) => other.key !== item.key))
  }

  return (
    <fieldset className="flex flex-col gap-3">
      <legend className="text-sm font-medium">{t('attachments.title')}</legend>
      <p className="app-meta">{t('attachments.hint', { max: supportMaxMedia })}</p>

      {value.length > 0 ? (
        <ul className="grid grid-cols-3 gap-2 sm:grid-cols-6">
          {value.map((item) => (
            <li key={item.key} className="relative aspect-square overflow-hidden rounded-lg border bg-muted">
              {item.previewUrl ? (
                <img src={item.previewUrl} alt="" className="size-full object-cover" />
              ) : (
                <span className="flex size-full items-center justify-center text-muted-foreground">
                  <VideoIcon className="size-6" />
                </span>
              )}
              {item.state !== 'ready' ? (
                <span
                  className={cn(
                    'absolute inset-0 flex flex-col items-center justify-center gap-1 text-xs font-medium text-on-scrim tabular-nums',
                    item.state === 'error' ? 'bg-scrim/75' : 'bg-scrim/55',
                  )}
                >
                  {item.state === 'error' ? <CircleAlertIcon className="size-5" /> : <LoaderCircleIcon className="size-5 animate-spin" />}
                  {item.state === 'error' ? t('attachments.failed') : item.progress !== null ? `${item.progress}%` : null}
                </span>
              ) : item.kind === 'video' ? (
                <span className="absolute bottom-1 left-1 rounded bg-scrim/70 p-1 text-on-scrim">
                  <VideoIcon className="size-3.5" />
                </span>
              ) : null}
              <button
                type="button"
                onClick={() => remove(item)}
                aria-label={t('attachments.remove')}
                className="absolute top-1 right-1 flex size-7 items-center justify-center rounded-full bg-scrim/70 text-on-scrim hover:bg-scrim/90 focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
              >
                <XIcon className="size-4" />
              </button>
            </li>
          ))}
        </ul>
      ) : null}

      {notice ? <p className="text-sm text-destructive">{notice}</p> : null}

      <div className="flex flex-wrap gap-2">
        <input ref={photoInput} type="file" accept={acceptedImageTypes.join(',')} multiple className="hidden" onChange={addPhotos} />
        <input ref={videoInput} type="file" accept="video/*" className="hidden" onChange={(event) => void addVideo(event)} />
        <AppButton type="button" intent="neutral" onClick={() => photoInput.current?.click()} disabled={room <= 0}>
          <ImagePlusIcon />
          {t('attachments.addPhotos')}
        </AppButton>
        <AppButton type="button" intent="neutral" onClick={() => videoInput.current?.click()} disabled={room <= 0 || hasVideo}>
          <VideoIcon />
          {t('attachments.addVideo')}
        </AppButton>
      </div>
    </fieldset>
  )
}
