import type { ComponentProps } from 'react'

import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

import type { AppButtonIntent } from './app-button'

const variantByIntent = {
  primary: 'default',
  secondary: 'secondary',
  danger: 'destructive',
  neutral: 'outline',
  ghost: 'ghost',
  link: 'link',
} as const

export type AppIconButtonProps = Omit<ComponentProps<typeof Button>, 'variant' | 'size' | 'aria-label'> & {
  label: string
  intent?: AppButtonIntent
  size?: 'sm' | 'md' | 'lg'
  active?: boolean
}

const sizeByName = { sm: 'icon-sm', md: 'icon', lg: 'icon-lg' } as const

export function AppIconButton({
  label,
  intent = 'ghost',
  size = 'md',
  active = false,
  className,
  ...props
}: AppIconButtonProps) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          variant={variantByIntent[intent]}
          size={sizeByName[size]}
          aria-label={label}
          aria-pressed={active || undefined}
          className={cn(active && 'text-primary', className)}
          {...props}
        />
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}
