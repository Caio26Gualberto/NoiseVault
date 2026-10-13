import { useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'

import { AppConfirmContext } from './app-confirm-context'
import type { ConfirmOptions } from './app-confirm-context'

export function AppConfirmProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const [options, setOptions] = useState<ConfirmOptions | null>(null)
  const [busy, setBusy] = useState(false)

  const confirm = useCallback((next: ConfirmOptions) => setOptions(next), [])

  const close = () => {
    if (busy) return
    setOptions(null)
  }

  const run = async () => {
    if (!options) return
    setBusy(true)
    try {
      await options.onConfirm()
      setOptions(null)
    } finally {
      setBusy(false)
    }
  }

  const value = useMemo(() => ({ confirm }), [confirm])

  return (
    <AppConfirmContext.Provider value={value}>
      {children}
      <AlertDialog open={options !== null} onOpenChange={(open) => (open ? null : close())}>
        <AlertDialogContent size="sm">
          <AlertDialogHeader>
            <AlertDialogTitle className="app-h3">{options?.title}</AlertDialogTitle>
            {options?.message ? <AlertDialogDescription>{options.message}</AlertDialogDescription> : null}
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={busy}>{options?.cancelLabel ?? t('actions.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              variant={options?.danger ? 'destructive' : 'default'}
              disabled={busy}
              onClick={(event) => {
                event.preventDefault()
                void run()
              }}
            >
              {options?.confirmLabel ?? t('actions.confirm')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </AppConfirmContext.Provider>
  )
}
