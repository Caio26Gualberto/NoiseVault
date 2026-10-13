import { useContext } from 'react'

import { AppConfirmContext } from './app-confirm-context'

export function useAppConfirm() {
  const context = useContext(AppConfirmContext)
  if (!context) {
    throw new Error('useAppConfirm precisa estar dentro de <AppConfirmProvider>')
  }
  return context.confirm
}
