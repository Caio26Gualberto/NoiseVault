import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'
import { AppForm, AppTextareaField } from '@/components/app-field'

import { expressInterest } from '../services/ensaio-service'
import type { BandOpeningDto } from '../services/ensaio-service'

const schema = z.object({ message: z.string().trim().max(300, 'ensaio:validation.message') })

type FormValues = z.infer<typeof schema>

type InterestDialogProps = {
  opening: BandOpeningDto | null
  onOpenChange: (open: boolean) => void
  onSent: (opening: BandOpeningDto) => void
}

export function InterestDialog({ opening, onOpenChange, onSent }: InterestDialogProps) {
  const { t } = useTranslation('ensaio')
  const [sending, setSending] = useState(false)
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { message: '' } })

  useEffect(() => {
    if (opening) form.reset({ message: '' })
  }, [opening, form])

  const submit = async (values: FormValues) => {
    if (!opening) return
    setSending(true)
    try {
      onSent(await expressInterest(opening.id, values.message || null))
      onOpenChange(false)
    } catch {
      return
    } finally {
      setSending(false)
    }
  }

  return (
    <AppDialog
      open={opening !== null}
      onOpenChange={onOpenChange}
      title={t('interestDialog.title')}
      description={t('interestDialog.description', { band: opening?.band.name ?? '' })}
    >
      <AppForm form={form} onSubmit={submit}>
        <AppTextareaField name="message" label={t('interestDialog.message')} placeholder={t('interestDialog.messagePlaceholder')} rows={4} maxLength={300} />
        <div className="flex justify-end gap-2">
          <AppButton intent="ghost" type="button" onClick={() => onOpenChange(false)}>
            {t('common:actions.cancel')}
          </AppButton>
          <AppButton type="submit" loading={sending}>
            {t('interestDialog.submit')}
          </AppButton>
        </div>
      </AppForm>
    </AppDialog>
  )
}
