import { SquarePlusIcon, ShareIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'
import { AppDialog } from '@/components/app-dialog'

type InstallAppDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function InstallAppDialog({ open, onOpenChange }: InstallAppDialogProps) {
  const { t } = useTranslation()

  return (
    <AppDialog
      open={open}
      onOpenChange={onOpenChange}
      title={t('layout:pwa.iosTitle')}
      description={t('layout:pwa.iosDescription')}
      footer={<AppButton onClick={() => onOpenChange(false)}>{t('layout:pwa.iosDone')}</AppButton>}
    >
      <ol className="flex flex-col gap-3 text-sm">
        <li className="flex items-start gap-3">
          <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted">
            <ShareIcon className="size-4" />
          </span>
          <span className="pt-1.5">{t('layout:pwa.iosStepShare')}</span>
        </li>
        <li className="flex items-start gap-3">
          <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted">
            <SquarePlusIcon className="size-4" />
          </span>
          <span className="pt-1.5">{t('layout:pwa.iosStepAdd')}</span>
        </li>
      </ol>
    </AppDialog>
  )
}
