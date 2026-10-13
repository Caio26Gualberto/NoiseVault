import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type AppPageHeaderProps = {
  title: ReactNode
  description?: ReactNode
  actions?: ReactNode
  meta?: ReactNode
  className?: string
}

export function AppPageHeader({ title, description, actions, meta, className }: AppPageHeaderProps) {
  return (
    <header className={cn('mb-6 flex flex-col gap-3 md:flex-row md:items-end md:justify-between', className)}>
      <div className="flex min-w-0 flex-col gap-1">
        {meta ? <div className="app-meta">{meta}</div> : null}
        <h1 className="app-h1 text-balance">{title}</h1>
        {description ? <p className="text-muted-foreground">{description}</p> : null}
      </div>
      {actions ? <div className="flex shrink-0 flex-wrap gap-2">{actions}</div> : null}
    </header>
  )
}
