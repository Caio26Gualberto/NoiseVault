import { LockKeyholeIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router-dom'

import { loginPath, registerPath } from '@/auth/routes'
import { AppButton } from '@/components/app-button'
import { cn } from '@/lib/utils'

type AppSignInPromptProps = {
  message: ReactNode
  className?: string
}

export function AppSignInPrompt({ message, className }: AppSignInPromptProps) {
  const { t } = useTranslation()
  const location = useLocation()
  const from = `${location.pathname}${location.search}`

  return (
    <div
      className={cn(
        'flex flex-col gap-3 rounded-xl border border-dashed p-4 sm:flex-row sm:items-center sm:justify-between',
        className,
      )}
    >
      <div className="flex items-start gap-3 text-sm">
        <LockKeyholeIcon className="mt-0.5 size-5 shrink-0 text-muted-foreground" />
        <span className="text-muted-foreground">{message}</span>
      </div>
      <div className="flex shrink-0 gap-2">
        <AppButton size="sm" asChild>
          <Link to={loginPath} state={{ from }}>
            {t('layout:visitor.signIn')}
          </Link>
        </AppButton>
        <AppButton size="sm" intent="neutral" asChild>
          <Link to={registerPath}>{t('layout:visitor.signUp')}</Link>
        </AppButton>
      </div>
    </div>
  )
}
