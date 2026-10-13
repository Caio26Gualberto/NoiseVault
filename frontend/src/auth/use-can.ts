import { useCallback } from 'react'

import type { Permission } from './permissions'
import { useAuth } from './use-auth'

export function useCan() {
  const { access } = useAuth()

  return useCallback(
    (permission: Permission) => access?.effectivePermissions.includes(permission) ?? false,
    [access],
  )
}
