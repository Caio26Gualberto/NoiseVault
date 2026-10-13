import type { ComponentProps, ReactNode } from 'react'

import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { cn } from '@/lib/utils'

type AppCardProps = Omit<ComponentProps<typeof Card>, 'title'> & {
  title?: ReactNode
  description?: ReactNode
  footer?: ReactNode
  padding?: 'normal' | 'compact' | 'none'
}

export function AppCard({ title, description, footer, padding = 'normal', className, children, ...props }: AppCardProps) {
  return (
    <Card
      size={padding === 'compact' ? 'sm' : 'default'}
      className={cn('ring-0 border border-border shadow-none', padding === 'none' && 'gap-0 py-0', className)}
      {...props}
    >
      {title || description ? (
        <CardHeader>
          {title ? <CardTitle className="app-h3">{title}</CardTitle> : null}
          {description ? <CardDescription>{description}</CardDescription> : null}
        </CardHeader>
      ) : null}
      {padding === 'none' ? children : <CardContent>{children}</CardContent>}
      {footer ? <CardFooter>{footer}</CardFooter> : null}
    </Card>
  )
}
