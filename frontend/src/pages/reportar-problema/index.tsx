import { zodResolver } from '@hookform/resolvers/zod'
import { CircleCheckIcon, InboxIcon, InfoIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { homePath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppChipsField, AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { useIsMobile } from '@/hooks/use-media-query'
import { lastVisitedPath } from '@/layout/last-location'

import { ReportAttachments } from './components/report-attachments'
import type { ReportAttachment } from './components/report-attachments'
import { createTicket } from './services/suporte-service'
import { myTicketPath, myTicketsPath } from './support-paths'
import type { SupportTicketDto } from './services/suporte-service'
import { SupportTag, allSupportAreas, allSupportTags, areaFromPath, useSupportLabels } from './support-enums'
import type { SupportArea } from './support-enums'

const schema = z.object({
  title: z.string().trim().min(5, 'validation.supportTitle').max(120, 'validation.supportTitle'),
  description: z.string().trim().min(20, 'validation.supportDescription').max(5000, 'validation.supportDescription'),
  tags: z.array(z.number()).min(1, 'validation.supportTags'),
  areas: z.array(z.number()),
})

type Values = z.infer<typeof schema>


export function ReportarProblemaPage() {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const isMobile = useIsMobile()
  const [origin] = useState(() => lastVisitedPath())
  const originArea = areaFromPath(origin)
  const [attachments, setAttachments] = useState<ReportAttachment[]>([])
  const [submitting, setSubmitting] = useState(false)
  const [created, setCreated] = useState<SupportTicketDto | null>(null)

  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      tags: [SupportTag.bug, isMobile ? SupportTag.mobile : SupportTag.desktop],
      areas: originArea === null ? [] : [originArea],
    },
  })

  const uploading = attachments.some((item) => item.state === 'uploading')
  const failed = attachments.some((item) => item.state === 'error')

  const submit = async (values: Values) => {
    setSubmitting(true)
    try {
      const ticket = await createTicket({
        title: values.title,
        description: values.description,
        tags: values.tags as SupportTag[],
        areas: values.areas as SupportArea[],
        mediaIds: attachments.filter((item) => item.state === 'ready' && item.mediaId !== null).map((item) => item.mediaId!),
        originPath: origin,
        userAgent: navigator.userAgent,
        viewportWidth: window.innerWidth,
        viewportHeight: window.innerHeight,
      })
      setAttachments([])
      setCreated(ticket)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <AppButton intent="neutral" asChild>
            <Link to={myTicketsPath}>
              <InboxIcon />
              {t('myTickets')}
            </Link>
          </AppButton>
        }
      />

      {created ? (
        <AppCard>
          <div className="flex flex-col items-center gap-4 py-4 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
              <CircleCheckIcon className="size-7" />
            </div>
            <div className="flex flex-col gap-1">
              <h2 className="app-h3">{t('sent.title')}</h2>
              <p className="text-sm text-muted-foreground">{t('sent.description', { protocol: created.protocol })}</p>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row">
              <AppButton asChild>
                <Link to={myTicketPath(created.id)}>{t('sent.follow')}</Link>
              </AppButton>
              <AppButton intent="neutral" asChild>
                <Link to={homePath}>{t('sent.back')}</Link>
              </AppButton>
            </div>
          </div>
        </AppCard>
      ) : (
        <AppCard>
          <AppForm form={form} onSubmit={submit}>
            <AppChipsField
              name="tags"
              label={t('fields.tags')}
              description={t('fields.tagsHint')}
              options={allSupportTags.map((tag) => ({ value: tag, label: labels.tag(tag) }))}
            />
            <AppField name="title" label={t('fields.title')} placeholder={t('fields.titlePlaceholder')} maxLength={120} />
            <AppTextareaField
              name="description"
              label={t('fields.description')}
              rows={6}
              maxLength={5000}
              placeholder={t('fields.descriptionPlaceholder')}
            />
            <AppChipsField
              name="areas"
              label={t('fields.areas')}
              description={t('fields.areasHint')}
              options={allSupportAreas.map((area) => ({ value: area, label: labels.area(area) }))}
            />
            <ReportAttachments value={attachments} onChange={setAttachments} />

            <div className="flex gap-2 rounded-lg border border-dashed p-3 text-sm text-muted-foreground">
              <InfoIcon className="mt-0.5 size-4 shrink-0" />
              <p>{origin ? t('context.withOrigin', { path: origin }) : t('context.withoutOrigin')}</p>
            </div>

            {failed ? <p className="text-sm text-destructive">{t('attachments.failedHint')}</p> : null}

            <AppButton type="submit" loading={submitting} disabled={uploading} className="self-start">
              {uploading ? t('uploading') : t('submit')}
            </AppButton>
          </AppForm>
        </AppCard>
      )}
    </div>
  )
}
