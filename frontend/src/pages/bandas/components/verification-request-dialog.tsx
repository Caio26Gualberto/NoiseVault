import { useState } from 'react'
import type { FormEvent } from 'react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'

import { bandVerificationMessageMaxLength, bandVerificationMessageMinLength, bandVerificationPhoneMaxLength, requestBandVerification } from '../services/bandas-service'

type VerificationRequestDialogProps = {
  slug: string
  open: boolean
  onOpenChange: (open: boolean) => void
  onRequested: () => void
}

export function VerificationRequestDialog({ slug, open, onOpenChange, onRequested }: VerificationRequestDialogProps) {
  const { t } = useTranslation('bandas')
  const [message, setMessage] = useState('')
  const [phone, setPhone] = useState('')
  const [saving, setSaving] = useState(false)

  const value = message.trim()
  const valid = value.length >= bandVerificationMessageMinLength && value.length <= bandVerificationMessageMaxLength

  const change = (next: boolean) => {
    if (!next) {
      setMessage('')
      setPhone('')
    }
    onOpenChange(next)
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!valid) return
    setSaving(true)
    try {
      await requestBandVerification(slug, { message: value, contactPhone: phone.trim() || null })
      onRequested()
      change(false)
    } catch {
      return
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppDialog open={open} onOpenChange={change} title={t('verification.dialogTitle')} description={t('verification.dialogDescription')}>
      <form onSubmit={(event) => void submit(event)} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">{t('verification.messageLabel')}</span>
          <textarea
            value={message}
            onChange={(event) => setMessage(event.target.value)}
            rows={5}
            maxLength={bandVerificationMessageMaxLength}
            placeholder={t('verification.messagePlaceholder')}
            className="w-full resize-y rounded-lg border bg-background px-3 py-2 text-[15px] outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
          />
          <span className="app-meta">{t('verification.messageHint', { min: bandVerificationMessageMinLength, max: bandVerificationMessageMaxLength })}</span>
        </label>
        <label className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">{t('verification.phoneLabel')}</span>
          <input
            type="tel"
            value={phone}
            onChange={(event) => setPhone(event.target.value)}
            maxLength={bandVerificationPhoneMaxLength}
            placeholder={t('verification.phonePlaceholder')}
            autoComplete="tel"
            className="h-11 w-full rounded-lg border bg-background px-3 text-[15px] outline-none focus-visible:ring-2 focus-visible:ring-ring/50 md:h-10"
          />
          <span className="app-meta">{t('verification.phoneHint')}</span>
        </label>
        <div className="flex justify-end gap-2">
          <AppButton type="button" intent="ghost" onClick={() => change(false)}>
            {t('verification.cancel')}
          </AppButton>
          <AppButton type="submit" loading={saving} disabled={!valid}>
            {t('verification.submit')}
          </AppButton>
        </div>
      </form>
    </AppDialog>
  )
}
