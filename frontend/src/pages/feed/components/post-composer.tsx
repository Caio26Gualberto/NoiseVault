import { CircleAlertIcon, ImagePlusIcon, LoaderCircleIcon, VideoIcon, XIcon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { useAuth } from '@/auth/use-auth'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton, AppIconButton } from '@/components/app-button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { acceptedImageTypes, checkImageFile } from '@/lib/image-file'
import { cn } from '@/lib/utils'
import { checkVideoFile, postPhotoMaxBytes } from '@/lib/video-file'
import { getMyBands } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'

import { createPost } from '../services/feed-service'
import type { PostDto } from '../services/feed-service'
import { deleteMedia, uploadPhoto, uploadVideo } from '../services/media-service'
import { postContentMaxLength } from './post-edit-dialog'

const maxPhotos = 10
const maxVideos = 1
const asSelf = 'self'

type Attachment = {
  key: string
  kind: 'photo' | 'video'
  previewUrl: string
  progress: number | null
  state: 'uploading' | 'ready' | 'error'
  mediaId: number | null
  controller: AbortController | null
}

type PostComposerProps = {
  onPublished: (post: PostDto) => void
}

export function PostComposer({ onPublished }: PostComposerProps) {
  const { t } = useTranslation('feed')
  const { user } = useAuth()
  const [content, setContent] = useState('')
  const [publishing, setPublishing] = useState(false)
  const [attachments, setAttachments] = useState<Attachment[]>([])
  const [notice, setNotice] = useState<string | null>(null)
  const [bands, setBands] = useState<BandSummaryDto[]>([])
  const [publishAs, setPublishAs] = useState(asSelf)
  const photoInput = useRef<HTMLInputElement>(null)
  const videoInput = useRef<HTMLInputElement>(null)
  const attachmentsRef = useRef(attachments)

  useEffect(() => {
    attachmentsRef.current = attachments
  }, [attachments])

  useEffect(() => {
    let cancelled = false
    getMyBands()
      .then((data) => {
        if (!cancelled) setBands(data)
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(
    () => () => {
      for (const attachment of attachmentsRef.current) {
        attachment.controller?.abort()
        URL.revokeObjectURL(attachment.previewUrl)
      }
    },
    [],
  )

  const patch = (key: string, changes: Partial<Attachment>) =>
    setAttachments((previous) => previous.map((item) => (item.key === key ? { ...item, ...changes } : item)))

  const active = attachments.filter((item) => item.state !== 'error')
  const photoCount = active.filter((item) => item.kind === 'photo').length
  const videoCount = active.filter((item) => item.kind === 'video').length
  const uploading = attachments.some((item) => item.state === 'uploading')
  const readyIds = attachments.filter((item) => item.state === 'ready' && item.mediaId !== null).map((item) => item.mediaId!)
  const value = content.trim()
  const canPublish = !uploading && (value.length > 0 || readyIds.length > 0)

  const addPhotos = (event: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? [])
    event.target.value = ''
    setNotice(null)

    const room = maxPhotos - photoCount
    if (files.length > room) setNotice(t('composer.limit', { photos: maxPhotos, videos: maxVideos }))

    for (const file of files.slice(0, Math.max(room, 0))) {
      const check = checkImageFile(file, postPhotoMaxBytes)
      if (check !== 'ok') {
        setNotice(t(`composer.photo.${check}`, { size: 15 }))
        continue
      }

      const key = crypto.randomUUID()
      setAttachments((previous) => [
        ...previous,
        { key, kind: 'photo', previewUrl: URL.createObjectURL(file), progress: null, state: 'uploading', mediaId: null, controller: null },
      ])

      uploadPhoto(file)
        .then((media) => patch(key, { state: 'ready', mediaId: media.id }))
        .catch(() => patch(key, { state: 'error' }))
    }
  }

  const addVideo = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    setNotice(null)
    if (!file) return

    if (videoCount >= maxVideos) {
      setNotice(t('composer.limit', { photos: maxPhotos, videos: maxVideos }))
      return
    }

    const check = await checkVideoFile(file)
    if (check !== 'ok') {
      setNotice(t(`composer.video.${check}`, { size: 500, minutes: 10 }))
      return
    }

    const key = crypto.randomUUID()
    const controller = new AbortController()
    setAttachments((previous) => [
      ...previous,
      { key, kind: 'video', previewUrl: URL.createObjectURL(file), progress: 0, state: 'uploading', mediaId: null, controller },
    ])

    uploadVideo(file, (loaded, total) => patch(key, { progress: Math.round((loaded / total) * 100) }), controller.signal)
      .then((media) => patch(key, { state: 'ready', mediaId: media.id, progress: 100, controller: null }))
      .catch(() => {
        if (!controller.signal.aborted) patch(key, { state: 'error', controller: null })
      })
  }

  const removeAttachment = (attachment: Attachment) => {
    attachment.controller?.abort()
    if (attachment.mediaId !== null) void deleteMedia(attachment.mediaId).catch(() => undefined)
    URL.revokeObjectURL(attachment.previewUrl)
    setAttachments((previous) => previous.filter((item) => item.key !== attachment.key))
  }

  const reset = () => {
    for (const attachment of attachments) URL.revokeObjectURL(attachment.previewUrl)
    setAttachments([])
    setContent('')
    setNotice(null)
    setPublishAs(asSelf)
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!canPublish) return
    setPublishing(true)
    try {
      const post = await createPost({ content: value, bandSlug: publishAs === asSelf ? null : publishAs, mediaIds: readyIds })
      reset()
      onPublished(post)
    } catch {
      return
    } finally {
      setPublishing(false)
    }
  }

  if (!user) return null

  const firstName = user.name.split(' ')[0]

  return (
    <form onSubmit={(event) => void submit(event)} className="flex gap-3 rounded-xl border bg-card p-4">
      <AppAvatar name={user.name} src={user.avatarUrl} size="lg" className="hidden sm:flex" />
      <div className="flex min-w-0 flex-1 flex-col gap-3">
        {bands.length > 0 ? (
          <Select value={publishAs} onValueChange={setPublishAs}>
            <SelectTrigger className="h-8 w-full sm:w-64" aria-label={t('composer.publishAs')}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={asSelf}>{t('composer.asSelf', { name: user.name })}</SelectItem>
              {bands.map((band) => (
                <SelectItem key={band.slug} value={band.slug}>
                  {t('composer.asBand', { name: band.name })}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : null}

        <textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          rows={2}
          maxLength={postContentMaxLength}
          placeholder={t('composer.placeholder', { name: firstName })}
          aria-label={t('composer.placeholder', { name: firstName })}
          className="min-h-14 w-full resize-none bg-transparent text-[15px] outline-none placeholder:text-muted-foreground"
        />

        {attachments.length > 0 ? (
          <ul className="grid grid-cols-3 gap-2 sm:grid-cols-4">
            {attachments.map((attachment) => (
              <li key={attachment.key} className="relative aspect-square overflow-hidden rounded-lg border bg-muted">
                {attachment.kind === 'photo' ? (
                  <img src={attachment.previewUrl} alt="" className="size-full object-cover" />
                ) : (
                  <video src={attachment.previewUrl} muted playsInline preload="metadata" className="size-full object-cover" />
                )}
                {attachment.state !== 'ready' ? (
                  <span
                    className={cn(
                      'absolute inset-0 flex flex-col items-center justify-center gap-1 text-xs font-medium text-on-scrim tabular-nums',
                      attachment.state === 'error' ? 'bg-scrim/70' : 'bg-scrim/50',
                    )}
                  >
                    {attachment.state === 'error' ? (
                      <>
                        <CircleAlertIcon className="size-5" />
                        {t('composer.uploadFailed')}
                      </>
                    ) : (
                      <>
                        <LoaderCircleIcon className="size-5 animate-spin" />
                        {attachment.progress !== null ? `${attachment.progress}%` : null}
                      </>
                    )}
                  </span>
                ) : attachment.kind === 'video' ? (
                  <span className="absolute bottom-1 left-1 rounded bg-scrim/70 px-1.5 py-0.5 text-xs text-on-scrim">
                    <VideoIcon className="size-3.5" />
                  </span>
                ) : null}
                <button
                  type="button"
                  onClick={() => removeAttachment(attachment)}
                  aria-label={t('composer.remove')}
                  className="absolute top-1 right-1 flex size-6 items-center justify-center rounded-full bg-scrim/70 text-on-scrim focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
                >
                  <XIcon className="size-3.5" />
                </button>
              </li>
            ))}
          </ul>
        ) : null}

        {notice ? <p className="text-sm text-destructive">{notice}</p> : null}

        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-1">
            <input ref={photoInput} type="file" accept={acceptedImageTypes.join(',')} multiple className="hidden" onChange={addPhotos} />
            <input ref={videoInput} type="file" accept="video/*" className="hidden" onChange={(event) => void addVideo(event)} />
            <AppIconButton type="button" label={t('composer.addPhotos')} onClick={() => photoInput.current?.click()} disabled={photoCount >= maxPhotos}>
              <ImagePlusIcon />
            </AppIconButton>
            <AppIconButton type="button" label={t('composer.addVideo')} onClick={() => videoInput.current?.click()} disabled={videoCount >= maxVideos}>
              <VideoIcon />
            </AppIconButton>
          </div>
          <AppButton type="submit" size="sm" loading={publishing} disabled={!canPublish}>
            {uploading ? t('composer.waitUpload') : t('composer.publish')}
          </AppButton>
        </div>
      </div>
    </form>
  )
}
