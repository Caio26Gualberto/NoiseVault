import { createContext } from 'react'
import type { ReactNode } from 'react'

export type ConfirmOptions = {
  title: ReactNode
  message?: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  danger?: boolean
  onConfirm: () => void | Promise<void>
}

export type AppConfirmContextValue = {
  confirm: (options: ConfirmOptions) => void
}

export const AppConfirmContext = createContext<AppConfirmContextValue | null>(null)
