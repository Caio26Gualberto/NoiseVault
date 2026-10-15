import { CircleHelpIcon, EyeIcon, EyeOffIcon } from 'lucide-react'
import { useId, useRef, useState } from 'react'
import type { ComponentProps, ReactNode } from 'react'
import { Controller, useFormContext } from 'react-hook-form'
import { useTranslation } from 'react-i18next'

import { AppDateTimeInput } from '@/components/app-date-time-input'
import { AppCitySelect } from '@/components/app-city-select'
import type { AppCity, AppCityValue } from '@/components/app-city-select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

type FieldShellProps = {
  name: string
  label: string
  description?: ReactNode
  hint?: string
  children: (bindings: { id: string; invalid: boolean; describedBy: string | undefined }) => ReactNode
}

function useFieldError(name: string): string | null {
  const { t } = useTranslation()
  const { formState } = useFormContext()
  const raw = name.split('.').reduce<unknown>((acc, key) => (acc as Record<string, unknown> | undefined)?.[key], formState.errors)
  const message = (raw as { message?: string } | undefined)?.message
  return message ? t(message, { defaultValue: message }) : null
}

function FieldHint({ label, text }: { label: string; text: string }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const openOnPress = useRef(false)

  return (
    <Tooltip open={open} onOpenChange={setOpen}>
      <TooltipTrigger asChild>
        <button
          type="button"
          onPointerDown={(event) => {
            event.preventDefault()
            openOnPress.current = open
          }}
          onClick={(event) => {
            event.preventDefault()
            setOpen(!openOnPress.current)
          }}
          aria-label={t('field.hint', { label })}
          className="-m-1 rounded-full p-1 text-muted-foreground hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none"
        >
          <CircleHelpIcon className="size-4" />
        </button>
      </TooltipTrigger>
      <TooltipContent side="top" sideOffset={6} className="text-sm leading-snug">
        {text}
      </TooltipContent>
    </Tooltip>
  )
}

function FieldShell({ name, label, description, hint, children }: FieldShellProps) {
  const id = useId()
  const error = useFieldError(name)
  const descriptionId = description ? `${id}-description` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [descriptionId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className="flex flex-col gap-1.5">
      <span className="flex items-center gap-1.5">
        <Label htmlFor={id}>{label}</Label>
        {hint ? <FieldHint label={label} text={hint} /> : null}
      </span>
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
  hint?: string
}

export function AppField({ name, label, description, hint, className, ...props }: AppFieldProps) {
  const { register } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
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

export function AppPasswordField({ name, label, description, hint, className, autoComplete, ...props }: AppFieldProps) {
  const { register } = useFormContext()
  const { t } = useTranslation()
  const [visible, setVisible] = useState(false)

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
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
  hint?: string
}

export function AppTextareaField({ name, label, description, hint, className, ...props }: AppTextareaFieldProps) {
  const { register } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
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

export type AppSelectOption = { value: string; label: string }

export type AppSelectFieldProps = {
  name: string
  label: string
  options: AppSelectOption[]
  placeholder?: string
  emptyLabel?: string
  description?: ReactNode
  hint?: string
}

const emptySelectValue = '__empty__'

export function AppSelectField({ name, label, options, placeholder, emptyLabel, description, hint }: AppSelectFieldProps) {
  const { control } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
      {({ id, invalid, describedBy }) => (
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <Select
              value={field.value === '' ? (emptyLabel ? emptySelectValue : '') : String(field.value)}
              onValueChange={(value) => field.onChange(value === emptySelectValue ? '' : value)}
            >
              <SelectTrigger
                id={id}
                aria-invalid={invalid || undefined}
                aria-describedby={describedBy}
                onBlur={field.onBlur}
                className={fieldInputClass}
              >
                <SelectValue placeholder={placeholder} />
              </SelectTrigger>
              <SelectContent>
                {emptyLabel ? <SelectItem value={emptySelectValue}>{emptyLabel}</SelectItem> : null}
                {options.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
      )}
    </FieldShell>
  )
}

export type AppChipOption<T extends string | number> = { value: T; label: string }

export type AppChipsFieldProps<T extends string | number> = {
  name: string
  label: string
  options: AppChipOption<T>[]
  max?: number
  description?: ReactNode
  hint?: string
}

export function AppChipsField<T extends string | number>({ name, label, options, max, description, hint }: AppChipsFieldProps<T>) {
  const { control } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
      {({ id, describedBy }) => (
        <Controller
          control={control}
          name={name}
          render={({ field }) => {
            const selected = (field.value ?? []) as T[]
            const full = max !== undefined && selected.length >= max
            const toggle = (value: T) =>
              field.onChange(selected.includes(value) ? selected.filter((item) => item !== value) : [...selected, value])

            return (
              <div id={id} role="group" aria-describedby={describedBy} className="flex flex-wrap gap-1.5">
                {options.map((option) => {
                  const active = selected.includes(option.value)
                  return (
                    <button
                      key={option.value}
                      type="button"
                      aria-pressed={active}
                      disabled={!active && full}
                      onClick={() => toggle(option.value)}
                      className={cn(
                        'h-8 rounded-full border px-3 text-[13px] transition-colors focus-visible:ring-2 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-40',
                        active
                          ? 'border-primary bg-primary/15 text-primary'
                          : 'bg-transparent text-muted-foreground hover:bg-accent hover:text-foreground',
                      )}
                    >
                      {option.label}
                    </button>
                  )
                })}
              </div>
            )
          }}
        />
      )}
    </FieldShell>
  )
}

export type AppCityFieldProps = {
  name: string
  label: string
  placeholder: string
  search: (term: string) => Promise<AppCity[]>
  suggest?: () => Promise<AppCity[]>
  clearLabel?: string
  allowAbroad?: boolean
  description?: ReactNode
  hint?: string
}

export function AppCityField({ name, label, placeholder, search, suggest, clearLabel, allowAbroad, description, hint }: AppCityFieldProps) {
  const { control } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
      {({ id, invalid, describedBy }) => (
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <AppCitySelect
              id={id}
              invalid={invalid}
              describedBy={describedBy}
              value={field.value as AppCityValue}
              onChange={field.onChange}
              search={search}
              suggest={suggest}
              placeholder={placeholder}
              clearLabel={clearLabel}
              allowAbroad={allowAbroad}
            />
          )}
        />
      )}
    </FieldShell>
  )
}

export type AppDateTimeFieldProps = {
  name: string
  label: string
  description?: ReactNode
  hint?: string
}

export function AppDateTimeField({ name, label, description, hint }: AppDateTimeFieldProps) {
  const { control } = useFormContext()

  return (
    <FieldShell name={name} label={label} description={description} hint={hint}>
      {({ id, invalid, describedBy }) => (
        <Controller
          control={control}
          name={name}
          render={({ field }) => (
            <AppDateTimeInput id={id} invalid={invalid} describedBy={describedBy} value={field.value as string} onChange={field.onChange} onBlur={field.onBlur} />
          )}
        />
      )}
    </FieldShell>
  )
}
