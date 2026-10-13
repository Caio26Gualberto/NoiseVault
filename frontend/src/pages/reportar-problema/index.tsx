import { zodResolver } from '@hookform/resolvers/zod'
import { CircleCheckIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { z } from 'zod'

import { homePath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { AppCard } from '@/components/app-card'
import { AppField, AppForm, AppTextareaField } from '@/components/app-field'
import { AppPageHeader } from '@/components/app-page-header'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

import { IssueLabel, reportProblem } from './services/suporte-service'

const schema = z.object({
  label: z.number(),
  title: z.string().trim().min(5, 'validation.required').max(120),
  description: z.string().trim().min(20, 'validation.required'),
  pageUrl: z.string().trim(),
})

type Values = z.infer<typeof schema>

const labels = Object.values(IssueLabel) as IssueLabel[]
const labelKey: Record<IssueLabel, string> = {
  [IssueLabel.bug]: 'bug',
  [IssueLabel.suggestion]: 'suggestion',
  [IssueLabel.content]: 'content',
  [IssueLabel.account]: 'account',
}

export function ReportarProblemaPage() {
  const { t } = useTranslation('suporte')
  const [submitting, setSubmitting] = useState(false)
  const [protocol, setProtocol] = useState<string | null>(null)
  const form = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { label: IssueLabel.bug, title: '', description: '', pageUrl: '' },
  })

  const label = useWatch({ control: form.control, name: 'label' })

  const submit = async (values: Values) => {
    setSubmitting(true)
    try {
      const result = await reportProblem({
        label: values.label as IssueLabel,
        title: values.title,
        description: values.description,
        pageUrl: values.pageUrl || null,
      })
      setProtocol(result.protocol)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="app-page-enter mx-auto flex max-w-2xl flex-col">
      <AppPageHeader title={t('title')} description={t('description')} />

      {protocol ? (
        <AppCard>
          <div className="flex flex-col items-center gap-4 py-4 text-center">
            <div className="flex size-14 items-center justify-center rounded-full bg-success/15 text-success">
              <CircleCheckIcon className="size-7" />
            </div>
            <div className="flex flex-col gap-1">
              <h2 className="app-h3">{t('sent.title')}</h2>
              <p className="text-sm text-muted-foreground">{t('sent.description', { protocol })}</p>
            </div>
            <AppButton intent="neutral" asChild>
              <Link to={homePath}>{t('sent.back')}</Link>
            </AppButton>
          </div>
        </AppCard>
      ) : (
        <AppCard>
          <AppForm form={form} onSubmit={submit}>
            <div className="flex flex-col gap-1.5">
              <Label>{t('fields.label')}</Label>
              <Select value={String(label)} onValueChange={(value) => form.setValue('label', Number(value))}>
                <SelectTrigger className="h-11 md:h-10">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {labels.map((option) => (
                    <SelectItem key={option} value={String(option)}>
                      {t(`labels.${labelKey[option]}`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <AppField name="title" label={t('fields.title')} placeholder={t('fields.titlePlaceholder')} />
            <AppTextareaField name="description" label={t('fields.description')} rows={6} placeholder={t('fields.descriptionPlaceholder')} />
            <AppField name="pageUrl" label={t('fields.pageUrl')} type="url" placeholder="https://" />
            <AppButton type="submit" loading={submitting} className="self-start">
              {t('submit')}
            </AppButton>
          </AppForm>
        </AppCard>
      )}
    </div>
  )
}
