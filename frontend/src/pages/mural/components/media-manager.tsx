import { DndContext, KeyboardSensor, PointerSensor, TouchSensor, closestCenter, useSensor, useSensors } from '@dnd-kit/core'
import type { DragEndEvent } from '@dnd-kit/core'
import { SortableContext, arrayMove, sortableKeyboardCoordinates, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import {
  ArrowDownIcon,
  ArrowUpIcon,
  CircleAlertIcon,
  GripVerticalIcon,
  ImagePlusIcon,
  LoaderCircleIcon,
  RotateCcwIcon,
  StarIcon,
  VideoIcon,
  XIcon,
} from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton, AppIconButton } from '@/components/app-button'
import { AppMediaViewer } from '@/components/app-media-viewer'
import type { ViewerItem } from '@/components/app-media-viewer'
import { acceptedImageTypes, checkImageFile } from '@/lib/image-file'
import { createLimiter } from '@/lib/limit'
import { cn } from '@/lib/utils'
import { archivePhotoMaxBytes, archiveVideoLimits, checkVideoFile } from '@/lib/video-file'
import { deleteMedia, uploadVideo } from '@/pages/feed/services/media-service'

import { archiveMaxMedia } from '../managed-media'
import type { ManagedMedia } from '../managed-media'
import { archiveUploadPaths, uploadArchivePhoto } from '../services/mural-service'
import type { ArchiveUploadTarget } from '../services/mural-service'

const captionMaxLength = 500
const parallelUploads = 3

type MediaManagerProps = {
  value: ManagedMedia[]
  onChange: (update: (previous: ManagedMedia[]) => ManagedMedia[]) => void
  coverKey: string | null
  onCoverChange: (key: string) => void
  target: ArchiveUploadTarget
}

export function MediaManager({ value, onChange, coverKey, onCoverChange, target }: MediaManagerProps) {
  const { t } = useTranslation('mural')
  const photoInput = useRef<HTMLInputElement>(null)
  const videoInput = useRef<HTMLInputElement>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [viewerIndex, setViewerIndex] = useState<number | null>(null)
  const valueRef = useRef(value)
  const unmounted = useRef(false)
  const [limit] = useState(() => createLimiter(parallelUploads))
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 150, tolerance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  useEffect(() => {
    valueRef.current = value
  }, [value])

  useEffect(() => {
    unmounted.current = false
    return () => {
      unmounted.current = true
      for (const item of valueRef.current) {
        item.controller?.abort()
        if (item.localUrl && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
      }
    }
  }, [])

  const patch = (key: string, changes: Partial<ManagedMedia>) =>
    onChange((previous) => previous.map((item) => (item.key === key ? { ...item, ...changes } : item)))

  const stillHere = (key: string) => !unmounted.current && valueRef.current.some((item) => item.key === key)

  const startPhoto = (key: string, file: File) => {
    limit(async () => {
      if (!stillHere(key)) return
      try {
        const media = await uploadArchivePhoto(file, target)
        patch(key, { state: 'ready', mediaId: media.id, file: null })
      } catch {
        patch(key, { state: 'error' })
      }
    })
  }

  const startVideo = (key: string, file: File) => {
    const controller = new AbortController()
    patch(key, { controller, progress: 0 })
    limit(async () => {
      if (!stillHere(key) || controller.signal.aborted) return
      try {
        const media = await uploadVideo(file, (loaded, total) => patch(key, { progress: Math.round((loaded / total) * 100) }), controller.signal, archiveUploadPaths[target].videos)
        patch(key, { state: 'ready', mediaId: media.id, progress: 100, controller: null, file: null })
      } catch {
        if (!controller.signal.aborted) patch(key, { state: 'error', controller: null })
      }
    })
  }

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
      const item: ManagedMedia = { key, kind: 'photo', previewUrl: URL.createObjectURL(file), localUrl: true, mediaId: null, caption: '', state: 'uploading', progress: null, saved: false, controller: null, file, playUrl: null }
      valueRef.current = [...valueRef.current, item]
      onChange((previous) => [...previous, item])
      startPhoto(key, file)
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
      const item: ManagedMedia = { key, kind: 'video', previewUrl: null, localUrl: false, mediaId: null, caption: '', state: 'uploading', progress: 0, saved: false, controller: null, file, playUrl: null }
      valueRef.current = [...valueRef.current, item]
      onChange((previous) => [...previous, item])
      startVideo(key, file)
    }
  }

  const retry = (item: ManagedMedia) => {
    if (!item.file) return
    patch(item.key, { state: 'uploading', progress: item.kind === 'video' ? 0 : null })
    if (item.kind === 'photo') startPhoto(item.key, item.file)
    else startVideo(item.key, item.file)
  }

  const retryAll = () => value.filter((item) => item.state === 'error' && item.file).forEach(retry)

  const remove = (item: ManagedMedia) => {
    item.controller?.abort()
    if (!item.saved && item.mediaId !== null) void deleteMedia(item.mediaId).catch(() => undefined)
    if (item.localUrl && item.previewUrl) URL.revokeObjectURL(item.previewUrl)
    onChange((previous) => previous.filter((other) => other.key !== item.key))
  }

  const move = (index: number, step: number) => onChange((previous) => arrayMove(previous, index, index + step))

  const onDragEnd = ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) return
    onChange((previous) => {
      const from = previous.findIndex((item) => item.key === active.id)
      const to = previous.findIndex((item) => item.key === over.id)
      return from < 0 || to < 0 ? previous : arrayMove(previous, from, to)
    })
  }

  const viewable = value.filter((item) => item.previewUrl && (item.kind === 'photo' || item.playUrl))
  const viewerItems: ViewerItem[] = viewable.map((item) => ({
    id: item.key,
    kind: item.kind,
    url: item.kind === 'photo' ? item.previewUrl! : item.playUrl!,
    posterUrl: item.kind === 'video' ? item.previewUrl : null,
    caption: item.caption || null,
  }))
  const openViewer = (key: string) => {
    const index = viewable.findIndex((item) => item.key === key)
    if (index >= 0) setViewerIndex(index)
  }

  const effectiveCover = coverKey ?? value.find((item) => item.state === 'ready')?.key ?? null
  const failedCount = value.filter((item) => item.state === 'error' && item.file).length

  return (
    <fieldset className="flex flex-col gap-3">
      <legend className="text-sm font-medium">{t('form.media.title')}</legend>
      <p className="app-meta">{t('form.media.hint')}</p>
      {value.length > 1 ? <p className="app-meta">{t('form.media.dragHint')}</p> : null}

      {value.length > 0 ? (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
          <SortableContext items={value.map((item) => item.key)} strategy={verticalListSortingStrategy}>
            <ol className="flex flex-col gap-2">
              {value.map((item, index) => (
                <SortableMedia
                  key={item.key}
                  item={item}
                  index={index}
                  total={value.length}
                  isCover={effectiveCover === item.key}
                  onPatch={patch}
                  onCover={onCoverChange}
                  onMove={move}
                  onRemove={remove}
                  onRetry={retry}
                  onOpen={viewable.some((other) => other.key === item.key) ? openViewer : undefined}
                />
              ))}
            </ol>
          </SortableContext>
        </DndContext>
      ) : null}

      {notice ? <p className="text-sm text-destructive">{notice}</p> : null}

      {failedCount > 1 ? (
        <AppButton type="button" intent="secondary" size="sm" onClick={retryAll} className="self-start">
          <RotateCcwIcon />
          {t('form.media.retryAll', { count: failedCount })}
        </AppButton>
      ) : null}

      <AppMediaViewer items={viewerItems} index={viewerIndex} onIndexChange={setViewerIndex} />

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

type SortableMediaProps = {
  item: ManagedMedia
  index: number
  total: number
  isCover: boolean
  onPatch: (key: string, changes: Partial<ManagedMedia>) => void
  onCover: (key: string) => void
  onMove: (index: number, step: number) => void
  onRemove: (item: ManagedMedia) => void
  onRetry: (item: ManagedMedia) => void
  onOpen?: (key: string) => void
}

function SortableMedia({ item, index, total, isCover, onPatch, onCover, onMove, onRemove, onRetry, onOpen }: SortableMediaProps) {
  const { t } = useTranslation('mural')
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({ id: item.key })

  return (
    <li
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn('flex gap-2 rounded-lg border bg-background p-2', isCover && 'border-primary/60', isDragging && 'relative z-10 shadow-lg ring-2 ring-ring/50')}
    >
      <button
        ref={setActivatorNodeRef}
        type="button"
        className="flex w-6 shrink-0 cursor-grab touch-none items-center justify-center rounded text-muted-foreground hover:bg-accent hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none active:cursor-grabbing"
        aria-label={t('form.media.drag', { position: index + 1 })}
        {...attributes}
        {...listeners}
      >
        <GripVerticalIcon className="size-4" />
      </button>
      <div className="relative size-20 shrink-0 overflow-hidden rounded-md bg-muted sm:size-24">
        {item.previewUrl && onOpen ? (
          <button
            type="button"
            onClick={() => onOpen(item.key)}
            aria-label={t('form.media.open', { position: index + 1 })}
            className="size-full cursor-zoom-in focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
          >
            <img src={item.previewUrl} alt="" className="size-full object-cover" draggable={false} />
          </button>
        ) : item.previewUrl ? (
          <img src={item.previewUrl} alt="" className="size-full object-cover" draggable={false} />
        ) : null}
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
          onChange={(event) => onPatch(item.key, { caption: event.target.value })}
          placeholder={t('form.media.captionPlaceholder')}
          aria-label={t('form.media.caption')}
          className="h-9 w-full rounded-md border bg-transparent px-2.5 text-sm placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        />
        <div className="flex flex-wrap items-center gap-1">
          {item.state === 'error' && item.file ? (
            <AppButton type="button" intent="secondary" size="sm" onClick={() => onRetry(item)}>
              <RotateCcwIcon />
              {t('form.media.retry')}
            </AppButton>
          ) : (
            <AppButton
              type="button"
              intent={isCover ? 'primary' : 'ghost'}
              size="sm"
              disabled={item.state !== 'ready'}
              onClick={() => onCover(item.key)}
              aria-pressed={isCover}
            >
              <StarIcon className={cn(isCover && 'fill-current')} />
              {isCover ? t('form.media.isCover') : t('form.media.makeCover')}
            </AppButton>
          )}
          <span className="ml-auto flex items-center">
            <span className="app-meta mr-1 tabular-nums">{index + 1}</span>
            <AppIconButton type="button" label={t('form.media.up')} size="sm" disabled={index === 0} onClick={() => onMove(index, -1)}>
              <ArrowUpIcon />
            </AppIconButton>
            <AppIconButton type="button" label={t('form.media.down')} size="sm" disabled={index === total - 1} onClick={() => onMove(index, 1)}>
              <ArrowDownIcon />
            </AppIconButton>
            <AppIconButton type="button" label={t('form.media.remove')} size="sm" onClick={() => onRemove(item)}>
              <XIcon />
            </AppIconButton>
          </span>
        </div>
      </div>
    </li>
  )
}
