import type { ReactNode } from 'react'

import type { Permission } from './permissions'
import { useCan } from './use-can'

type CanProps = {
  permission: Permission
  children: ReactNode
  fallback?: ReactNode
}

export function Can({ permission, children, fallback = null }: CanProps) {
  const can = useCan()

  if (!can(permission)) {
    return <>{fallback}</>
  }

  return <>{children}</>
}
