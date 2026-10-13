import { ImageUpIcon, Trash2Icon } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { useTranslation } from 'react-i18next'

import type { ProfileDto } from '@/api/types'
import { AppAvatar } from '@/components/app-avatar'
import { AppButton } from '@/components/app-button'
import { useAppConfirm } from '@/components/app-confirm'

import { acceptedImageTypes, avatarMaxBytes, checkImageFile } from '../profile-validators'
import { removeAvatar, setAvatar } from '../services/conta-service'

type AvatarUploaderProps = {
  user: ProfileDto
  onChange: (user: ProfileDto) => void
}

export function AvatarUploader({ user, onChange }: AvatarUploaderProps) {
  const { t } = useTranslation('conta')
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
      setError(t(`image.${check}`, { size: 5 }))
      return
    }
    setError(null)
    setFile(chosen)
  }

  const save = async () => {
    if (!file) return
    setSaving(true)
    try {
      onChange(await setAvatar(file))
      setFile(null)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  const remove = () =>
    confirm({
      title: t('avatar.removeTitle'),
      message: t('avatar.removeMessage'),
      confirmLabel: t('avatar.remove'),
      danger: true,
      onConfirm: async () => {
        onChange(await removeAvatar())
      },
    })

  return (
    <div className="flex flex-col gap-3">
      <span className="text-sm font-medium">{t('avatar.title')}</span>
      <div className="flex flex-wrap items-center gap-4">
        {preview ? (
          <img src={preview} alt={t('avatar.preview')} className="size-16 rounded-full object-cover ring-2 ring-primary" />
        ) : (
          <AppAvatar name={user.name} src={user.avatarUrl} size="xl" />
        )}
        <div className="flex flex-wrap gap-2">
          <input ref={inputRef} type="file" accept={acceptedImageTypes.join(',')} className="hidden" onChange={pick} />
          {file ? (
            <>
              <AppButton size="sm" loading={saving} onClick={() => void save()}>
                {t('avatar.save')}
              </AppButton>
              <AppButton size="sm" intent="ghost" disabled={saving} onClick={() => setFile(null)}>
                {t('common:actions.cancel')}
              </AppButton>
            </>
          ) : (
            <>
              <AppButton size="sm" intent="neutral" onClick={() => inputRef.current?.click()}>
                <ImageUpIcon />
                {t('avatar.choose')}
              </AppButton>
              {user.avatarUrl ? (
                <AppButton size="sm" intent="ghost" onClick={remove}>
                  <Trash2Icon />
                  {t('avatar.remove')}
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
        <p className="text-[13px] text-muted-foreground">{t('avatar.hint')}</p>
      )}
    </div>
  )
}
