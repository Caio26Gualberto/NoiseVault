import { ImageUpIcon, MoveVerticalIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, KeyboardEvent, PointerEvent, SyntheticEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { acceptedImageTypes, bannerMaxBytes, bannerMinWidth, checkImageFile } from '@/lib/image-file'
import { cn } from '@/lib/utils'

type SavedBanner = { bannerPosition: number }

type AppBannerEditorProps = {
  bannerUrl: string | null
  bannerPosition: number
  onUpload: (file: File, position: number) => Promise<SavedBanner>
  onPosition: (position: number) => Promise<SavedBanner>
  onRemove: () => Promise<SavedBanner>
}

type Drag = { pointerId: number; startY: number; startPosition: number; overflow: number }

const keyboardStep = 2

function clamp(value: number) {
  return Math.min(100, Math.max(0, Math.round(value)))
}

export function AppBannerEditor({ bannerUrl, bannerPosition, onUpload, onPosition, onRemove }: AppBannerEditorProps) {
  const { t } = useTranslation()
  const confirm = useAppConfirm()
  const inputRef = useRef<HTMLInputElement>(null)
  const frameRef = useRef<HTMLDivElement>(null)
  const dragRef = useRef<Drag | null>(null)
  const naturalRef = useRef<{ width: number; height: number } | null>(null)
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [position, setPosition] = useState(bannerPosition)
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!file) {
      setPreview(null)
      return
    }
    const url = URL.createObjectURL(file)
    setPreview(url)
    return () => URL.revokeObjectURL(url)
  }, [file])

  const source = preview ?? bannerUrl
  const positionChanged = !file && Boolean(bannerUrl) && position !== bannerPosition

  const pick = (event: ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files?.[0]
    event.target.value = ''
    if (!chosen) return
    const check = checkImageFile(chosen, bannerMaxBytes)
    if (check !== 'ok') {
      setError(t(`imageUpload.image.${check}`, { size: 10 }))
      return
    }
    setError(null)
    setPosition(50)
    setFile(chosen)
  }

  const handleLoad = (event: SyntheticEvent<HTMLImageElement>) => {
    const image = event.currentTarget
    naturalRef.current = { width: image.naturalWidth, height: image.naturalHeight }
    if (file && image.naturalWidth < bannerMinWidth) {
      setError(t('imageUpload.image.tooNarrow', { width: bannerMinWidth }))
      setFile(null)
      setPosition(bannerPosition)
    }
  }

  const overflowOf = () => {
    const frame = frameRef.current
    const natural = naturalRef.current
    if (!frame || !natural || natural.width === 0) return 0
    const renderedHeight = Math.max(frame.clientHeight, (frame.clientWidth * natural.height) / natural.width)
    return renderedHeight - frame.clientHeight
  }

  const startDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (!source) return
    const overflow = overflowOf()
    if (overflow <= 0) return
    event.currentTarget.setPointerCapture(event.pointerId)
    dragRef.current = { pointerId: event.pointerId, startY: event.clientY, startPosition: position, overflow }
    setDragging(true)
  }

  const moveDrag = (event: PointerEvent<HTMLDivElement>) => {
    const drag = dragRef.current
    if (!drag || drag.pointerId !== event.pointerId) return
    const delta = event.clientY - drag.startY
    setPosition(clamp(drag.startPosition - (delta / drag.overflow) * 100))
  }

  const endDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (dragRef.current?.pointerId !== event.pointerId) return
    dragRef.current = null
    setDragging(false)
  }

  const handleKey = (event: KeyboardEvent<HTMLDivElement>) => {
    const next = {
      ArrowUp: position + keyboardStep,
      ArrowDown: position - keyboardStep,
      Home: 0,
      End: 100,
    }[event.key]
    if (next === undefined) return
    event.preventDefault()
    setPosition(clamp(next))
  }

  const run = async (action: () => Promise<SavedBanner>) => {
    setSaving(true)
    try {
      const saved = await action()
      setFile(null)
      setPosition(saved.bannerPosition)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const cancel = () => {
    setFile(null)
    setError(null)
    setPosition(bannerPosition)
  }

  const remove = () =>
    confirm({
      title: t('imageUpload.banner.removeTitle'),
      message: t('imageUpload.banner.removeMessage'),
      confirmLabel: t('imageUpload.banner.remove'),
      danger: true,
      onConfirm: async () => {
        const saved = await onRemove()
        setPosition(saved.bannerPosition)
      },
    })

  return (
    <div className="flex flex-col gap-3">
      <span className="text-sm font-medium">{t('imageUpload.banner.title')}</span>
      <div
        ref={frameRef}
        role={source ? 'slider' : undefined}
        tabIndex={source ? 0 : undefined}
        aria-label={source ? t('imageUpload.banner.positionLabel') : undefined}
        aria-orientation={source ? 'vertical' : undefined}
        aria-valuemin={source ? 0 : undefined}
        aria-valuemax={source ? 100 : undefined}
        aria-valuenow={source ? position : undefined}
        onPointerDown={startDrag}
        onPointerMove={moveDrag}
        onPointerUp={endDrag}
        onPointerCancel={endDrag}
        onKeyDown={source ? handleKey : undefined}
        className={cn(
          'group relative aspect-[3/1] w-full touch-none overflow-hidden rounded-xl border bg-muted select-none focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none md:aspect-[4/1]',
          source && (dragging ? 'cursor-grabbing' : 'cursor-grab'),
        )}
      >
        {source ? (
          <>
            <img
              src={source}
              alt=""
              draggable={false}
              onLoad={handleLoad}
              className="pointer-events-none size-full object-cover"
              style={{ objectPosition: `center ${position}%` }}
            />
            <span className="pointer-events-none absolute right-2 bottom-2 inline-flex items-center gap-1.5 rounded-full bg-scrim/70 px-2.5 py-1 text-xs text-on-scrim">
              <MoveVerticalIcon className="size-3.5" />
              {t('imageUpload.banner.dragHint')}
            </span>
          </>
        ) : (
          <div className="flex size-full items-center justify-center text-sm text-muted-foreground">{t('imageUpload.banner.empty')}</div>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <input ref={inputRef} type="file" accept={acceptedImageTypes.join(',')} className="hidden" onChange={pick} />
        {file ? (
          <>
            <AppButton type="button" size="sm" loading={saving} onClick={() => void run(() => onUpload(file, position))}>
              {t('imageUpload.banner.save')}
            </AppButton>
            <AppButton type="button" size="sm" intent="ghost" disabled={saving} onClick={cancel}>
              {t('actions.cancel')}
            </AppButton>
          </>
        ) : (
          <>
            {positionChanged ? (
              <>
                <AppButton type="button" size="sm" loading={saving} onClick={() => void run(() => onPosition(position))}>
                  {t('imageUpload.banner.savePosition')}
                </AppButton>
                <AppButton type="button" size="sm" intent="ghost" disabled={saving} onClick={cancel}>
                  {t('actions.cancel')}
                </AppButton>
              </>
            ) : null}
            <AppButton type="button" size="sm" intent="neutral" disabled={saving} onClick={() => inputRef.current?.click()}>
              <ImageUpIcon />
              {bannerUrl ? t('imageUpload.banner.replace') : t('imageUpload.banner.choose')}
            </AppButton>
            {bannerUrl ? (
              <AppButton type="button" size="sm" intent="ghost" disabled={saving} onClick={remove}>
                <Trash2Icon />
                {t('imageUpload.banner.remove')}
              </AppButton>
            ) : null}
          </>
        )}
      </div>
      {error ? (
        <p role="alert" className="text-[13px] text-destructive">
          {error}
        </p>
      ) : (
        <p className="text-[13px] text-muted-foreground">{t('imageUpload.banner.hint')}</p>
      )}
    </div>
  )
}
