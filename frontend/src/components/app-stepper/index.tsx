import { CheckIcon } from 'lucide-react'
import { Fragment } from 'react'
import { useTranslation } from 'react-i18next'

import { cn } from '@/lib/utils'

type AppStepperProps = {
  steps: string[]
  current: number
  className?: string
}

export function AppStepper({ steps, current, className }: AppStepperProps) {
  const { t } = useTranslation()
  const progress = ((current + 1) / steps.length) * 100

  return (
    <div className={className}>
      <div className="flex flex-col gap-2 md:hidden">
        <p className="text-sm">
          <span className="text-muted-foreground">{t('stepper.progress', { current: current + 1, total: steps.length })}</span>
          <span className="text-muted-foreground"> · </span>
          <span className="font-medium">{steps[current]}</span>
        </p>
        <div className="h-1.5 overflow-hidden rounded-full bg-muted" role="progressbar" aria-valuemin={1} aria-valuemax={steps.length} aria-valuenow={current + 1}>
          <div className="h-full rounded-full bg-primary transition-[width] duration-300" style={{ width: `${progress}%` }} />
        </div>
      </div>

      <ol className="hidden items-center gap-3 md:flex">
        {steps.map((label, index) => {
          const done = index < current
          const active = index === current
          return (
            <Fragment key={label}>
              {index > 0 ? <li aria-hidden className={cn('h-px min-w-4 flex-1', done || active ? 'bg-primary' : 'bg-border')} /> : null}
              <li className="flex shrink-0 items-center gap-2" aria-current={active ? 'step' : undefined}>
                <span
                  className={cn(
                    'flex size-7 items-center justify-center rounded-full border text-[13px] font-semibold tabular-nums transition-colors',
                    done && 'border-primary bg-primary text-primary-foreground',
                    active && 'border-primary bg-primary/15 text-primary',
                    !done && !active && 'text-muted-foreground',
                  )}
                >
                  {done ? <CheckIcon className="size-4" /> : index + 1}
                </span>
                <span className={cn('text-sm', active ? 'font-medium text-foreground' : 'text-muted-foreground')}>{label}</span>
              </li>
            </Fragment>
          )
        })}
      </ol>
    </div>
  )
}
