import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, Outlet } from 'react-router-dom'

import { dataDeletionPath, loginPath, privacyPath } from '../auth/routes'
import { cn } from '@/lib/utils'
import { Wordmark } from './wordmark'

export function PublicFrame({ children, wide = false }: { children: ReactNode; wide?: boolean }) {
  const { t } = useTranslation()

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden px-4 py-10">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-[40vh] bg-[radial-gradient(ellipse_at_top,_var(--primary)_0%,_transparent_60%)] opacity-20"
      />
      <div className={cn('relative flex w-full flex-col items-center gap-6', wide ? 'max-w-[720px]' : 'max-w-[400px]')}>
        <div className="flex flex-col items-center gap-1 text-center">
          <Wordmark size="lg" to={loginPath} />
          <p className="app-meta">{t('tagline')}</p>
        </div>
        <div className="app-page-enter w-full rounded-xl border bg-card p-6 md:p-8">{children}</div>
        <nav aria-label={t('legal.label')} className="flex flex-wrap justify-center gap-x-4 gap-y-1 text-[13px] text-muted-foreground">
          <Link to={privacyPath} className="hover:text-foreground hover:underline">
            {t('legal.privacy')}
          </Link>
          <Link to={dataDeletionPath} className="hover:text-foreground hover:underline">
            {t('legal.dataDeletion')}
          </Link>
        </nav>
      </div>
    </div>
  )
}

export function PublicLayout({ wide = false }: { wide?: boolean }) {
  return (
    <PublicFrame wide={wide}>
      <Outlet />
    </PublicFrame>
  )
}
