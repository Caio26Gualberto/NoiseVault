import { ImageUpIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { acceptedImageTypes, checkImageFile } from '@/lib/image-file'

const maxBytes = 10 * 1024 * 1024

type FlyerPhotoUploaderProps = {
  photoUrl: string | null
  fallbackUrl: string | null
  disabled: boolean
  onUpload: (file: File) => Promise<unknown>
  onRemove: () => Promise<unknown>
  showPreview?: boolean
  onPreviewChange?: (url: string | null) => void
}

export function FlyerPhotoUploader({ photoUrl, fallbackUrl, disabled, onUpload, onRemove, showPreview = true, onPreviewChange }: FlyerPhotoUploaderProps) {
  const { t } = useTranslation('ensaio')
  const confirm = useAppConfirm()
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!file) {
      setPreview(null)
      onPreviewChange?.(null)
      return
    }
    const url = URL.createObjectURL(file)
    setPreview(url)
    onPreviewChange?.(url)
    return () => URL.revokeObjectURL(url)
  }, [file, onPreviewChange])

  const pick = (event: ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files?.[0]
    event.target.value = ''
    if (!chosen) return
    const check = checkImageFile(chosen, maxBytes)
    if (check !== 'ok') {
      setFile(null)
      setError(t(`common:imageUpload.image.${check}`, { size: 10 }))
      return
    }
    setError(null)
    setFile(chosen)
  }

  const save = async () => {
    if (!file) return
    setSaving(true)
    try {
      await onUpload(file)
      setFile(null)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const remove = () =>
    confirm({
      title: t('photo.removeTitle'),
      message: t('photo.removeMessage'),
      confirmLabel: t('photo.remove'),
      danger: true,
      onConfirm: async () => {
        await onRemove()
      },
    })

  const shown = preview ?? photoUrl ?? fallbackUrl

  return (
    <div className="flex flex-col gap-3">
      <span className="text-sm font-medium">{t('photo.title')}</span>
      <div className="ensaio flex flex-col gap-4 sm:flex-row sm:items-center">
        {showPreview ? (
          <span className="ensaio-halftone relative h-40 w-full shrink-0 overflow-hidden sm:w-72">
            {shown ? <img src={shown} alt={t('photo.preview')} className="size-full object-cover mix-blend-multiply grayscale contrast-125" /> : null}
          </span>
        ) : null}
        <div className="flex flex-col gap-2">
          <input ref={inputRef} type="file" accept={acceptedImageTypes.join(',')} className="hidden" onChange={pick} />
          {file ? (
            <div className="flex flex-wrap gap-2">
              <AppButton type="button" size="sm" loading={saving} onClick={() => void save()}>
                {t('photo.save')}
              </AppButton>
              <AppButton type="button" size="sm" intent="ghost" disabled={saving} onClick={() => setFile(null)}>
                {t('common:actions.cancel')}
              </AppButton>
            </div>
          ) : (
            <div className="flex flex-wrap gap-2">
              <AppButton type="button" size="sm" intent="neutral" disabled={disabled} onClick={() => inputRef.current?.click()}>
                <ImageUpIcon />
                {t(photoUrl ? 'photo.change' : 'photo.choose')}
              </AppButton>
              {photoUrl ? (
                <AppButton type="button" size="sm" intent="ghost" onClick={remove}>
                  <Trash2Icon />
                  {t('photo.remove')}
                </AppButton>
              ) : null}
            </div>
          )}
          {error ? (
            <p role="alert" className="text-[13px] text-destructive">
              {error}
            </p>
          ) : (
            <p className="text-[13px] text-muted-foreground">{t(disabled ? 'photo.saveFirst' : photoUrl ? 'photo.hint' : 'photo.fallbackHint')}</p>
          )}
        </div>
      </div>
    </div>
  )
}
