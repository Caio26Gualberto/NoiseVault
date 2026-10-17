import { TimerOffIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'

import { loginPath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'

export function ExternalStepExpired() {
  const { t } = useTranslation('auth')

  return (
    <div className="flex flex-col items-center gap-5 text-center" role="status">
      <div className="flex size-14 items-center justify-center rounded-full bg-warning/15 text-warning">
        <TimerOffIcon className="size-7" />
      </div>
      <div className="flex flex-col gap-1">
        <h1 className="app-h2">{t('external.expiredTitle')}</h1>
        <p className="text-sm text-muted-foreground">{t('external.expiredDescription')}</p>
      </div>
      <AppButton size="lg" block asChild>
        <Link to={loginPath}>{t('external.backToLogin')}</Link>
      </AppButton>
    </div>
  )
}
