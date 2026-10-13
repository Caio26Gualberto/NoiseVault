import { Link } from 'react-router-dom'

import { cn } from '@/lib/utils'

import { homePath } from '../auth/routes'

type WordmarkProps = {
  size?: 'sm' | 'md' | 'lg'
  to?: string
  className?: string
}

const sizeClass = { sm: 'text-lg', md: 'text-xl', lg: 'text-3xl' } as const

export function Wordmark({ size = 'md', to = homePath, className }: WordmarkProps) {
  return (
    <Link
      to={to}
      className={cn(
        'inline-flex items-center gap-2 font-heading font-bold tracking-[0.12em] uppercase text-foreground no-underline',
        sizeClass[size],
        className,
      )}
    >
      <span aria-hidden className="inline-block h-[1em] w-1.5 skew-x-[-12deg] rounded-sm bg-primary" />
      <span>
        Noise <span className="text-primary">Vault</span>
      </span>
    </Link>
  )
}
