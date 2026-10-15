import { LocateFixedIcon, MapPinOffIcon, ShieldCheckIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

import { AppButton } from '@/components/app-button'

import { useApproxLocation } from '../hooks/use-approx-location'

export function LocationControl({ compact = false }: { compact?: boolean }) {
  const { t } = useTranslation('ensaio')
  const location = useApproxLocation()

  return (
    <div className="flex flex-col gap-2">
      {location.enabled ? (
        <div className="flex flex-wrap items-center gap-2">
          <span className="inline-flex items-center gap-1.5 text-sm font-medium text-success">
            <LocateFixedIcon className="size-4" aria-hidden="true" />
            {t('location.on')}
          </span>
          <AppButton type="button" size="sm" intent="ghost" onClick={() => void location.disable()}>
            <MapPinOffIcon />
            {t('location.disable')}
          </AppButton>
        </div>
      ) : (
        <AppButton type="button" size="sm" intent="neutral" className="self-start" loading={location.state === 'locating'} onClick={() => void location.enable()}>
          <LocateFixedIcon />
          {t('location.enable')}
        </AppButton>
      )}
      {location.state === 'denied' ? (
        <p role="alert" className="text-[13px] text-destructive">
          {t('location.denied')}
        </p>
      ) : location.state === 'unavailable' ? (
        <p role="alert" className="text-[13px] text-destructive">
          {t('location.unavailable')}
        </p>
      ) : null}
      {compact ? null : (
        <p className="flex gap-1.5 text-[13px] text-muted-foreground">
          <ShieldCheckIcon className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          {t('location.privacy')}
        </p>
      )}
    </div>
  )
}
