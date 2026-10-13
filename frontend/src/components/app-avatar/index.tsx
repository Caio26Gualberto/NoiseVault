import { BadgeCheckIcon } from 'lucide-react'

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { cn } from '@/lib/utils'

import { initialsOf } from './initials'

type AppAvatarProps = {
  name: string
  src?: string | null
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl'
  verified?: boolean
  className?: string
}

const sizeClass = {
  sm: 'size-7 text-xs',
  md: 'size-9 text-sm',
  lg: 'size-12 text-base',
  xl: 'size-16 text-lg',
  '2xl': 'size-24 text-2xl md:size-32 md:text-3xl',
} as const

const badgeClass = {
  sm: 'size-3 -right-0.5 -bottom-0.5',
  md: 'size-3.5 -right-0.5 -bottom-0.5',
  lg: 'size-4 right-0 bottom-0',
  xl: 'size-5 right-0 bottom-0',
  '2xl': 'size-7 right-1 bottom-1',
} as const

export function AppAvatar({ name, src, size = 'md', verified = false, className }: AppAvatarProps) {
  return (
    <span className={cn('relative inline-flex shrink-0', className)}>
      <Avatar className={cn('after:border-border/60', sizeClass[size])}>
        {src ? <AvatarImage src={src} alt={name} /> : null}
        <AvatarFallback className="bg-secondary font-heading font-semibold tracking-wide text-secondary-foreground">
          {initialsOf(name)}
        </AvatarFallback>
      </Avatar>
      {verified ? (
        <span
          className={cn(
            'absolute flex items-center justify-center rounded-full bg-background text-highlight',
            badgeClass[size],
          )}
        >
          <BadgeCheckIcon className="size-full fill-highlight text-background" />
        </span>
      ) : null}
    </span>
  )
}
