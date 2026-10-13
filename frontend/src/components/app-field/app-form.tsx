import type { FormEvent, ReactNode } from 'react'
import { FormProvider } from 'react-hook-form'
import type { FieldValues, UseFormReturn } from 'react-hook-form'

import { cn } from '@/lib/utils'

type AppFormProps<T extends FieldValues> = {
  form: UseFormReturn<T>
  onSubmit: (values: T) => void | Promise<void>
  children: ReactNode
  className?: string
}

export function AppForm<T extends FieldValues>({ form, onSubmit, children, className }: AppFormProps<T>) {
  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    void form.handleSubmit(onSubmit)(event)
  }

  return (
    <FormProvider {...form}>
      <form onSubmit={handleSubmit} noValidate className={cn('flex flex-col gap-4', className)}>
        {children}
      </form>
    </FormProvider>
  )
}
