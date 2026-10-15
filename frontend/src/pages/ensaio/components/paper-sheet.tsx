import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

export function PaperSheet({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <section className={cn('relative bg-paper px-5 py-8 text-paper-foreground shadow-lg md:px-10 md:py-10', className)}>
      <span aria-hidden="true" className="absolute -top-3 left-1/2 h-6 w-28 -translate-x-1/2 -rotate-2 bg-tape" />
      {children}
    </section>
  )
}

export function PaperSection({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-3 border-t-2 border-dashed border-paper-rule pt-5">
      <h2 className="font-heading text-lg font-bold tracking-wider uppercase">{title}</h2>
      {children}
    </div>
  )
}
