import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppForm, AppSelectField, AppTextareaField } from '@/components/app-field'
import { allSupportPriorities, allSupportStatuses, useSupportLabels } from '@/pages/reportar-problema/support-enums'
import type { SupportTicketPriority, SupportTicketStatus } from '@/pages/reportar-problema/support-enums'

import { updateTicket } from '../services/suporte-admin-service'
import type { SupportTicketAdminDto } from '../services/suporte-admin-service'

const schema = z.object({
  status: z.string(),
  priority: z.string(),
  internalNote: z.string().max(5000),
})

type Values = z.infer<typeof schema>

type TriagePanelProps = {
  ticket: SupportTicketAdminDto
  onSaved: (ticket: SupportTicketAdminDto) => void
}

export function TriagePanel({ ticket, onSaved }: TriagePanelProps) {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const [saving, setSaving] = useState(false)
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      status: String(ticket.status),
      priority: String(ticket.priority),
      internalNote: ticket.internalNote ?? '',
    },
  })

  const submit = async (values: Values) => {
    setSaving(true)
    try {
      onSaved(
        await updateTicket(ticket.id, {
          status: Number(values.status) as SupportTicketStatus,
          priority: Number(values.priority) as SupportTicketPriority,
          internalNote: values.internalNote.trim() || null,
        }),
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <AppCard title={t('admin.triage.title')}>
      <AppForm form={form} onSubmit={submit}>
        <AppSelectField
          name="status"
          label={t('admin.triage.status')}
          options={allSupportStatuses.map((status) => ({ value: String(status), label: labels.status(status) }))}
        />
        <AppSelectField
          name="priority"
          label={t('admin.triage.priority')}
          options={allSupportPriorities.map((priority) => ({ value: String(priority), label: labels.priority(priority) }))}
        />
        <AppTextareaField
          name="internalNote"
          label={t('admin.triage.note')}
          description={t('admin.triage.noteHint')}
          rows={4}
          maxLength={5000}
        />
        <AppButton type="submit" loading={saving} className="self-start">
          {t('admin.triage.save')}
        </AppButton>
      </AppForm>
    </AppCard>
  )
}
