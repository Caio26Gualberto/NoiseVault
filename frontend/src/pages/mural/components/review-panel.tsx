import { zodResolver } from '@hookform/resolvers/zod'
import { CheckIcon, PencilIcon, XIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'
import { AppForm, AppTextareaField } from '@/components/app-field'
import { approveSubmission, rejectSubmission } from '@/pages/admin/mural/services/mural-admin-service'

import type { ArchiveEntryDto } from '../services/mural-service'

const schema = z.object({
  reason: z.string().trim().min(3, 'validation.required').max(500),
})

type Values = z.infer<typeof schema>

type ReviewPanelProps = {
  entry: ArchiveEntryDto
  onReviewed: (entry: ArchiveEntryDto) => void
}

export function ReviewPanel({ entry, onReviewed }: ReviewPanelProps) {
  const { t } = useTranslation('mural')
  const [approving, setApproving] = useState(false)
  const [rejecting, setRejecting] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { reason: '' } })

  const approve = async () => {
    setApproving(true)
    try {
      onReviewed(await approveSubmission(entry.id))
    } catch {
      return
    } finally {
      setApproving(false)
    }
  }

  const reject = async (values: Values) => {
    setRejecting(true)
    try {
      onReviewed(await rejectSubmission(entry.id, values.reason))
      setDialogOpen(false)
    } catch {
      return
    } finally {
      setRejecting(false)
    }
  }

  return (
    <section className="flex flex-col gap-3 rounded-xl border border-primary/40 bg-primary/[0.04] p-4 md:p-5">
      <div className="flex flex-col gap-1">
        <h2 className="app-h3">{t('review.title')}</h2>
        <p className="app-meta">{t('review.description', { name: entry.author.name })}</p>
      </div>
      <div className="flex flex-wrap gap-2">
        <AppButton onClick={() => void approve()} loading={approving}>
          <CheckIcon />
          {t('review.approve')}
        </AppButton>
        {entry.canEdit ? (
          <AppButton intent="neutral" asChild>
            <Link to={`/mural/${entry.id}/editar`}>
              <PencilIcon />
              {t('review.editFirst')}
            </Link>
          </AppButton>
        ) : null}
        <AppButton intent="ghost" onClick={() => setDialogOpen(true)}>
          <XIcon />
          {t('review.reject')}
        </AppButton>
      </div>

      <AppDialog open={dialogOpen} onOpenChange={setDialogOpen} title={t('review.rejectTitle')} description={t('review.rejectDescription')}>
        <AppForm form={form} onSubmit={reject}>
          <AppTextareaField name="reason" label={t('review.reason')} placeholder={t('review.reasonPlaceholder')} rows={4} maxLength={500} />
          <div className="flex justify-end gap-2">
            <AppButton intent="ghost" type="button" onClick={() => setDialogOpen(false)}>
              {t('common:actions.cancel')}
            </AppButton>
            <AppButton intent="danger" type="submit" loading={rejecting}>
              {t('review.rejectConfirm')}
            </AppButton>
          </div>
        </AppForm>
      </AppDialog>
    </section>
  )
}
