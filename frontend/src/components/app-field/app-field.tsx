import { EyeIcon, EyeOffIcon } from 'lucide-react'
import { useId, useState } from 'react'
import type { ComponentProps, ReactNode } from 'react'
import { useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'

type FieldShellProps = {
  name: string
  label: string
  description?: ReactNode
  children: (bindings: { id: string; invalid: boolean; describedBy: string | undefined }) => ReactNode
}

function useFieldError(name: string): string | null {
  const { t } = useTranslation()
  const { formState } = useFormContext()
  const raw = name.split('.').reduce<unknown>((acc, key) => (acc as Record<string, unknown> | undefined)?.[key], formState.errors)
  const message = (raw as { message?: string } | undefined)?.message
  return message ? t(message, { defaultValue: message }) : null
}

function FieldShell({ name, label, description, children }: FieldShellProps) {
  const id = useId()
  const error = useFieldError(name)
  const descriptionId = description ? `${id}-description` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [descriptionId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      {children({ id, invalid: Boolean(error), describedBy })}
      {description ? (
        <p id={descriptionId} className="text-[13px] text-muted-foreground">
          {description}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-[13px] text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  )
}

const fieldInputClass = 'h-11 text-[15px] md:h-10'

export type AppFieldProps = Omit<ComponentProps<typeof Input>, 'name' | 'id'> & {
  name: string
  label: string
  description?: ReactNode
}

export function AppField({ name, label, description, className, ...props }: AppFieldProps) {
  const { register } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description}>
      {({ id, invalid, describedBy }) => (
        <Input
          id={id}
          aria-invalid={invalid || undefined}
          aria-describedby={describedBy}
          className={cn(fieldInputClass, className)}
          {...register(name)}
          {...props}
        />
      )}
    </FieldShell>
  )
}

export function AppPasswordField({ name, label, description, className, autoComplete, ...props }: AppFieldProps) {
  const { register } = useFormContext()
  const { t } = useTranslation()
  const [visible, setVisible] = useState(false)

  return (
    <FieldShell name={name} label={label} description={description}>
      {({ id, invalid, describedBy }) => (
        <div className="relative">
          <Input
            id={id}
            type={visible ? 'text' : 'password'}
            autoComplete={autoComplete ?? 'current-password'}
            aria-invalid={invalid || undefined}
            aria-describedby={describedBy}
            className={cn(fieldInputClass, 'pr-11', className)}
            {...register(name)}
            {...props}
          />
          <button
            type="button"
            onClick={() => setVisible((current) => !current)}
            aria-label={visible ? t('password.hide') : t('password.show')}
            className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:text-foreground"
          >
            {visible ? <EyeOffIcon className="size-4" /> : <EyeIcon className="size-4" />}
          </button>
        </div>
      )}
    </FieldShell>
  )
}

export type AppTextareaFieldProps = Omit<ComponentProps<typeof Textarea>, 'name' | 'id'> & {
  name: string
  label: string
  description?: ReactNode
}

export function AppTextareaField({ name, label, description, className, ...props }: AppTextareaFieldProps) {
  const { register } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description}>
      {({ id, invalid, describedBy }) => (
        <Textarea
          id={id}
          aria-invalid={invalid || undefined}
          aria-describedby={describedBy}
          className={cn('text-[15px]', className)}
          {...register(name)}
          {...props}
        />
      )}
    </FieldShell>
  )
}
