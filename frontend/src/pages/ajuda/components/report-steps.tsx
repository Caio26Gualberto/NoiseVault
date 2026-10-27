import { zodResolver } from '@hookform/resolvers/zod'
import { InfoIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'

import { AppButton } from '@/components/app-button'
import { AppChipsField, AppField, AppTextareaField } from '@/components/app-field'
import { useIsMobile } from '@/hooks/use-media-query'

import type { ReportPayload } from '../hooks/use-report-submit'
import {
  bugAnswerMaxLength,
  composeBugDescription,
  composeSuggestionDescription,
  ideaMaxLength,
  reasonMaxLength,
  reportAnswerMinLength,
} from '../report-description'
import { SupportTag, allSupportAreas, areaFromPath, useSupportLabels } from '../support-enums'
import type { SupportArea } from '../support-enums'
import { ReportAttachments } from './report-attachments'
import type { ReportAttachment } from './report-attachments'
import { ReportWizard } from './report-wizard'
import type { ReportWizardStep } from './report-wizard'

const titleField = z.string().trim().min(5, 'validation.supportTitle').max(120, 'validation.supportTitle')
const answer = (max: number) => z.string().trim().min(reportAnswerMinLength, 'validation.supportAnswer').max(max, 'validation.supportAnswer')
const optionalAnswer = (max: number) => z.string().trim().max(max, 'validation.supportAnswer')

const bugSchema = z.object({
  areas: z.array(z.number()),
  devices: z.array(z.number()),
  doing: answer(bugAnswerMaxLength),
  expected: optionalAnswer(bugAnswerMaxLength),
  happened: answer(bugAnswerMaxLength),
  title: titleField,
})

const suggestionSchema = z.object({
  title: titleField,
  idea: answer(ideaMaxLength),
  reason: optionalAnswer(reasonMaxLength),
  areas: z.array(z.number()),
})

type BugValues = z.infer<typeof bugSchema>
type SuggestionValues = z.infer<typeof suggestionSchema>

const deviceTags = [SupportTag.mobile, SupportTag.desktop]

type ReportFlowProps = {
  origin: string | null
  submitting: boolean
  onSubmit: (payload: ReportPayload) => Promise<void>
  onExit: (dirty: boolean) => void
}

type ReviewRow = { label: string; value: string; step: number }

function ReportReview({ rows, onEdit }: { rows: ReviewRow[]; onEdit: (step: number) => void }) {
  const { t } = useTranslation('suporte')

  return (
    <div className="flex flex-col gap-2">
      <span className="text-sm font-medium">{t('review.title')}</span>
      <dl className="flex flex-col divide-y rounded-lg border">
        {rows.map((row) => (
          <div key={row.label} className="flex items-start gap-3 p-3">
            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
              <dt className="app-meta">{row.label}</dt>
              <dd className={row.value ? 'line-clamp-3 text-sm break-words whitespace-pre-line' : 'text-sm text-muted-foreground'}>
                {row.value || t('review.empty')}
              </dd>
            </div>
            <AppButton type="button" intent="ghost" size="sm" onClick={() => onEdit(row.step)}>
              {t('review.edit')}
            </AppButton>
          </div>
        ))}
      </dl>
    </div>
  )
}

function ContextNotice({ origin }: { origin: string | null }) {
  const { t } = useTranslation('suporte')

  return (
    <div className="flex gap-2 rounded-xl border border-dashed p-4 text-sm text-muted-foreground">
      <InfoIcon className="mt-0.5 size-4 shrink-0" />
      <p>{origin ? t('context.withOrigin', { path: origin }) : t('context.withoutOrigin')}</p>
    </div>
  )
}

function AttachmentsStep({ value, onChange }: { value: ReportAttachment[]; onChange: (update: (previous: ReportAttachment[]) => ReportAttachment[]) => void }) {
  const { t } = useTranslation('suporte')
  const failed = value.some((item) => item.state === 'error')

  return (
    <>
      <ReportAttachments value={value} onChange={onChange} />
      {failed ? <p className="text-sm text-destructive">{t('attachments.failedHint')}</p> : null}
    </>
  )
}

function useAttachmentsSummary(attachments: ReportAttachment[]) {
  const { t } = useTranslation('suporte')
  return attachments.length > 0 ? t('review.attachments', { count: attachments.length }) : ''
}

export function BugReport({ origin, submitting, onSubmit, onExit }: ReportFlowProps) {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const isMobile = useIsMobile()
  const [step, setStep] = useState(0)
  const [attachments, setAttachments] = useState<ReportAttachment[]>([])
  const originArea = areaFromPath(origin)

  const form = useForm<BugValues>({
    resolver: zodResolver(bugSchema),
    defaultValues: {
      areas: originArea === null ? [] : [originArea],
      devices: [isMobile ? SupportTag.mobile : SupportTag.desktop],
      doing: '',
      expected: '',
      happened: '',
      title: '',
    },
  })

  const values = useWatch({ control: form.control }) as BugValues
  const { isDirty } = form.formState
  const attachmentsSummary = useAttachmentsSummary(attachments)

  const submit = (data: BugValues) =>
    onSubmit({
      title: data.title,
      description: composeBugDescription(t, data),
      tags: [SupportTag.bug, ...(data.devices as SupportTag[])],
      areas: data.areas as SupportArea[],
      attachments,
    })

  const steps: ReportWizardStep<BugValues>[] = [
    {
      label: t('steps.bug.where.label'),
      title: t('steps.bug.where.title'),
      description: t('steps.bug.where.description'),
      fields: ['areas', 'devices'],
      content: (
        <>
          <AppChipsField
            name="areas"
            label={t('fields.areas')}
            description={t('fields.areasHint')}
            options={allSupportAreas.map((area) => ({ value: area, label: labels.area(area) }))}
          />
          <AppChipsField name="devices" label={t('fields.devices')} options={deviceTags.map((tag) => ({ value: tag, label: labels.tag(tag) }))} />
        </>
      ),
    },
    {
      label: t('steps.bug.doing.label'),
      title: t('steps.bug.doing.title'),
      description: t('steps.bug.doing.description'),
      fields: ['doing'],
      content: <AppTextareaField name="doing" label={t('fields.doing')} rows={5} maxLength={bugAnswerMaxLength} placeholder={t('fields.doingPlaceholder')} />,
    },
    {
      label: t('steps.bug.result.label'),
      title: t('steps.bug.result.title'),
      description: t('steps.bug.result.description'),
      fields: ['expected', 'happened'],
      content: (
        <>
          <AppTextareaField name="expected" label={t('fields.expected')} rows={3} maxLength={bugAnswerMaxLength} placeholder={t('fields.expectedPlaceholder')} />
          <AppTextareaField name="happened" label={t('fields.happened')} rows={4} maxLength={bugAnswerMaxLength} placeholder={t('fields.happenedPlaceholder')} />
        </>
      ),
    },
    {
      label: t('steps.bug.attachments.label'),
      title: t('steps.bug.attachments.title'),
      description: t('steps.bug.attachments.description'),
      fields: [],
      content: <AttachmentsStep value={attachments} onChange={setAttachments} />,
    },
    {
      label: t('steps.bug.review.label'),
      title: t('steps.bug.review.title'),
      description: t('steps.bug.review.description'),
      fields: ['title'],
      content: (
        <>
          <AppField name="title" label={t('fields.title')} placeholder={t('fields.titlePlaceholder')} maxLength={120} />
          <ReportReview
            onEdit={setStep}
            rows={[
              { label: t('review.where'), value: [...values.areas.map((area) => labels.area(area as SupportArea)), ...values.devices.map((tag) => labels.tag(tag as SupportTag))].join(', '), step: 0 },
              { label: t('fields.doing'), value: values.doing.trim(), step: 1 },
              { label: t('fields.expected'), value: values.expected.trim(), step: 2 },
              { label: t('fields.happened'), value: values.happened.trim(), step: 2 },
              { label: t('review.attachmentsLabel'), value: attachmentsSummary, step: 3 },
            ]}
          />
          <ContextNotice origin={origin} />
        </>
      ),
    },
  ]

  return (
    <ReportWizard
      form={form}
      steps={steps}
      step={step}
      onStepChange={setStep}
      onExit={() => onExit(isDirty || attachments.length > 0)}
      onSubmit={submit}
      submitting={submitting}
      uploading={attachments.some((item) => item.state === 'uploading')}
    />
  )
}

export function SuggestionReport({ origin, submitting, onSubmit, onExit }: ReportFlowProps) {
  const { t } = useTranslation('suporte')
  const labels = useSupportLabels()
  const [step, setStep] = useState(0)
  const [attachments, setAttachments] = useState<ReportAttachment[]>([])

  const form = useForm<SuggestionValues>({
    resolver: zodResolver(suggestionSchema),
    defaultValues: { title: '', idea: '', reason: '', areas: [] },
  })

  const values = useWatch({ control: form.control }) as SuggestionValues
  const { isDirty } = form.formState
  const attachmentsSummary = useAttachmentsSummary(attachments)

  const submit = (data: SuggestionValues) =>
    onSubmit({
      title: data.title,
      description: composeSuggestionDescription(t, data),
      tags: [SupportTag.suggestion],
      areas: data.areas as SupportArea[],
      attachments,
    })

  const steps: ReportWizardStep<SuggestionValues>[] = [
    {
      label: t('steps.suggestion.idea.label'),
      title: t('steps.suggestion.idea.title'),
      description: t('steps.suggestion.idea.description'),
      fields: ['title', 'idea'],
      content: (
        <>
          <AppField name="title" label={t('fields.title')} placeholder={t('fields.suggestionTitlePlaceholder')} maxLength={120} />
          <AppTextareaField name="idea" label={t('fields.idea')} rows={6} maxLength={ideaMaxLength} placeholder={t('fields.ideaPlaceholder')} />
        </>
      ),
    },
    {
      label: t('steps.suggestion.reason.label'),
      title: t('steps.suggestion.reason.title'),
      description: t('steps.suggestion.reason.description'),
      fields: ['reason', 'areas'],
      content: (
        <>
          <AppTextareaField name="reason" label={t('fields.reason')} rows={4} maxLength={reasonMaxLength} placeholder={t('fields.reasonPlaceholder')} />
          <AppChipsField
            name="areas"
            label={t('fields.suggestionAreas')}
            options={allSupportAreas.map((area) => ({ value: area, label: labels.area(area) }))}
          />
        </>
      ),
    },
    {
      label: t('steps.suggestion.attachments.label'),
      title: t('steps.suggestion.attachments.title'),
      description: t('steps.suggestion.attachments.description'),
      fields: [],
      content: <AttachmentsStep value={attachments} onChange={setAttachments} />,
    },
    {
      label: t('steps.suggestion.review.label'),
      title: t('steps.suggestion.review.title'),
      description: t('steps.suggestion.review.description'),
      fields: [],
      content: (
        <>
          <ReportReview
            onEdit={setStep}
            rows={[
              { label: t('fields.title'), value: values.title.trim(), step: 0 },
              { label: t('fields.idea'), value: values.idea.trim(), step: 0 },
              { label: t('fields.reason'), value: values.reason.trim(), step: 1 },
              { label: t('fields.suggestionAreas'), value: values.areas.map((area) => labels.area(area as SupportArea)).join(', '), step: 1 },
              { label: t('review.attachmentsLabel'), value: attachmentsSummary, step: 2 },
            ]}
          />
          <ContextNotice origin={origin} />
        </>
      ),
    },
  ]

  return (
    <ReportWizard
      form={form}
      steps={steps}
      step={step}
      onStepChange={setStep}
      onExit={() => onExit(isDirty || attachments.length > 0)}
      onSubmit={submit}
      submitting={submitting}
      uploading={attachments.some((item) => item.state === 'uploading')}
    />
  )
}
