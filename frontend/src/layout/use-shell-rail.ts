import { useContext, useEffect } from 'react'
import type { ReactNode } from 'react'

import { ShellRailContext } from './shell-rail-context'

export function useShellRail(node: ReactNode) {
  const context = useContext(ShellRailContext)

  useEffect(() => {
    if (!context) return
    context.setNode(node)
    return () => context.setNode(null)
  }, [context, node])
}

export function useShellRailNode(): ReactNode {
  return useContext(ShellRailContext)?.node ?? null
}
