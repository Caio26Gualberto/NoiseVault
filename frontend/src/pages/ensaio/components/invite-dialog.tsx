import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'
import { AppField, AppForm, AppSelectField } from '@/components/app-field'
import { inviteBandMember } from '@/pages/bandas/services/bandas-service'
import type { BandSummaryDto } from '@/pages/bandas/services/bandas-service'

const schema = z.object({
  band: z.string().min(1, 'ensaio:validation.band'),
  instrument: z.string().trim().max(60, 'validation.instrumentLength'),
})

type FormValues = z.infer<typeof schema>

type InviteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  bands: BandSummaryDto[]
  person: { name: string; nickname: string } | null
  instrument: string
}

export function InviteDialog({ open, onOpenChange, bands, person, instrument }: InviteDialogProps) {
  const { t } = useTranslation('ensaio')
  const [sending, setSending] = useState(false)
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { band: '', instrument: '' } })

  useEffect(() => {
    if (open) form.reset({ band: bands.length === 1 ? bands[0].slug : '', instrument })
  }, [open, bands, instrument, form])

  const submit = async (values: FormValues) => {
    if (!person) return
    setSending(true)
    try {
      await inviteBandMember(values.band, person.nickname, values.instrument || null)
      onOpenChange(false)
    } catch {
      return
    } finally {
      setSending(false)
    }
  }

  return (
    <AppDialog open={open} onOpenChange={onOpenChange} title={t('inviteDialog.title', { name: person?.name ?? '' })} description={t('inviteDialog.description')}>
      <AppForm form={form} onSubmit={submit}>
        <AppSelectField name="band" label={t('inviteDialog.band')} options={bands.map((band) => ({ value: band.slug, label: band.name }))} />
        <AppField name="instrument" label={t('inviteDialog.instrument')} />
        <div className="flex justify-end gap-2">
          <AppButton intent="ghost" type="button" onClick={() => onOpenChange(false)}>
            {t('common:actions.cancel')}
          </AppButton>
          <AppButton type="submit" loading={sending}>
            {t('inviteDialog.submit')}
          </AppButton>
        </div>
      </AppForm>
    </AppDialog>
  )
}
