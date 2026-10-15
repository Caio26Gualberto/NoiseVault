import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type AppFormLayoutProps = {
  aside: ReactNode
  children: ReactNode
  className?: string
}

export function AppFormLayout({ aside, children, className }: AppFormLayoutProps) {
  return (
    <div className={cn('grid gap-6 lg:grid-cols-[340px_minmax(0,1fr)] lg:items-start', className)}>
      <aside className="flex min-w-0 flex-col gap-4 lg:sticky lg:top-24">{aside}</aside>
      <div className="grid min-w-0 gap-4 xl:grid-cols-2">{children}</div>
    </div>
  )
}

type AppFormPanelProps = {
  title: string
  description?: ReactNode
  wide?: boolean
  className?: string
  children: ReactNode
}

export function AppFormPanel({ title, description, wide = false, className, children }: AppFormPanelProps) {
  return (
    <section className={cn('flex min-w-0 flex-col gap-3 rounded-xl border bg-card p-4 md:p-5', wide && 'xl:col-span-2', className)}>
      <div className="flex flex-col gap-0.5">
        <h2 className="font-heading text-base font-bold tracking-wider uppercase">{title}</h2>
        {description ? <p className="app-meta">{description}</p> : null}
      </div>
      {children}
    </section>
  )
}

export function AppFormAsideBox({ title, description, children, className }: { title?: string; description?: ReactNode; children?: ReactNode; className?: string }) {
  return (
    <div className={cn('flex flex-col gap-2 rounded-xl border bg-card p-4', className)}>
      {title ? <span className="text-sm font-medium">{title}</span> : null}
      {description ? <span className="app-meta">{description}</span> : null}
      {children}
    </div>
  )
}
