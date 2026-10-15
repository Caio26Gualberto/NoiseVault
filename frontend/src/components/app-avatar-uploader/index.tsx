import { ImageUpIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'
import { acceptedImageTypes, avatarMaxBytes, checkImageFile } from '@/lib/image-file'

type AppAvatarUploaderProps = {
  name: string
  avatarUrl: string | null
  onUpload: (file: File) => Promise<unknown>
  onRemove: () => Promise<unknown>
}

export function AppAvatarUploader({ name, avatarUrl, onUpload, onRemove }: AppAvatarUploaderProps) {
  const { t } = useTranslation()
  const confirm = useAppConfirm()
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
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

  const pick = (event: ChangeEvent<HTMLInputElement>) => {
    const chosen = event.target.files?.[0]
    event.target.value = ''
    if (!chosen) return
    const check = checkImageFile(chosen, avatarMaxBytes)
    if (check !== 'ok') {
      setFile(null)
      setError(t(`imageUpload.image.${check}`, { size: 5 }))
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
      title: t('imageUpload.avatar.removeTitle'),
      message: t('imageUpload.avatar.removeMessage'),
      confirmLabel: t('imageUpload.avatar.remove'),
      danger: true,
      onConfirm: async () => {
        await onRemove()
      },
    })

  return (
    <div className="flex flex-col gap-3">
      <span className="text-sm font-medium">{t('imageUpload.avatar.title')}</span>
      <div className="flex flex-wrap items-center gap-4">
        {preview ? (
          <img src={preview} alt={t('imageUpload.avatar.preview')} className="size-16 rounded-full object-cover ring-2 ring-primary" />
        ) : (
          <AppAvatar name={name} src={avatarUrl} size="xl" />
        )}
        <div className="flex flex-wrap gap-2">
          <input ref={inputRef} type="file" accept={acceptedImageTypes.join(',')} className="hidden" onChange={pick} />
          {file ? (
            <>
              <AppButton type="button" size="sm" loading={saving} onClick={() => void save()}>
                {t('imageUpload.avatar.save')}
              </AppButton>
              <AppButton type="button" size="sm" intent="ghost" disabled={saving} onClick={() => setFile(null)}>
                {t('actions.cancel')}
              </AppButton>
            </>
          ) : (
            <>
              <AppButton type="button" size="sm" intent="neutral" onClick={() => inputRef.current?.click()}>
                <ImageUpIcon />
                {t('imageUpload.avatar.choose')}
              </AppButton>
              {avatarUrl ? (
                <AppButton type="button" size="sm" intent="ghost" onClick={remove}>
                  <Trash2Icon />
                  {t('imageUpload.avatar.remove')}
                </AppButton>
              ) : null}
            </>
          )}
        </div>
      </div>
      {error ? (
        <p role="alert" className="text-[13px] text-destructive">
          {error}
        </p>
      ) : (
        <p className="text-[13px] text-muted-foreground">{t('imageUpload.avatar.hint')}</p>
      )}
    </div>
  )
}
