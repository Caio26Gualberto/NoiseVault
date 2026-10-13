import { useMemo, useState } from 'react'
import type { ReactNode } from 'react'

import { ShellRailContext } from './shell-rail-context'

export function ShellRailProvider({ children }: { children: ReactNode }) {
  const [node, setNode] = useState<ReactNode>(null)
  const value = useMemo(() => ({ node, setNode }), [node])
  return <ShellRailContext.Provider value={value}>{children}</ShellRailContext.Provider>
}
