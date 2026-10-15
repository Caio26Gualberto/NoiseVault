import { ScissorsIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

import { cn } from '@/lib/utils'

const tapeAngles = [-3, 2, -1, 3, -2, 1]

export type FlyerAction = {
  label: string
  onClick?: () => void
  to?: string
  disabled?: boolean
}

type FlyerCardProps = {
  index: number
  to?: string
  title: string
  name: string
  meta: string | null
  photoUrl: string | null
  badge?: ReactNode
  action?: FlyerAction | null
}

export function FlyerCard({ index, to, title, name, meta, photoUrl, badge, action }: FlyerCardProps) {
  const angle = tapeAngles[index % tapeAngles.length]

  return (
    <article className="relative flex min-w-0 bg-paper text-paper-foreground shadow-lg sm:flex-col">
      <span
        aria-hidden="true"
        className="absolute -top-2.5 left-6 h-5 w-20 bg-tape sm:left-1/2 sm:h-6 sm:w-24 sm:-translate-x-1/2"
        style={{ rotate: `${angle}deg` }}
      />
      <FlyerBody to={to}>
        <span className="ensaio-halftone relative w-28 shrink-0 overflow-hidden sm:h-48 sm:w-auto">
          {photoUrl ? <img src={photoUrl} alt="" className="size-full object-cover mix-blend-multiply grayscale contrast-125" /> : null}
        </span>
        <span className={cn('flex min-w-0 flex-1 flex-col gap-1 px-4 pt-4 sm:px-6 sm:pt-5 sm:pb-5', action ? 'pb-14' : 'pb-4')}>
          {badge}
          <span className="font-heading text-3xl leading-none font-bold uppercase break-words sm:text-[40px]">{title}</span>
          <span className="truncate text-[17px] font-semibold sm:text-lg">{name}</span>
          {meta ? <span className="truncate text-sm text-paper-muted sm:text-[15px]">{meta}</span> : null}
        </span>
      </FlyerBody>
      {action ? <FlyerTearStrip action={action} /> : null}
    </article>
  )
}

function FlyerBody({ to, children }: { to?: string; children: ReactNode }) {
  const className = 'flex min-w-0 flex-1 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary sm:flex-col'

  return to ? (
    <Link to={to} className={className}>
      {children}
    </Link>
  ) : (
    <div className={className}>{children}</div>
  )
}

function FlyerTearStrip({ action }: { action: FlyerAction }) {
  const className = cn(
    'flex h-12 w-full shrink-0 items-center justify-center gap-2 border-t-2 border-dashed border-paper-rule font-typewriter text-[15px] text-paper-foreground transition-colors sm:h-[52px] sm:text-base',
    action.disabled ? 'cursor-default opacity-60' : 'hover:bg-paper-rule/15 focus-visible:bg-paper-rule/15 focus-visible:outline-none',
  )

  const content = (
    <>
      <ScissorsIcon className="size-4" aria-hidden="true" />
      {action.label}
    </>
  )

  return (
    <div className="absolute inset-x-0 bottom-0 ml-28 sm:static sm:ml-0">
      {action.to ? (
        <Link to={action.to} className={className}>
          {content}
        </Link>
      ) : (
        <button type="button" disabled={action.disabled} onClick={action.onClick} className={className}>
          {content}
        </button>
      )}
    </div>
  )
}
