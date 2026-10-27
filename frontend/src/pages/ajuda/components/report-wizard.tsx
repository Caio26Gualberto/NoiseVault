import { ArrowLeftIcon, ArrowRightIcon } from 'lucide-react'
import { useEffect, useRef } from 'react'
import type { KeyboardEvent, ReactNode } from 'react'
import type { FieldValues, Path, UseFormReturn } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppForm } from '@/components/app-field'
import { AppStepper } from '@/components/app-stepper'
import { cn } from '@/lib/utils'

export type ReportWizardStep<T extends FieldValues> = {
  label: string
  title: string
  description?: ReactNode
  fields: Path<T>[]
  content: ReactNode
}

type ReportWizardProps<T extends FieldValues> = {
  form: UseFormReturn<T>
  steps: ReportWizardStep<T>[]
  step: number
  onStepChange: (step: number) => void
  onExit: () => void
  onSubmit: (values: T) => Promise<void>
  submitting: boolean
  uploading: boolean
}

export function ReportWizard<T extends FieldValues>({ form, steps, step, onStepChange, onExit, onSubmit, submitting, uploading }: ReportWizardProps<T>) {
  const { t } = useTranslation('suporte')
  const heading = useRef<HTMLHeadingElement>(null)
  const mounted = useRef(false)
  const last = step === steps.length - 1

  useEffect(() => {
    if (mounted.current) heading.current?.focus()
    mounted.current = true
  }, [step])

  const next = async () => {
    if (await form.trigger(steps[step].fields, { shouldFocus: true })) onStepChange(step + 1)
  }

  const back = () => (step === 0 ? onExit() : onStepChange(step - 1))

  const skipEarlySubmit = (event: KeyboardEvent<HTMLDivElement>) => {
    if (last || event.key !== 'Enter' || !(event.target instanceof HTMLInputElement)) return
    event.preventDefault()
    void next()
  }

  return (
    <AppForm form={form} onSubmit={onSubmit} className="mx-auto w-full max-w-2xl">
      <div className="flex flex-col gap-6" onKeyDown={skipEarlySubmit}>
        <AppStepper steps={steps.map((item) => item.label)} current={step} />

        <section className="flex flex-col gap-5 rounded-xl border bg-card p-4 md:p-6">
          <div className="flex flex-col gap-1">
            <h2 ref={heading} tabIndex={-1} className="app-h2 outline-none">
              {steps[step].title}
            </h2>
            {steps[step].description ? <p className="text-sm text-muted-foreground">{steps[step].description}</p> : null}
          </div>
          {steps.map((item, index) => (
            <div key={item.label} className={cn('flex flex-col gap-4', index !== step && 'hidden')}>
              {item.content}
            </div>
          ))}
        </section>

        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
          <AppButton type="button" intent="neutral" onClick={back} disabled={submitting}>
            <ArrowLeftIcon />
            {t('wizard.back')}
          </AppButton>
          {last ? (
            <AppButton type="submit" loading={submitting} disabled={uploading} className="font-heading tracking-wider uppercase">
              {uploading ? t('uploading') : t('submit')}
            </AppButton>
          ) : (
            <AppButton type="button" onClick={() => void next()}>
              {t('wizard.next')}
              <ArrowRightIcon />
            </AppButton>
          )}
        </div>
      </div>
    </AppForm>
  )
}
