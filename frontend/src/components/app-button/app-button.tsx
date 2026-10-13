import { Loader2Icon } from 'lucide-react'
import type { ComponentProps } from 'react'

import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export type AppButtonIntent = 'primary' | 'secondary' | 'danger' | 'neutral' | 'ghost' | 'link'
export type AppButtonSize = 'sm' | 'md' | 'lg'

const variantByIntent = {
  primary: 'default',
  secondary: 'secondary',
  danger: 'destructive',
  neutral: 'outline',
  ghost: 'ghost',
  link: 'link',
} as const

const sizeByName = {
  sm: 'sm',
  md: 'default',
  lg: 'lg',
} as const

export type AppButtonProps = Omit<ComponentProps<typeof Button>, 'variant' | 'size'> & {
  intent?: AppButtonIntent
  size?: AppButtonSize
  loading?: boolean
  block?: boolean
}

export function AppButton({
  intent = 'primary',
  size = 'md',
  loading = false,
  block = false,
  disabled,
  className,
  children,
  asChild,
  ...props
}: AppButtonProps) {
  return (
    <Button
      variant={variantByIntent[intent]}
      size={sizeByName[size]}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      asChild={asChild}
      className={cn(
        size === 'lg' && 'h-11 px-4 text-[15px]',
        block && 'w-full',
        intent === 'primary' && 'font-semibold',
        className,
      )}
      {...props}
    >
      {asChild ? (
        children
      ) : (
        <>
          {loading ? <Loader2Icon className="animate-spin" /> : null}
          {children}
        </>
      )}
    </Button>
  )
}
