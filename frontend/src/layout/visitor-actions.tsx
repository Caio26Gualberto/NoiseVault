import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router-dom'

import { loginPath, registerPath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { cn } from '@/lib/utils'

type VisitorActionsProps = {
  stacked?: boolean
}

export function VisitorActions({ stacked = false }: VisitorActionsProps) {
  const { t } = useTranslation()
  const location = useLocation()

  return (
    <div className={cn('flex gap-2', stacked && 'flex-col')}>
      <AppButton size="sm" block={stacked} asChild>
        <Link to={loginPath} state={{ from: `${location.pathname}${location.search}` }}>
          {t('layout:visitor.signIn')}
        </Link>
      </AppButton>
      <AppButton size="sm" intent="neutral" block={stacked} asChild>
        <Link to={registerPath}>{t('layout:visitor.signUp')}</Link>
      </AppButton>
    </div>
  )
}

export function VisitorBanner() {
  const { t } = useTranslation()

  return (
    <div className="mb-6 flex flex-col gap-3 rounded-xl border bg-card p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-col gap-0.5">
        <span className="font-medium">{t('layout:visitor.bannerTitle')}</span>
        <span className="text-sm text-muted-foreground">{t('layout:visitor.bannerDescription')}</span>
      </div>
      <div className="shrink-0">
        <VisitorActions />
      </div>
    </div>
  )
}
