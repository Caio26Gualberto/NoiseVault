import type { ComponentProps } from 'react'

import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

export type AppBadgeTone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger' | 'info' | 'highlight'

const toneClass: Record<AppBadgeTone, string> = {
  neutral: 'border-border bg-muted text-muted-foreground',
  brand: 'bg-primary/15 text-primary',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/20 text-warning-foreground text-foreground',
  danger: 'bg-destructive/15 text-destructive',
  info: 'bg-info/15 text-info',
  highlight: 'bg-highlight/20 text-foreground',
}

type AppBadgeProps = Omit<ComponentProps<typeof Badge>, 'variant'> & {
  tone?: AppBadgeTone
}

export function AppBadge({ tone = 'neutral', className, ...props }: AppBadgeProps) {
  return <Badge variant="outline" className={cn('border-transparent', toneClass[tone], className)} {...props} />
}
