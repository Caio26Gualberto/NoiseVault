import { createContext } from 'react'
import type { ReactNode } from 'react'

export type ShellRailContextValue = {
  node: ReactNode
  setNode: (node: ReactNode) => void
}

export const ShellRailContext = createContext<ShellRailContextValue | null>(null)
