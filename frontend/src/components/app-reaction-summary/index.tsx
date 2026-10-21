import type { ReactNode } from 'react'

import { AppAvatar } from '@/components/app-avatar'
import type { AppPerson } from '@/components/app-people-dialog'
import { cn } from '@/lib/utils'

type AppReactionSummaryProps = {
  people: AppPerson[]
  onOpen?: () => void
  children: ReactNode
  className?: string
}

export function AppReactionSummary({ people, onOpen, children, className }: AppReactionSummaryProps) {
  const body = (
    <>
      {people.length > 0 ? (
        <span className="flex shrink-0 -space-x-2">
          {people.map((person) => (
            <AppAvatar key={person.id} name={person.name} src={person.avatarUrl} size="sm" className="ring-2 ring-card" />
          ))}
        </span>
      ) : null}
      <span className="min-w-0 text-left [&_b]:font-semibold [&_b]:text-foreground">{children}</span>
    </>
  )

  const base = 'flex items-center gap-2 text-sm text-muted-foreground'

  if (!onOpen) return <span className={cn(base, className)}>{body}</span>

  return (
    <button type="button" onClick={onOpen} className={cn(base, 'self-start rounded-full py-1 pr-2 transition-colors hover:text-foreground', className)}>
      {body}
    </button>
  )
}
