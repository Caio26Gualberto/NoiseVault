import { ImageUpIcon, MoveVerticalIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, KeyboardEvent, PointerEvent, SyntheticEvent } from 'react'
import { useTranslation } from 'react-i18next'

import type { ProfileDto } from '@/api/types'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { cn } from '@/lib/utils'

import { acceptedImageTypes, bannerMaxBytes, bannerMinWidth, checkImageFile } from '../profile-validators'
import { removeBanner, setBanner, setBannerPosition } from '../services/conta-service'

type BannerEditorProps = {
  user: ProfileDto
  onChange: (user: ProfileDto) => void
}

type Drag = { pointerId: number; startY: number; startPosition: number; overflow: number }

const keyboardStep = 2

function clamp(value: number) {
  return Math.min(100, Math.max(0, Math.round(value)))
}

export function BannerEditor({ user, onChange }: BannerEditorProps) {
  const { t } = useTranslation('conta')
  const confirm = useAppConfirm()
  const inputRef = useRef<HTMLInputElement>(null)
  const frameRef = useRef<HTMLDivElement>(null)
  const dragRef = useRef<Drag | null>(null)
  const naturalRef = useRef<{ width: number; height: number } | null>(null)
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [position, setPosition] = useState(user.bannerPosition)
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

  const source = preview ?? user.bannerUrl
  const positionChanged = !file && Boolean(user.bannerUrl) && position !== user.bannerPosition

  const pick = (event: ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files?.[0]
    event.target.value = ''
    if (!chosen) return
    const check = checkImageFile(chosen, bannerMaxBytes)
    if (check !== 'ok') {
      setError(t(`image.${check}`, { size: 10 }))
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
      setError(t('image.tooNarrow', { width: bannerMinWidth }))
      setFile(null)
      setPosition(user.bannerPosition)
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

  const run = async (action: () => Promise<ProfileDto>) => {
    setSaving(true)
    try {
      const updated = await action()
      onChange(updated)
      setFile(null)
      setPosition(updated.bannerPosition)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const cancel = () => {
    setFile(null)
    setError(null)
    setPosition(user.bannerPosition)
  }

  const remove = () =>
    confirm({
      title: t('banner.removeTitle'),
      message: t('banner.removeMessage'),
      confirmLabel: t('banner.remove'),
      danger: true,
      onConfirm: async () => {
        const updated = await removeBanner()
        onChange(updated)
        setPosition(updated.bannerPosition)
      },
    })

  return (
    <div className="flex flex-col gap-3">
      <span className="text-sm font-medium">{t('banner.title')}</span>
      <div
        ref={frameRef}
        role={source ? 'slider' : undefined}
        tabIndex={source ? 0 : undefined}
        aria-label={source ? t('banner.positionLabel') : undefined}
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
              {t('banner.dragHint')}
            </span>
          </>
        ) : (
          <div className="flex size-full items-center justify-center text-sm text-muted-foreground">{t('banner.empty')}</div>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <input ref={inputRef} type="file" accept={acceptedImageTypes.join(',')} className="hidden" onChange={pick} />
        {file ? (
          <>
            <AppButton size="sm" loading={saving} onClick={() => void run(() => setBanner(file, position))}>
              {t('banner.save')}
            </AppButton>
            <AppButton size="sm" intent="ghost" disabled={saving} onClick={cancel}>
              {t('common:actions.cancel')}
            </AppButton>
          </>
        ) : (
          <>
            {positionChanged ? (
              <>
                <AppButton size="sm" loading={saving} onClick={() => void run(() => setBannerPosition(position))}>
                  {t('banner.savePosition')}
                </AppButton>
                <AppButton size="sm" intent="ghost" disabled={saving} onClick={cancel}>
                  {t('common:actions.cancel')}
                </AppButton>
              </>
            ) : null}
            <AppButton size="sm" intent="neutral" disabled={saving} onClick={() => inputRef.current?.click()}>
              <ImageUpIcon />
              {user.bannerUrl ? t('banner.replace') : t('banner.choose')}
            </AppButton>
            {user.bannerUrl ? (
              <AppButton size="sm" intent="ghost" disabled={saving} onClick={remove}>
                <Trash2Icon />
                {t('banner.remove')}
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
        <p className="text-[13px] text-muted-foreground">{t('banner.hint')}</p>
      )}
    </div>
  )
}
